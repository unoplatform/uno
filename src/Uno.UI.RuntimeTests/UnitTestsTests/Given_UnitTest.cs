using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.UnitTestsTests
{
	[TestClass]
	public class Given_UnitTest
	{
		static int When_UnhandledException_Count;

		// This tests that automatic retry is working.
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_UnhandledException()
		{
			if (When_UnhandledException_Count++ < 2)
			{
				throw new InvalidOperationException();
			}
		}
	}

	[TestClass]
	public class Given_UnitTest_Initialize
	{
		static int Initialize_Count;

		// This tests that automatic retry is working.
		[TestInitialize]
		public void Initialize()
		{
			if (Initialize_Count++ < 2)
			{
				throw new InvalidOperationException();
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_Success()
		{
		}
	}
}
