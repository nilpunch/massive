﻿namespace Massive.ECS
{
	public class NormalSetFactory : ISetFactory<ISet>
	{
		private readonly int _dataCapacity;

		public NormalSetFactory(int dataCapacity = Constants.DataCapacity)
		{
			_dataCapacity = dataCapacity;
		}

		public ISet CreateSet()
		{
			return new SparseSet(_dataCapacity);
		}

		public ISet CreateDataSet<T>() where T : unmanaged
		{
			return new DataSet<T>(_dataCapacity);
		}
	}
}