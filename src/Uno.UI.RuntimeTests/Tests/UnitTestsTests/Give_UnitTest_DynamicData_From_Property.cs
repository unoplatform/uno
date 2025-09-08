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
			// This is because Dispose is never called, and the assert is not be doing what it's supposed to do!
			// This assert is also wrong altogether.
			// The correct test flow *should* be:
			// 1. Create instance of the test class
			// 2. Invoke the first test case.
			// 3. Dispose
			// 4. Repeat for the other two test cases.
			// So, Dispose should be called three times. First with value 1, then 2, then 3.
			// The current behavior is:
			// 1. Create a single instance of the test class.
			// 2. Invoke all test cases.
			// 3. Dispose is never called.
			Assert.AreEqual(3, TestSucces_Count);
	}
}
