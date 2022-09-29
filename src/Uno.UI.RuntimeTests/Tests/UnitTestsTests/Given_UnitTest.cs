#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.UnitTestsTests
{
	[TestClass]
	class Given_UnitTest
	{
		static int When_UnhandledException_Count;

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
		static int Initialize_Count;

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
}
