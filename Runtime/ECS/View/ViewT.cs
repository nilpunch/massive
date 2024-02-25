﻿using System.Runtime.CompilerServices;

namespace Massive.ECS
{
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
	public readonly struct View<T> where T : unmanaged
	{
		private readonly IDataSet<T> _components;

		public View(IDataSet<T> components)
		{
			_components = components;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ForEach(EntityAction action) => ForEach((int id, ref T _) => action.Invoke(id));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ForEach(ActionRef<T> action) => ForEach((int _, ref T value) => action.Invoke(ref value));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ForEach(EntityActionRef<T> action)
		{
			var data = _components.AliveData;
			var ids = _components.AliveIds;
			for (int dense = ids.Length - 1; dense >= 0; dense--)
			{
				action.Invoke(ids[dense], ref data[dense]);
			}
		}
	}
}