﻿using System.Reflection;

namespace Massive
{
	/// <summary>
	/// Cross-platform component information.
	/// </summary>
	public static class ComponentMeta<T> where T : struct
	{
		// ReSharper disable once StaticMemberInGenericType
		public static bool HasAnyFields { get; }

		static ComponentMeta()
		{
			HasAnyFields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0;
		}

#if UNITY_2020_3_OR_NEWER
		[UnityEngine.Scripting.Preserve]
#endif
		private static void VirtualGenericsCompilerHint()
		{
			// ReSharper disable ReturnValueOfPureMethodIsNotUsed
			new NormalSetFactory().CreateDataSet<T>();
			new MassiveSetFactory().CreateDataSet<T>();
		}
	}
}