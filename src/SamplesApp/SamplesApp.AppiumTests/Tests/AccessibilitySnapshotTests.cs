#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
[TestCategory(TestCategories.HostRequired)]
public sealed class AccessibilitySnapshotTests : AppiumFixtureBase
{
	protected override string SampleQuery => AccessibilityScreenReaderSnapshotDefinition.SampleQuery;

	[TestMethod]
	[TestCategory(TestCategories.Snapshot)]
	public void AccessibilityScreenReader_CanonicalSnapshot_MatchesBaseline()
	{
		var definition = AccessibilityScreenReaderSnapshotDefinition.Definition;
		var actual = Session.CaptureSnapshot(definition);
		var baselinePath = SnapshotPaths.ResolveBaselinePath(Session.Options.Platform, definition, Session.Options);

		if (Session.Options.RecordSnapshots)
		{
			SnapshotSerializer.Write(baselinePath, actual);
			TestContext.WriteLine($"Recorded canonical accessibility baseline to '{baselinePath}' ({Session.DiagnosticContext}).");
			return;
		}

		var expected = SnapshotSerializer.Read(baselinePath)
			?? throw new AssertFailedException(
				$"Baseline file is missing: '{baselinePath}'. Run the snapshot test with {AppiumTestOptions.EnvVarRecordSnapshots}=1 to record it.");

		var diff = SnapshotComparer.Compare(expected, actual);
		if (diff.IsMatch)
		{
			return;
		}

		var actualPath = Session.WriteActualSnapshot(definition.SnapshotId, actual);
		var rawTreePath = Session.TryWriteDiagnosticTree(definition.SnapshotId);

		Assert.Fail(
			$"Canonical accessibility snapshot diverged from the committed baseline ({Session.DiagnosticContext}).{System.Environment.NewLine}" +
			$"  baseline: {baselinePath}{System.Environment.NewLine}" +
			$"  actual:   {actualPath}{System.Environment.NewLine}" +
			$"  rawTree:  {rawTreePath ?? "(not captured)"}{System.Environment.NewLine}" +
			diff.Format());
	}
}

