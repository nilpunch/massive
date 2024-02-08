﻿using UnityEngine;

namespace MassiveData.Samples.Physics
{
	public class DebugCollisionDetection : MonoBehaviour
	{
		[SerializeField] private Vector3 _size;
		[Space]
		[SerializeField] private Transform _sphere;
		[SerializeField] private float _sphereRadius;

		private void OnDrawGizmos()
		{
			if (_sphere == null)
				return;

			BoxCollider boxCollider = new BoxCollider(0, _size)
			{
				WorldPosition = transform.position,
				WorldRotation = transform.rotation,
			};

			SphereCollider sphereCollider = new SphereCollider(0, _sphereRadius)
			{
				WorldPosition = _sphere.position
			};

			Gizmos.DrawSphere(sphereCollider.WorldPosition, _sphereRadius);

			var orig = Gizmos.matrix;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawCube(Vector3.zero, _size);
			Gizmos.matrix = orig;

			Contact contact = new Contact();
			
			CollisionTester.SphereVsBox(ref sphereCollider, ref boxCollider, boxCollider.WorldPosition - sphereCollider.WorldPosition, boxCollider.WorldRotation, ref contact);

			if (contact.Depth > 0f)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(sphereCollider.WorldPosition + contact.OffsetFromA, 0.1f);
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(sphereCollider.WorldPosition + contact.OffsetFromA, sphereCollider.WorldPosition + contact.OffsetFromA + contact.Normal * contact.Depth);
			}
		}
	}
}