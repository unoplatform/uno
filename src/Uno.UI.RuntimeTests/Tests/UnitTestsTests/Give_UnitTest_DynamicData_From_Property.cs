using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.UnitTestsTests
{
	[TestClass]
	public class Give_UnitTest_DynamicData_From_Property : IDisposable
	{
		static int TestSucces_Count;

		[TestMethod]
		[DynamicData(nameof(Data))]
		public void When_Get_Arguments_From_Property(int a, int b, int expected)
		{
			var actual = a + b;
			Assert.AreEqual(expected, actual);
			TestSucces_Count++;
		}

		public static IEnumerable<object[]> Data
		{
			get
			{
				yield return new object[] { 1, 1, 2 };
				yield return new object[] { 12, 30, 42 };
				yield return new object[] { 14, 1, 15 };
			}
		}

		public void Dispose() =>
			// TODO: This used to be Assert.Equals which **always** throws at runtime and everything was green.
			// It's likely that Dispose is never called, or the exception is swallowed.
			// This means that this assert might not be doing what it's supposed to do!
			Assert.AreEqual(3, TestSucces_Count);
	}
}
