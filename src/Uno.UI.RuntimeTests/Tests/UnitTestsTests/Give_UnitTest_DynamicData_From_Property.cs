using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.UnitTestsTests
{
	[TestClass]
	public class Give_UnitTest_DynamicData_From_Property : IDisposable
	{
		static int TestSucces_Count;

		[DataTestMethod]
		[DynamicData(nameof(Data), DynamicDataSourceType.Property)]
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

#pragma warning disable MSTEST0029 // Public methods should be test methods - https://github.com/microsoft/testfx/issues/4660
		public void Dispose() =>
			Assert.Equals(TestSucces_Count, 3);
#pragma warning restore MSTEST0029 // Public methods should be test methods
	}
}
