﻿using System;
using System.Runtime.CompilerServices;

namespace Massive
{
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.NullChecks, false)]
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.ArrayBoundsChecks, false)]
	[Unity.IL2CPP.CompilerServices.Il2CppSetOption(Unity.IL2CPP.CompilerServices.Option.DivideByZeroChecks, false)]
	public class MassiveSparseSet : SparseSet, IMassiveSet
	{
		private readonly int[] _denseByFrames;
		private readonly int[] _sparseByFrames;
		private readonly int[] _maxDenseByFrames;
		private readonly int[] _maxIdByFrames;
		private readonly int[] _aliveCountByFrames;

		private readonly int _framesCapacity;
		private int _currentFrame;
		private int _savedFrames;

		public MassiveSparseSet(int framesCapacity = Constants.FramesCapacity, int dataCapacity = Constants.DataCapacity)
			: base(dataCapacity)
		{
			_framesCapacity = framesCapacity;

			_denseByFrames = new int[framesCapacity * Dense.Length];
			_sparseByFrames = new int[framesCapacity * Sparse.Length];
			_maxDenseByFrames = new int[framesCapacity];
			_maxIdByFrames = new int[framesCapacity];
			_aliveCountByFrames = new int[framesCapacity];
		}

		public int CurrentFrame => _currentFrame;

		/// <summary>
		/// Can be negative, when there absolutely no saved frames to restore information.
		/// </summary>
		public int CanRollbackFrames => _savedFrames - 1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SaveFrame()
		{
			int currentMaxDense = MaxDense;
			int currentMaxId = MaxId;
			int currentAliveCount = AliveCount;
			int nextFrame = Loop(_currentFrame + 1, _framesCapacity);

			// Copy everything from current state to next frame
			Array.Copy(Dense, 0, _denseByFrames, nextFrame * Dense.Length, currentMaxDense);
			Array.Copy(Sparse, 0, _sparseByFrames, nextFrame * Sparse.Length, currentMaxId);
			_maxDenseByFrames[nextFrame] = currentMaxDense;
			_maxIdByFrames[nextFrame] = currentMaxId;
			_aliveCountByFrames[nextFrame] = currentAliveCount;

			_currentFrame = nextFrame;
			_savedFrames = Math.Min(_savedFrames + 1, _framesCapacity);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Rollback(int frames)
		{
			if (frames > CanRollbackFrames)
			{
				throw new InvalidOperationException($"Can't rollback this far. CanRollback:{CanRollbackFrames}, Requested: {frames}.");
			}

			_savedFrames -= frames;
			_currentFrame = LoopNegative(_currentFrame - frames, _framesCapacity);

			// Copy everything from rollback frame to current state
			int rollbackMaxDense = _maxDenseByFrames[_currentFrame];
			int rollbackMaxId = _maxIdByFrames[_currentFrame];
			int rollbackAliveCount = _aliveCountByFrames[_currentFrame];
			int rollbackFrame = _currentFrame;

			// Copy last MaxDense and MaxId elements to ensure zeroing excess
			Array.Copy(_denseByFrames, rollbackFrame * Dense.Length, Dense, 0, MaxDense);
			Array.Copy(_sparseByFrames, rollbackFrame * Sparse.Length, Sparse, 0, MaxId);
			MaxDense = rollbackMaxDense;
			MaxId = rollbackMaxId;
			AliveCount = rollbackAliveCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Loop(int a, int b)
		{
			return a % b;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int LoopNegative(int a, int b)
		{
			int result = a % b;

			if (result < 0)
			{
				return result + b;
			}

			return result;
		}
	}
}