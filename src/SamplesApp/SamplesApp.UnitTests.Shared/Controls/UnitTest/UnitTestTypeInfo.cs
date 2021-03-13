#nullable enable

using System;
using System.Reflection;

namespace Uno.UI.Samples.Tests
{
	public class UnitTestClassInfo
	{
		public UnitTestClassInfo(
			Type? type,
			MethodInfo[]? tests,
			MethodInfo? initialize,
			MethodInfo? cleanup)
		{
			Type = type;
			Tests = tests;
			Initialize = initialize;
			Cleanup = cleanup;
		}

		public Type? Type { get; }

		public MethodInfo[]? Tests { get; }

		public MethodInfo? Initialize { get; }

		public MethodInfo? Cleanup { get; }
	}
}
