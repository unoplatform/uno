#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Tests;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_UnitTestsControl
{
	[TestMethod]
	public async Task When_Instance_Run_Is_First_Run_It_Completes()
	{
		var embeddedRoot = TestServices.WindowHelper.EmbeddedTestRoot;
		try
		{
			// A fresh runner, as hosted by NativeStorageRuntimeTests: RunTestsForInstance without a prior RunTests.
			UnitTestsControl runner = new();

			await runner.RunTestsForInstance(new SingleTestFixture(), new UnitTestEngineConfig());

			Assert.AreEqual("1", runner.RunTestCountForUITest);
		}
		finally
		{
			TestServices.WindowHelper.EmbeddedTestRoot = embeddedRoot;
		}
	}

	// No [TestClass]: only run through RunTestsForInstance, never discovered by the outer runner.
	public class SingleTestFixture
	{
		[TestMethod]
		public void Passes()
		{
		}
	}
}
