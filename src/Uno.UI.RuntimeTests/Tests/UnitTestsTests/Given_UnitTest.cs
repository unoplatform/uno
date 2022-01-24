using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.UnitTestsTests
{
	[TestClass]
	class Given_UnitTest
	{
		static int When_UnhandledException_Count = 0;

		[TestMethod]
		public void When_UnhandledException()
		{
			if (When_UnhandledException_Count++ < 2)
			{
				throw new InvalidOperationException();
			}
		}
	}

	[TestClass]
	class Given_UnitTest_Initialize
	{
		static int Initialize_Count = 0;

		[TestInitialize]
		public void Initialize()
		{
			if (Initialize_Count++ < 2)
			{
				throw new InvalidOperationException();
			}
		}

		[TestMethod]
		public void When_Success()
		{
		}
	}

	[TestClass]
	class Give_UnitTest_DynamicData_From_Method
	{
		static int TestSucces_Count = 0;

		[DataTestMethod]
		[DynamicData(nameof(GetData), DynamicDataSourceType.Method)]
		public void When_Get_Arguments_From_Method(int a, int b, int expected)
		{
			var actual = a + b;
			Assert.AreEqual(expected, actual);
			TestSucces_Count++;
		}

		public static IEnumerable<object[]> GetData()
		{
			yield return new object[] { 1, 1, 2 };
			yield return new object[] { 12, 30, 42 };
			yield return new object[] { 14, 1, 15 };
		}
		
		[TestCleanup]
		public void TestCleanup()
		{
			Assert.Equals(TestSucces_Count, 3);
		}
	}

	[TestClass]
	class Give_UnitTest_DynamicData_From_Property
	{
		static int TestSucces_Count = 0;

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

		[TestCleanup]
		public void TestCleanup()
		{
			Assert.Equals(TestSucces_Count, 3);
		}
	}
}
