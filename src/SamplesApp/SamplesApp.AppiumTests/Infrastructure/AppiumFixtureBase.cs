#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Spins up an Appium session per test for the platform selected by
/// <c>UNO_APPIUM_PLATFORM</c>. Host-independent tests should not derive from
/// this class.
/// </summary>
public abstract class AppiumFixtureBase
{
	private AppiumTestSession? _session;

	/// <summary>
	/// Override to point the fixture at a specific sample. Format is the same
	/// string SamplesApp's App.Tests.TryNavigateToLaunchSample expects:
	/// <c>sample=Category/SampleName</c>.
	/// </summary>
	protected abstract string SampleQuery { get; }

	public TestContext TestContext { get; set; } = null!;

	protected AppiumTestSession Session
		=> _session ?? throw new InvalidOperationException("Appium test session not initialized.");

	[TestInitialize]
	public void InitializeTest()
		=> _session = AppiumTestSession.Create(TestContext, SampleQuery);

	[TestCleanup]
	public void CleanupTest()
	{
		if (_session is null)
		{
			return;
		}

		try
		{
			_session.Dispose();
		}
		catch (Exception ex) when (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
		{
			TestContext.WriteLine($"Session cleanup failed after {TestContext.CurrentTestOutcome}: {ex}");
		}
		finally
		{
			_session = null;
		}
	}
}
