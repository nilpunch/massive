﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Massive.Samples.Physics
{
	public readonly struct Collision
	{
		public ContactPoint ContactFirst { get; }

		public ContactPoint ContactSecond { get; }

		public Vector3 PenetrationNormal { get; }

		public float PenetrationDepth { get; }

		public Collision(ContactPoint contactFirst, ContactPoint contactSecond, Vector3 penetrationNormal, float penetrationDepth)
		{
			ContactFirst = contactFirst;
			PenetrationNormal = penetrationNormal;
			PenetrationDepth = penetrationDepth;
			ContactSecond = contactSecond;
		}
	}

	public struct ContactPoint
	{
		public ContactPoint(Vector3 position)
		{
			Position = position;
		}

		public Vector3 Position { get; }
	}

	public static class EpaAlgorithm
	{
		private static float Tolerance => 0.0001f;
		private static float NormalBias => 0.00001f;

		public static List<MinkowskiDifference> Vertices { get; } = new List<MinkowskiDifference>();
		public static List<PolytopeFace> Faces { get; } = new List<PolytopeFace>();
		private static List<(int a, int b)> LooseEdges { get; } = new List<(int a, int b)>();

		public static Vector3 Barycentric(Vector3 a, Vector3 b, Vector3 c, Vector3 point, bool clamp = false)
		{
			Vector3 v0 = b - a;
			Vector3 v1 = c - a;
			Vector3 v2 = point - a;
			float d00 = Vector3.Dot(v0, v0);
			float d01 = Vector3.Dot(v0, v1);
			float d11 = Vector3.Dot(v1, v1);
			float d20 = Vector3.Dot(v2, v0);
			float d21 = Vector3.Dot(v2, v1);
			float denominator = d00 * d11 - d01 * d01;
			float v = (d11 * d20 - d01 * d21) / denominator;
			float w = (d00 * d21 - d01 * d20) / denominator;
			float u = 1f - v - w;

			return new Vector3(u, v, w);
		}

		public struct PolytopeFace
		{
			public PolytopeFace(int a, int b, int c, Vector3 centroid)
			{
				A = a;
				B = b;
				C = c;

				Vector3 vA = Vertices[A].Difference;
				Vector3 vB = Vertices[B].Difference;
				Vector3 vC = Vertices[C].Difference;
				Vector3 center = (vA + vB + vC) / 3f;

				Vector3 direction = center - centroid;
				Vector3 normal = CalculateFaceNormal(vA, vB, vC);

				if (Vector3.Dot(direction, normal) < 0f)
				{
					(B, C) = (C, B);
					Normal = -normal;
				}
				else
				{
					Normal = normal;
				}
			}

			public int A;
			public int B;
			public int C;
			public Vector3 Normal;
		}

		public static bool ApproximatelyEqual(float a, float b, float epsilon)
		{
			var difference = Mathf.Abs(a - b);
			return difference <= epsilon || difference <= Mathf.Max(Mathf.Abs(a), Mathf.Abs(b)) * epsilon;
		}

		public static Collision Calculate<T>(Simplex simplex, T shapeA,
			T shapeB, int maxIterations = 100) where T : ISupportMappable
		{
			Faces.Clear();
			Vertices.Clear();

			Vertices.Add(simplex.A);
			Vertices.Add(simplex.B);
			Vertices.Add(simplex.C);
			Vertices.Add(simplex.D);

			var centroid = CalculateCentroid();

			Faces.Add(new PolytopeFace(0, 1, 2, centroid));
			Faces.Add(new PolytopeFace(0, 2, 3, centroid));
			Faces.Add(new PolytopeFace(0, 3, 1, centroid));
			Faces.Add(new PolytopeFace(1, 3, 2, centroid));

			(float Distance, PolytopeFace Face) closestFace = default;

			for (int i = 0; i < maxIterations; i++)
			{
				closestFace = FindClosestFace(Faces);

				var searchDirection = closestFace.Face.Normal;
				MinkowskiDifference supportPoint = MinkowskiDifference.Calculate(shapeA, shapeB, searchDirection);

				float minkowskiDistance = Vector3.Dot(supportPoint.Difference, searchDirection);
				if (minkowskiDistance - closestFace.Distance < Tolerance)
				{
					break;
				}

				Vertices.Add(supportPoint);

				ExpandPolytope(supportPoint, centroid);
			}

			// foreach (var face in Faces)
			// {
			// 	Gizmos.color = Color.white;
			// 	Gizmos.DrawLine(Vertices[face.A].Difference, Vertices[face.B].Difference);
			// 	Gizmos.DrawLine(Vertices[face.B].Difference, Vertices[face.C].Difference);
			// 	Gizmos.DrawLine(Vertices[face.C].Difference, Vertices[face.A].Difference);
			// 	Gizmos.color = Color.cyan;
			// 	Vector3 center = (Vertices[face.A].Difference + Vertices[face.B].Difference + Vertices[face.C].Difference) / 3f;
			// 	Gizmos.DrawLine(center, center + face.Normal);
			// }

			// if (iteration >= maxIterations)
			// {
			// 	throw new Exception();
			// }

			Vector3 barycentric = Barycentric(
				Vertices[closestFace.Face.A].Difference,
				Vertices[closestFace.Face.B].Difference,
				Vertices[closestFace.Face.C].Difference,
				closestFace.Face.Normal * closestFace.Distance);

			Vector3 supportAA = Vertices[closestFace.Face.A].SupportA;
			Vector3 supportAB = Vertices[closestFace.Face.B].SupportA;
			Vector3 supportAC = Vertices[closestFace.Face.C].SupportA;
			Vector3 supportBA = Vertices[closestFace.Face.A].SupportB;
			Vector3 supportBB = Vertices[closestFace.Face.B].SupportB;
			Vector3 supportBC = Vertices[closestFace.Face.C].SupportB;

			Vector3 point1 = barycentric.x * supportAA + barycentric.y * supportAB + barycentric.z * supportAC;
			Vector3 point2 = barycentric.x * supportBA + barycentric.y * supportBB + barycentric.z * supportBC;

			return new Collision(new ContactPoint(point1), new ContactPoint(point2), closestFace.Face.Normal, closestFace.Distance + Tolerance);
		}

		public static void ExpandPolytope(MinkowskiDifference supportPoint, Vector3 centroid)
		{
			LooseEdges.Clear();

			for (int i = 0; i < Faces.Count; i++)
			{
				var face = Faces[i];

				if (Vector3.Dot(face.Normal, supportPoint.Difference - Vertices[face.A].Difference) > 0f)
				{
					(int a, int b) edgeAB = (face.A, face.B);
					(int a, int b) edgeBC = (face.B, face.C);
					(int a, int b) edgeCA = (face.C, face.A);

					RemoveIfExistsOrAdd(LooseEdges, edgeAB);
					RemoveIfExistsOrAdd(LooseEdges, edgeBC);
					RemoveIfExistsOrAdd(LooseEdges, edgeCA);

					Faces.RemoveAt(i);
					i -= 1;
				}
			}

			int c = Vertices.Count - 1;
			foreach ((int a, int b) in LooseEdges)
			{
				var face = new PolytopeFace(a, b, c, centroid);
				Faces.Add(face);
			}
		}

		public static (float Distance, PolytopeFace Face) FindClosestFace(List<PolytopeFace> faces)
		{
			(float Distance, PolytopeFace Face) closest = (Distance: float.MaxValue, default);

			for (int i = 0; i < faces.Count; i++)
			{
				var face = faces[i];
				var distance = Vector3.Dot(Vertices[face.A].Difference, face.Normal);

				if (distance < closest.Distance)
				{
					closest = (distance, face);
				}
			}

			return closest;
		}

		private static Vector3 CalculateFaceNormal(Vector3 a, Vector3 b, Vector3 c)
		{
			return Vector3.Normalize(Vector3.Cross(b - a, c - a));
		}

		private static Vector3 CalculateCentroid()
		{
			Vector3 center = Vector3.zero;
			foreach (var vertex in Vertices)
				center += vertex.Difference;
			center /= Vertices.Count;
			return center;
		}

		public static void RemoveIfExistsOrAdd<T>(List<(T a, T b)> edges, (T a, T b) edge) where T : IEquatable<T>
		{
			int edgeIndex = -1;

			for (int index = 0; index < edges.Count; index++)
			{
				(T a, T b) pair = edges[index];

				if (pair.a.Equals(edge.b) && pair.b.Equals(edge.a))
				{
					edgeIndex = index;
					break;
				}
			}

			if (edgeIndex != -1)
			{
				edges.RemoveAt(edgeIndex);
			}
			else
			{
				edges.Add(edge);
			}
		}
	}
}