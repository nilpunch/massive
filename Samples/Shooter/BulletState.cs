﻿using UnityEngine;

namespace UPR.PredictionRollback
{
	public struct BulletState
	{
		public EntityTransform Transform;
		
		public Vector3 Velocity;

		public float Damage;

		public float Lifetime;

		public bool IsDestroyed => Lifetime <= 0f;
	}
}