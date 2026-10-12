#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Tests;
using Windows.System;
using RunnerResult = Uno.UI.Samples.Tests.TestResult;

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

	[TestMethod]
	public async Task When_Run_Has_Failure_Dashboard_Shows_Error_Outcome()
	{
		var runner = await LoadRunner();

		await runner.RunTestsForInstance(new MixedFixture(), new UnitTestEngineConfig { Attempts = 1 });

		// Fails (1) + Passes (1) + two DataRows (2) + Ignored (1, reported as skipped).
		Assert.AreEqual(5, runner.PlannedTestCount);
		Assert.AreEqual("4", runner.RunTestCountForUITest);
		Assert.AreEqual("1", runner.FailedTestCountForUITest);

		var infoBar = runner.RunInfoBar;
		Assert.IsNotNull(infoBar, "The status InfoBar is loaded once a run starts outside CI.");
		Assert.AreEqual(InfoBarSeverity.Error, infoBar.Severity);
		Assert.AreEqual("1 failed", infoBar.Title);

		Assert.AreEqual(1, runner.Failures.Count);
		StringAssert.Contains(runner.Failures[0], "MixedFixture.Fails");
		Assert.AreEqual("1 of 1", Find<TextBlock>(runner, "ShellFailurePosition").Text);

		// Automation contract: the section-sign list, accumulated details, and the frozen counter names.
		StringAssert.Contains(Find<TextBlock>(runner, "failedTests").Text, "\u00A7Fails");
		StringAssert.Contains(Find<TextBlock>(runner, "failedTestDetails").Text, "Failed: Fails");
		Assert.AreEqual("4", Find<TextBlock>(runner, "runTestCount").Text);
		Assert.AreEqual("1", Find<TextBlock>(runner, "ignoredTestCount").Text);
		Assert.AreEqual("Passed: 3", Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(Find<TextBlock>(runner, "succeededTestCount")));

		// One class header and five rows.
		var results = Find<StackPanel>(runner, "testResults");
		Assert.AreEqual(6, results.Children.Count);

		Find<ToggleButton>(runner, "ShellFailedOnlyToggle").IsChecked = true;
		Assert.AreEqual(2, results.Children.Count(c => c.Visibility == Visibility.Visible), "Failed only keeps the class header and the failed row.");
		Find<ToggleButton>(runner, "ShellFailedOnlyToggle").IsChecked = false;
		Assert.AreEqual(6, results.Children.Count(c => c.Visibility == Visibility.Visible));
	}

	[TestMethod]
	public async Task When_Run_Passes_Dashboard_Shows_Success()
	{
		var runner = await LoadRunner();

		await runner.RunTestsForInstance(new SingleTestFixture(), new UnitTestEngineConfig { Attempts = 1 });

		Assert.AreEqual(InfoBarSeverity.Success, runner.RunInfoBar?.Severity);
		Assert.AreEqual("Passed", runner.RunInfoBar?.Title);
		Assert.AreEqual(0, runner.Failures.Count);
	}

	[TestMethod]
	public async Task When_Running_On_CI_No_InfoBar_Or_Rows_Are_Created()
	{
		var runner = await LoadRunner();
		runner.IsRunningOnCI = true;

		await runner.RunTestsForInstance(new MixedFixture(), new UnitTestEngineConfig { Attempts = 1 });

		Assert.IsNull(runner.RunInfoBar);
		Assert.AreEqual(0, runner.PlannedTestCount, "Planning is skipped on CI.");
		Assert.AreEqual(0, Find<StackPanel>(runner, "testResults").Children.Count);
		Assert.AreEqual("1", runner.FailedTestCountForUITest);
		StringAssert.Contains(Find<TextBlock>(runner, "failedTests").Text, "\u00A7Fails");
	}

	[TestMethod]
	[DataRow(1000, 0)]
	[DataRow(800, 1)]
	[DataRow(600, 2)]
	public async Task When_Runner_Width_Changes_Layout_Follows(double width, int expectedLayout)
	{
		var expected = (UnitTestsControl.RunLayout)expectedLayout;
		var runner = await LoadRunner(width);

		Assert.AreEqual(expected, runner.CurrentRunLayout);
		Assert.AreEqual(expected == UnitTestsControl.RunLayout.Wide ? 0 : 1, Grid.GetRow(Find<TextBox>(runner, "testFilter")));
		Assert.AreEqual(expected == UnitTestsControl.RunLayout.Wide ? 4 : 2, Find<Grid>(runner, "ShellRunStats").ColumnDefinitions.Count);
		Assert.AreEqual(
			expected == UnitTestsControl.RunLayout.Tiny ? Visibility.Collapsed : Visibility.Visible,
			Find<TextBlock>(runner, "ShellPassedLabel").Visibility,
			"Tiny drops the card labels.");
		Assert.AreEqual(TextWrapping.NoWrap, Find<TextBlock>(runner, "ShellInconclusiveLabel").TextWrapping);
	}

	[TestMethod]
	public async Task When_Splitters_Are_Used_From_Keyboard_Sizes_Change()
	{
		var runner = await LoadRunner();
		var splitter = Find<ShellSplitter>(runner, "ShellColumnSplitter");
		double SplitterX()
		{
			runner.UpdateLayout();
			return splitter.TransformToVisual(runner).TransformPoint(new Windows.Foundation.Point(0, 0)).X;
		}

		var before = SplitterX();

		Assert.IsTrue(splitter.HandleKey(VirtualKey.Right));
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(before + ShellSplitter.KeyboardStep, SplitterX(), 1);

		Assert.IsTrue(splitter.HandleKey(VirtualKey.Home));
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(before, SplitterX(), 1, "Home restores the 50/50 split.");

		Assert.IsFalse(splitter.HandleKey(VirtualKey.Up), "A column splitter ignores Up.");
	}

	[TestMethod]
	[DataRow(VirtualKey.Up, Orientation.Horizontal, false, -16d)]
	[DataRow(VirtualKey.Down, Orientation.Horizontal, false, 16d)]
	[DataRow(VirtualKey.Left, Orientation.Horizontal, false, null)]
	[DataRow(VirtualKey.Left, Orientation.Vertical, false, -16d)]
	[DataRow(VirtualKey.Right, Orientation.Vertical, false, 16d)]
	[DataRow(VirtualKey.Right, Orientation.Vertical, true, -16d)]
	[DataRow(VirtualKey.Up, Orientation.Vertical, false, null)]
	public void When_Splitter_Key_Is_Pressed_Delta_Follows_Orientation(VirtualKey key, Orientation orientation, bool isRightToLeft, double? expected)
		=> Assert.AreEqual(expected, ShellSplitter.GetKeyboardDelta(key, orientation, isRightToLeft));

	[TestMethod]
	[DataRow(0, 0, false, InfoBarSeverity.Success, "Passed")]
	[DataRow(0, 2, false, InfoBarSeverity.Warning, "2 inconclusive")]
	[DataRow(3, 2, false, InfoBarSeverity.Error, "3 failed")]
	[DataRow(0, 0, true, InfoBarSeverity.Warning, "Stopped")]
	[DataRow(1, 0, true, InfoBarSeverity.Error, "1 failed")]
	[DataRow(-1, 0, false, InfoBarSeverity.Error, "Runner failed")]
	public void When_Run_Ends_Outcome_Follows_Counts(int failed, int inconclusive, bool isStopped, InfoBarSeverity severity, string title)
		=> Assert.AreEqual((severity, title), UnitTestsControl.GetRunOutcome(failed, inconclusive, isStopped));

	[TestMethod]
	public void When_Formatting_Run_Values_Text_Is_Compact()
	{
		Assert.AreEqual("12 ms", UnitTestsControl.FormatDuration(TimeSpan.FromMilliseconds(12.3)));
		Assert.AreEqual(1.5.ToString("0.0") + " s", UnitTestsControl.FormatDuration(TimeSpan.FromMilliseconds(1500)));
		Assert.AreEqual("2m 05s", UnitTestsControl.FormatDuration(TimeSpan.FromSeconds(125)));

		Assert.AreEqual("3", UnitTestsControl.FormatProgress(3, 0));
		Assert.AreEqual("3 of 5", UnitTestsControl.FormatProgress(3, 5));
		Assert.AreEqual("5 of 5", UnitTestsControl.FormatProgress(7, 5));

		Assert.AreEqual("", UnitTestsControl.FormatFailurePosition(-1, 0));
		Assert.AreEqual("2 of 3", UnitTestsControl.FormatFailurePosition(1, 3));
		Assert.AreEqual("2/3", UnitTestsControl.FormatFailurePosition(1, 3, isShort: true));

		Assert.AreEqual("When_Tapped", UnitTestsControl.GetDisplayName("When_Tapped()"));
		Assert.AreEqual("When_Row(1, 2)", UnitTestsControl.GetDisplayName("When_Row(1, 2)"));
	}

	[TestMethod]
	public void When_Result_Has_Status_Detail_And_Glyph_Match()
	{
		Assert.AreEqual("4 ms", UnitTestsControl.GetResultDetail(RunnerResult.Passed, TimeSpan.FromMilliseconds(4), 0, null, null));
		Assert.AreEqual("passed after 2 retries", UnitTestsControl.GetResultDetail(RunnerResult.Passed, TimeSpan.Zero, 2, null, null));
		Assert.AreEqual("skipped", UnitTestsControl.GetResultDetail(RunnerResult.Skipped, TimeSpan.Zero, 0, null, "\n--> [Ignored]"));
		Assert.AreEqual("Expected 4", UnitTestsControl.GetResultDetail(RunnerResult.Failed, TimeSpan.Zero, 0, "\n  Expected 4\nBut was 5", null));
		Assert.AreEqual("failed", UnitTestsControl.GetResultDetail(RunnerResult.Error, TimeSpan.Zero, 0, null, null));
		Assert.AreEqual(80, UnitTestsControl.GetResultDetail(RunnerResult.Inconclusive, TimeSpan.Zero, 0, null, new string('x', 200)).Length);

#if HAS_UNO
		Assert.AreEqual("\uE738", UnitTestsControl.GetResultGlyph(RunnerResult.Skipped, false), "Uno.Fonts.Fluent has no E733.");
#else
		Assert.AreEqual("\uE733", UnitTestsControl.GetResultGlyph(RunnerResult.Skipped, false));
#endif
		Assert.AreEqual("\uE72C", UnitTestsControl.GetResultGlyph(RunnerResult.Passed, true));
		Assert.AreEqual("ShellTestWarningBrush", UnitTestsControl.GetResultBrushKey(RunnerResult.Passed, true));
		Assert.AreEqual("ShellTestFailedBrush", UnitTestsControl.GetResultBrushKey(RunnerResult.Error, false));
	}

	[TestMethod]
	[DataRow(0, false, 0, 0)]
	[DataRow(3, false, 0, 3)]
	[DataRow(3, true, 1, 1)]
	[DataRow(2, true, 1, 0)]
	public void When_Layout_Is_Not_Wide_Stat_Cards_Wrap(int index, bool isCompact, int row, int column)
		=> Assert.AreEqual((row, column), UnitTestsControl.GetStatCardCell(index, isCompact ? UnitTestsControl.RunLayout.Compact : UnitTestsControl.RunLayout.Wide));

	[TestMethod]
	[DataRow(1000d, 0d, 600d)]
	[DataRow(1000d, 100d, 540d)]
	[DataRow(200d, 100d, 120d)]
	public void When_Runner_Is_Short_Header_Keeps_Room_For_Results(double height, double failureDetailsHeight, double expected)
		=> Assert.AreEqual(expected, UnitTestsControl.GetHeaderMaxHeight(height, failureDetailsHeight));

	[TestMethod]
	public async Task When_Instance_Run_Twice_Failures_Do_Not_Accumulate()
	{
		var runner = await LoadRunner();

		await runner.RunTestsForInstance(new MixedFixture(), new UnitTestEngineConfig { Attempts = 1 });
		await runner.RunTestsForInstance(new MixedFixture(), new UnitTestEngineConfig { Attempts = 1 });

		Assert.AreEqual(1, runner.Failures.Count);
		Assert.AreEqual(0, runner.FailureIndex);
		Assert.AreEqual("1 of 1", Find<TextBlock>(runner, "ShellFailurePosition").Text);
		Assert.AreEqual(6, Find<StackPanel>(runner, "testResults").Children.Count);
	}

	[TestMethod]
	public async Task When_Rows_Have_Details_They_Can_Be_Invoked()
	{
		var runner = await LoadRunner();

		await runner.RunTestsForInstance(new MixedFixture(), new UnitTestEngineConfig { Attempts = 1 });
		await TestServices.WindowHelper.WaitForIdle();

		// The failed row (failure card) and the skipped row (its ignore message) are buttons; the passing rows are not.
		var rows = Find<StackPanel>(runner, "testResults").Children.OfType<StackPanel>().ToArray();
		Assert.AreEqual(2, rows.Length);

		var failedRow = (Button)rows[0].Children[0];
		Assert.IsTrue(failedRow.IsTabStop);
		var detailsRow = runner.FailureDetailsRow;
		detailsRow.Height = new GridLength(0);
		((IInvokeProvider)new ButtonAutomationPeer(failedRow).GetPattern(PatternInterface.Invoke)!).Invoke();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(UnitTestsControl.FailureDetailsHeight, detailsRow.Height.Value, "Invoking a failed row reopens the failure details.");

		var output = (TextBlock)rows[1].Children[1];
		Assert.AreEqual(Visibility.Collapsed, output.Visibility);
		StringAssert.Contains(output.Text, "Ignored on purpose");
		((IInvokeProvider)new ButtonAutomationPeer((Button)rows[1].Children[0]).GetPattern(PatternInterface.Invoke)!).Invoke();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(Visibility.Visible, output.Visibility);
		Assert.IsTrue(output.IsTextSelectionEnabled);
	}

	[TestMethod]
	public async Task When_Console_Output_Is_Captured_Failure_Shows_It()
	{
		var runner = await LoadRunner();

		await runner.RunTestsForInstance(new ConsoleFixture(), new UnitTestEngineConfig { Attempts = 1, IsConsoleOutputEnabled = true });

		Assert.AreEqual(1, runner.Failures.Count);
		StringAssert.Contains(runner.Failures[0], UnitTestsControl.ConsoleSeparator);
		StringAssert.Contains(runner.Failures[0], "failing output");

		// The passing test's output is reachable from its row.
		var passingRow = Find<StackPanel>(runner, "testResults").Children.OfType<StackPanel>()
			.Single(row => row.Children.Count > 1);
		StringAssert.Contains(((TextBlock)passingRow.Children[1]).Text, "passing output");
	}

	[TestMethod]
	public void When_Row_Output_Is_Built_It_Joins_Message_And_Console()
	{
		Assert.IsNull(UnitTestsControl.GetExpandedOutput(null, null));
		Assert.IsNull(UnitTestsControl.GetExpandedOutput(" ", "\n"));
		Assert.AreEqual("note", UnitTestsControl.GetExpandedOutput("\n note ", null));
		Assert.AreEqual($"note\n{UnitTestsControl.ConsoleSeparator}\nout", UnitTestsControl.GetExpandedOutput("note", "out\n"));
		Assert.AreEqual("C.T\nboom", UnitTestsControl.FormatFailure("C", "T", "boom", null));
		Assert.AreEqual($"T\nboom\n{UnitTestsControl.ConsoleSeparator}\nout", UnitTestsControl.FormatFailure(null, "T", "boom", "out"));
	}

	[TestMethod]
	[DataRow(700d, 0)]
	[DataRow(270d, 2)]
	public async Task When_Runner_Is_Short_Stat_Cards_Shrink(double height, int expectedMode)
	{
		var runner = await LoadRunner(height: height);

		var scroller = Find<ScrollViewer>(runner, "ShellRunHeaderScroller");
		Assert.AreEqual((UnitTestsControl.StatsMode)expectedMode, runner.CurrentStatsMode, $"Header {scroller.ExtentHeight}, max {scroller.MaxHeight}.");
		Assert.IsTrue(scroller.ScrollableHeight <= 0.5, $"The header fits without scrolling ({scroller.ScrollableHeight}).");
		Assert.AreEqual(
			expectedMode == 2 ? Visibility.Visible : Visibility.Collapsed,
			Find<TextBlock>(runner, "ShellResultsStatsSummary").Visibility);
	}

	[TestMethod]
	public async Task When_Runner_Shrinks_Stats_Step_Down_Through_Strip()
	{
		// The strip's height window moves with each platform's fonts, so walk through it rather than guess it.
		var runner = await LoadRunner(height: 700);
		var scroller = Find<ScrollViewer>(runner, "ShellRunHeaderScroller");
		List<UnitTestsControl.StatsMode> modes = new();

		for (var height = 700d; height >= 260; height -= 5)
		{
			runner.Height = height;
			runner.UpdateLayout();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(scroller.ScrollableHeight <= 0.5, $"At {height} the header scrolls ({scroller.ScrollableHeight}).");
			modes.Add(runner.CurrentStatsMode);
		}

		CollectionAssert.AreEqual(modes.OrderBy(m => m).ToList(), modes, $"Modes only get poorer: {string.Join(", ", modes.Distinct())}");
		CollectionAssert.AreEqual(
			new[] { UnitTestsControl.StatsMode.Cards, UnitTestsControl.StatsMode.Strip, UnitTestsControl.StatsMode.Hidden },
			modes.Distinct().ToArray());
	}

	[TestMethod]
	[DataRow(500d, 500d)]
	[DataRow(120d, 120d)]
	[DataRow(100d, 68d)]
	[DataRow(40d, 40d)]
	public void When_Header_Overflows_It_Is_Cut_At_A_Section(double maxHeight, double expected)
		=> Assert.AreEqual(expected, UnitTestsControl.SnapHeaderHeight(maxHeight, new[] { 60d, 112d }, 8));

	[TestMethod]
	[DataRow(152d, 354d, 100d, 166d)]
	[DataRow(300d, 354d, 100d, 300d)]
	public void When_Cards_Are_Hidden_Header_May_Grow_Until_Results_Minimum(double maxHeight, double controlHeight, double failureDetailsHeight, double expected)
		=> Assert.AreEqual(expected, UnitTestsControl.GetHeaderLastResortHeight(maxHeight, controlHeight, failureDetailsHeight));

	private static async Task<UnitTestsControl> LoadRunner(double width = 1000, double height = 700)
	{
		var embeddedRoot = TestServices.WindowHelper.EmbeddedTestRoot;
		UnitTestsControl runner;
		try
		{
			runner = new() { Width = width, Height = height };
		}
		finally
		{
			TestServices.WindowHelper.EmbeddedTestRoot = embeddedRoot;
		}

		TestServices.WindowHelper.WindowContent = runner;
		await TestServices.WindowHelper.WaitForLoaded(runner);
		await TestServices.WindowHelper.WaitForIdle();

		return runner;
	}

	private static T Find<T>(FrameworkElement root, string name) where T : class
		=> root.FindName(name) as T ?? throw new AssertFailedException($"{name} ({typeof(T).Name}) not found");

	// No [TestClass]: these only run through RunTestsForInstance, never discovered by the outer runner.
#pragma warning disable MSTEST0030 // Type containing [TestMethod] should be marked with [TestClass]
	public class SingleTestFixture
	{
		[TestMethod]
		public void Passes()
		{
		}
	}

	public class ConsoleFixture
	{
		[TestMethod]
		public void Passes() => Console.WriteLine("passing output");

		[TestMethod]
		public void Fails()
		{
			Console.WriteLine("failing output");
			Assert.Fail("Expected failure");
		}
	}

	public class MixedFixture
	{
		[TestMethod]
		public void Passes()
		{
		}

		[TestMethod]
		public void Fails() => Assert.Fail("Expected failure");

		[TestMethod]
		[DataRow(1)]
		[DataRow(2)]
		public void Rows(int value) => Assert.IsTrue(value > 0);

		[TestMethod]
		[Ignore("Ignored on purpose")]
		public void Ignored()
		{
		}
	}
#pragma warning restore MSTEST0030
}
