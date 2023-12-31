﻿using UnityEngine;

namespace Massive.Samples.Shooter
{
    public struct BulletState
    {
        public TransformState Transform;
        public Vector3 Velocity;

        public float Damage;

        public float Lifetime;

        public bool IsDestroyed => Lifetime <= 0f;
    }
}
