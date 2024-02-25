﻿namespace Massive.ECS
{
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
	public readonly struct FilterView<T> where T : unmanaged
	{
		private readonly IDataSet<T> _components;
		private readonly Filter _filter;

		public FilterView(IDataSet<T> components, Filter filter)
		{
			_components = components;
			_filter = filter;
		}

		public void ForEach(EntityAction action) => ForEach((int id, ref T _) => action.Invoke(id));

		public void ForEach(ActionRef<T> action) => ForEach((int _, ref T value) => action.Invoke(ref value));

		public void ForEach(EntityActionRef<T> action)
		{
			var data = _components.AliveData;
			var ids = _components.AliveIds;
			for (int dense = ids.Length - 1; dense >= 0; dense--)
			{
				int id = ids[dense];
				if (_filter.IsOkay(id))
				{
					action.Invoke(id, ref data[dense]);
				}
			}
		}
	}
}