using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests;

// TEMP: exercises the in-job re-run of failed runtime tests on every platform (#24682). Drop before merge.
[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class Given_InlineRerunProbe
{
	// Fails in the full shard run, which has no filter, and passes in the re-run, which filters on the failed tests.
	[Filters]
	[TestMethod]
	public void When_Run_Then_OnlyPassesWhenFiltered(string filters)
		=> Assert.IsFalse(string.IsNullOrWhiteSpace(filters), "Expected to fail on the first pass: the in-job re-run should recover it.");
}
