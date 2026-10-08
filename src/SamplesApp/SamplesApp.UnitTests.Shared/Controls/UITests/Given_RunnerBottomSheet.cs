#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Tests;
using Windows.Foundation;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_RunnerBottomSheet
{
	private const double PhoneWidth = 360;
	private const double PhoneHeight = 700;

	[TestMethod]
	[DataRow(360d, 700d, true)]
	[DataRow(599d, 800d, true)]
	[DataRow(600d, 800d, false)]
	[DataRow(500d, 400d, false)]
	[DataRow(1000d, 700d, false)]
	[DataRow(0d, 700d, false)]
	public void When_Size_Is_Narrow_Portrait_Sheet_Is_Used(double width, double height, bool expected)
		=> Assert.AreEqual(expected, UnitTestsControl.UsesBottomSheet(width, height));

	[TestMethod]
	[DataRow(100d, 400d, 0d, true)]
	[DataRow(300d, 400d, 0d, false)]
	[DataRow(300d, 400d, -1d, true)]
	[DataRow(50d, 400d, 1d, false)]
	public void When_Drag_Ends_Sheet_Snaps(double offset, double collapsedOffset, double velocity, bool expanded)
		=> Assert.AreEqual(expanded, UnitTestsControl.GetSheetSnapExpanded(offset, collapsedOffset, velocity));

	[TestMethod]
	public void When_Formatting_Sheet_Status_Text_Is_One_Line()
	{
		Assert.AreEqual("Ready", UnitTestsControl.FormatSheetStatus(false, 0, 0, null));
		Assert.AreEqual("Running", UnitTestsControl.FormatSheetStatus(true, 3, 0, null));
		Assert.AreEqual($"Running {74:N0}/{157:N0}", UnitTestsControl.FormatSheetStatus(true, 74, 157, null));
		Assert.AreEqual("3 failed", UnitTestsControl.FormatSheetStatus(false, 157, 157, "3 failed"));
	}

	[TestMethod]
	public async Task When_Narrow_Portrait_Host_Fills_Width_Above_Collapsed_Bar()
	{
		var runner = await LoadRunner();
		var host = Find<ContentControl>(runner, "unitTestContentRoot");
		var bar = Find<FrameworkElement>(runner, "ShellSheetBar");
		host.Content = new Border { Height = 5000 };
		await TestServices.WindowHelper.WaitForIdle();
		runner.UpdateLayout(); // WinUI may not have laid out the new content after WaitForIdle.

		Assert.IsTrue(runner.IsBottomSheet);
		Assert.IsFalse(runner.IsSheetExpanded);
		Assert.AreEqual(Visibility.Visible, bar.Visibility);
		Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(runner, "ShellColumnSplitter").Visibility);

		var hostBounds = GetBounds(host, runner);
		var barBounds = GetBounds(bar, runner);
		Assert.AreEqual(0, hostBounds.X, 0.5);
		Assert.AreEqual(0, hostBounds.Y, 0.5);
		Assert.AreEqual(PhoneWidth, hostBounds.Width, 0.5, "The test host gets the whole width.");
		Assert.IsTrue(barBounds.Height >= UnitTestsControl.TouchTargetSize, $"Bar height {barBounds.Height}.");
		Assert.AreEqual(PhoneHeight, barBounds.Bottom, 0.5, "The collapsed bar sits at the bottom.");
		Assert.AreEqual(barBounds.Y, hostBounds.Bottom, 0.5, "The host ends where the bar starts, never under it.");
	}

	[TestMethod]
	public async Task When_Handle_Invoked_Sheet_Expands_And_Collapses()
	{
		var runner = await LoadRunner();
		var handle = Find<ShellSheetHandle>(runner, "ShellSheetHandle");
		var sheet = Find<FrameworkElement>(runner, "ShellSheet");
		var body = Find<Control>(runner, "ShellSheetBody");
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(handle);
		var expandCollapse = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;

		Assert.AreEqual(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
		Assert.IsFalse(body.IsEnabled, "A collapsed sheet keeps its content out of the tab order.");

		((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)!).Invoke();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(runner.IsSheetExpanded);
		Assert.AreEqual(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);
		Assert.IsTrue(body.IsEnabled);
		var expandedTop = GetBounds(sheet, runner).Y;
		Assert.AreEqual(PhoneHeight * (1 - UnitTestsControl.SheetExpandedRatio), expandedTop, 1, "The sheet covers about three quarters.");

		expandCollapse.Collapse();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(runner.IsSheetExpanded);
		Assert.AreEqual(ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);
		Assert.AreEqual(GetBounds(Find<FrameworkElement>(runner, "ShellSheetBar"), runner).Y, GetBounds(sheet, runner).Y, 0.5);

		expandCollapse.Expand();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(runner.IsSheetExpanded);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // KeyboardHelper sends nothing on WinAppSDK.
	public async Task When_Escape_Pressed_Expanded_Sheet_Collapses()
	{
		var runner = await LoadRunner();
		runner.SetSheetExpanded(true);
		await TestServices.WindowHelper.WaitForIdle();

		var filter = Find<TextBox>(runner, "testFilter");
		filter.Focus(FocusState.Keyboard);
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.KeyboardHelper.Escape();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(runner.IsSheetExpanded);
		Assert.AreSame(Find<ShellSheetHandle>(runner, "ShellSheetHandle"), FocusManager.GetFocusedElement(runner.XamlRoot!), "Focus moves to the handle.");
	}

	[TestMethod]
	public async Task When_Animated_Sheet_Settles_At_Its_Position()
	{
		var runner = await LoadRunner();
		runner.IsSheetAnimationEnabled = true;
		var sheet = Find<FrameworkElement>(runner, "ShellSheet");

		runner.SetSheetExpanded(true);

		var expectedTop = PhoneHeight * (1 - UnitTestsControl.SheetExpandedRatio);
		var waited = Stopwatch.StartNew();
		while (Math.Abs(GetBounds(sheet, runner).Y - expectedTop) > 1 && waited.ElapsedMilliseconds < 3000)
		{
			await Task.Delay(50);
		}

		Assert.AreEqual(expectedTop, GetBounds(sheet, runner).Y, 1);
		Assert.IsTrue(Find<Control>(runner, "ShellSheetBody").IsEnabled);
	}

	[TestMethod]
	public async Task When_Run_Starts_Sheet_Collapses_And_Hint_Shows_While_Expanded()
	{
		var runner = await LoadRunner();
		var hint = Find<FrameworkElement>(runner, "ShellSheetRunHint");
		runner.SetSheetExpanded(true);
		Assert.AreEqual(Visibility.Collapsed, hint.Visibility, "No hint while idle.");

		var run = runner.RunTestsForInstance(new SlowFixture(), new UnitTestEngineConfig { Attempts = 1 });

		Assert.IsFalse(runner.IsSheetExpanded, "Starting a run collapses the sheet, so injected input never hits it.");

		runner.SetSheetExpanded(true);
		Assert.AreEqual(Visibility.Visible, hint.Visibility, "Expanding mid-run warns that the run may be affected.");

		await run;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(Visibility.Collapsed, hint.Visibility);
		Assert.AreEqual("Passed", Find<TextBlock>(runner, "ShellSheetStatus").Text);
		Assert.AreEqual(0d, Find<ProgressBar>(runner, "ShellSheetProgress").Opacity);
	}

	[TestMethod]
	public async Task When_Run_Fails_Bar_Shows_Outcome_And_Counters()
	{
		var runner = await LoadRunner();

		await runner.RunTestsForInstance(new Given_UnitTestsControl.MixedFixture(), new UnitTestEngineConfig { Attempts = 1 });
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("1 failed", Find<TextBlock>(runner, "ShellSheetStatus").Text);
		var stats = Find<Grid>(runner, "ShellRunStats");
		Assert.AreSame(Find<Grid>(runner, "ShellSheetBar"), stats.Parent, "The counters are chips in the bar.");
		Assert.AreEqual("Failed: 1", AutomationProperties.GetName(Find<TextBlock>(runner, "failedTestCount")));
		Assert.AreEqual(Visibility.Collapsed, Find<TextBlock>(runner, "ShellPassedLabel").Visibility);
		Assert.IsTrue(stats.Children.All(c => Grid.GetRow((FrameworkElement)c) == 0), "One row of chips.");

		// Result rows stay on one line in the sheet: plain rows are Grids, rows with details wrap one in a Button.
		var names = Find<StackPanel>(runner, "testResults").Children
			.Select(c => c as Grid ?? ((c as StackPanel)?.Children.FirstOrDefault() as Button)?.Content as Grid)
			.OfType<Grid>()
			.Select(row => row.Children.OfType<TextBlock>().First(t => Grid.GetColumn(t) == 1))
			.ToArray();
		Assert.AreEqual(5, names.Length);
		Assert.IsTrue(names.All(n => n.MaxLines == 1 && n.TextWrapping == TextWrapping.NoWrap));
	}

	[TestMethod]
	public async Task When_Automation_Is_Active_Sheet_Stays_Collapsed()
	{
		var runner = await LoadRunner();
		runner.IsAutomationActive = () => true;

		runner.SetSheetExpanded(true);
		Assert.IsFalse(runner.IsSheetExpanded);

		runner.IsAutomationActive = () => false;
		runner.SetSheetExpanded(true);
		Assert.IsTrue(runner.IsSheetExpanded);

		runner.IsRunningOnCI = true;
		Assert.IsFalse(runner.IsSheetExpanded, "CI collapses an open sheet.");
		runner.SetSheetExpanded(true);
		Assert.IsFalse(runner.IsSheetExpanded);
	}

	[TestMethod]
	public async Task When_Sheet_Is_Used_Named_Elements_Stay_Present_And_Return()
	{
		var runner = await LoadRunner();

		foreach (var name in new[] { "runTestCount", "ignoredTestCount", "failedTests", "failedTestDetails", "runningState", "testResults", "testResultsScroller", "runButton", "stopButton", "testFilter", "unitTestContentRoot", "UnitTestsRootControl", "ShellRowSplitter", "ShellColumnSplitter", "succeededTestCount", "failedTestCount", "inconclusiveTestCount", "retriedTestCount" })
		{
			Assert.IsNotNull(runner.FindName(name), name);
		}

		foreach (var name in new[] { "failedTests", "runningState", "failedTestDetails" })
		{
			Assert.AreEqual(Visibility.Visible, Find<TextBlock>(runner, name).Visibility, name);
		}

		var runButton = Find<Button>(runner, "runButton");
		Assert.AreSame(Find<Grid>(runner, "ShellSheetBar"), runButton.Parent, "Run stays reachable on the collapsed bar.");
		Assert.IsTrue(runButton.IsEnabled);

		// Rotating to landscape brings everything back to the side-by-side places.
		runner.Width = PhoneHeight;
		runner.Height = PhoneWidth;
		await TestServices.WindowHelper.WaitForIdle();
		runner.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		var toolbar = Find<Grid>(runner, "ShellRunToolbar");
		Assert.IsFalse(runner.IsBottomSheet);
		Assert.AreSame(toolbar, runButton.Parent);
		Assert.AreEqual(0, toolbar.Children.IndexOf(runButton));
		Assert.AreEqual(1, toolbar.Children.IndexOf(Find<Button>(runner, "stopButton")));
		Assert.AreSame(Find<StackPanel>(runner, "ShellRunHeaderPanel"), Find<Grid>(runner, "ShellRunStats").Parent);
		Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(runner, "ShellSheetBar").Visibility);
		Assert.IsTrue(Find<Control>(runner, "ShellSheetBody").IsEnabled);
		Assert.IsNull(Find<TextBlock>(runner, "failedTests").ReadLocalValue(TextBlock.ForegroundProperty) as Brush, "The hidden texts get their normal foreground back.");
	}

	[TestMethod]
	[DataRow(1000d, 700d)]
	[DataRow(800d, 400d)]
	[DataRow(500d, 400d)]
	public async Task When_Not_Narrow_Portrait_Layout_Is_Side_By_Side(double width, double height)
	{
		var runner = await LoadRunner(width, height);
		var host = Find<ContentControl>(runner, "unitTestContentRoot");
		host.Content = new Border { Height = 100 };
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(runner.IsBottomSheet);
		Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(runner, "ShellSheetBar").Visibility);

		// Two equal columns around the 5 px splitter, as before the sheet existed.
		var column = (width - 5) / 2;
		var bounds = GetBounds(host, runner);
		Assert.AreEqual(column + 5, bounds.X, 0.5);
		Assert.AreEqual(0, bounds.Y, 0.5);
		Assert.AreEqual(column, bounds.Width, 0.5);
		Assert.AreEqual(column, Find<FrameworkElement>(runner, "ShellSheet").ActualWidth, 0.5);
	}

	[TestMethod]
	public void When_Shell_Runner_Hosts_Tests_Host_Follows_Its_Layout()
	{
		if (TestServices.WindowHelper.EmbeddedTestRoot.control is not FrameworkElement { XamlRoot: not null } host
			|| FindAncestor<UnitTestsControl>(host) is not { } runner)
		{
			Assert.Inconclusive("Only meaningful inside the in-app runner.");
			return;
		}

		var hostBounds = GetBounds(host, runner);
		if (runner.IsBottomSheet)
		{
			Assert.AreEqual(0, hostBounds.X, 0.5);
			Assert.AreEqual(runner.ActualWidth, hostBounds.Width, 0.5);
			Assert.IsFalse(runner.IsSheetExpanded, "Automation keeps the sheet collapsed.");
		}
		else
		{
			var column = (runner.ActualWidth - 5) / 2;
			Assert.AreEqual(column + 5, hostBounds.X, 1, $"Host at {hostBounds}, runner {runner.ActualWidth}x{runner.ActualHeight}.");
			Assert.AreEqual(column, hostBounds.Width, 1);
		}

		Assert.AreEqual(0, hostBounds.Y, 0.5);
	}

	[TestMethod]
	public async Task When_Sheet_Collapses_Focus_Leaves_Its_Content()
	{
		var runner = await LoadRunner();
		var handle = Find<ShellSheetHandle>(runner, "ShellSheetHandle");
		runner.SetSheetExpanded(true);
		await TestServices.WindowHelper.WaitForIdle();

		var filter = Find<TextBox>(runner, "testFilter");
		Assert.IsTrue(filter.Focus(FocusState.Programmatic));
		await TestServices.WindowHelper.WaitForIdle();

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(handle);
		((IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!).Collapse();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(runner.IsSheetExpanded);
		Assert.AreSame(handle, FocusManager.GetFocusedElement(runner.XamlRoot!), "Focus moves to the handle, not onto hidden, disabled content.");
	}

	[TestMethod]
	public async Task When_Sheet_Is_Collapsed_Its_Content_Is_Hidden_But_Automation_Texts_Stay()
	{
		var runner = await LoadRunner();
		var hidden = new[] { "ShellRunHeaderScroller", "ShellFailureLayer", "ShellResultsLayer", "ShellRowSplitter" };

		foreach (var name in hidden)
		{
			Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(runner, name).Visibility, name);
		}

		foreach (var name in new[] { "failedTests", "runningState", "failedTestDetails", "ShellRunStats" })
		{
			Assert.AreEqual(Visibility.Visible, Find<FrameworkElement>(runner, name).Visibility, name);
		}

		runner.SetSheetExpanded(true);
		await TestServices.WindowHelper.WaitForIdle();

		foreach (var name in hidden.Take(3))
		{
			Assert.AreEqual(Visibility.Visible, Find<FrameworkElement>(runner, name).Visibility, name);
		}

		Assert.AreEqual(Visibility.Collapsed, Find<FrameworkElement>(runner, "ShellRowSplitter").Visibility, "Nothing to split without failure details.");
	}

	[TestMethod]
	public async Task When_Automation_Starts_While_Expanded_Sheet_Collapses_And_Handle_Is_Disabled()
	{
		var runner = await LoadRunner();
		var handle = Find<ShellSheetHandle>(runner, "ShellSheetHandle");
		runner.SetSheetExpanded(true);
		Assert.IsTrue(handle.IsEnabled);

		runner.IsAutomationActive = () => true;

		Assert.IsFalse(runner.IsSheetExpanded);
		Assert.IsFalse(handle.IsEnabled, "A locked sheet does not offer to expand.");

		runner.IsAutomationActive = () => false;
		Assert.IsTrue(handle.IsEnabled);
	}

	[TestMethod]
	public async Task When_Expanded_With_Failures_Results_Show_Whole_Rows()
	{
		var runner = await LoadRunner();
		await runner.RunTestsForInstance(new Given_UnitTestsControl.MixedFixture(), new UnitTestEngineConfig { Attempts = 1 });
		runner.SetSheetExpanded(true);
		await TestServices.WindowHelper.WaitForIdle();
		runner.UpdateLayout();

		var scroller = Find<ScrollViewer>(runner, "testResultsScroller");
		Assert.IsTrue(Find<FrameworkElement>(runner, "ShellFailureLayer").ActualHeight > 0, "Failure details are open.");
		Assert.IsTrue(
			scroller.ViewportHeight >= UnitTestsControl.SheetMinResultRows * UnitTestsControl.ResultRowHeight,
			$"Results viewport {scroller.ViewportHeight} shows fewer than {UnitTestsControl.SheetMinResultRows} rows.");
		Assert.AreEqual(Visibility.Visible, Find<FrameworkElement>(runner, "ShellRowSplitter").Visibility);
	}

	[TestMethod]
	public async Task When_Collapsed_Bar_Is_A_Compact_Peek()
	{
		var runner = await LoadRunner();
		var bar = Find<FrameworkElement>(runner, "ShellSheetBar");
		var status = Find<FrameworkElement>(runner, "ShellSheetStatus");
		var stats = Find<FrameworkElement>(runner, "ShellRunStats");
		var run = Find<FrameworkElement>(runner, "runButton");
		var handle = Find<FrameworkElement>(runner, "ShellSheetHandle");

		Assert.IsTrue(bar.ActualHeight <= 96, $"Bar height {bar.ActualHeight}, chips {stats.ActualHeight}.");

		var runBounds = GetBounds(run, runner);
		var runCenter = runBounds.Y + runBounds.Height / 2;
		Assert.AreEqual(runCenter, Center(GetBounds(status, runner)), 1, "The status is centred with Run and Stop.");
		Assert.AreEqual(runCenter, Center(GetBounds(handle, runner)), 1, "The handle is centred with Run and Stop.");
		Assert.AreEqual(runBounds.X, GetBounds(stats, runner).X, 0.5, "The chips start under Run.");
		Assert.IsTrue(GetBounds(stats, runner).Y >= runBounds.Bottom, "The chips sit below the buttons.");
		Assert.IsTrue(GetBounds(stats, runner).Right >= runner.ActualWidth - 16, "The chips use the bar's width.");

		static double Center(Rect bounds) => bounds.Y + bounds.Height / 2;
	}

#if HAS_UNO
	[TestMethod]
	public async Task When_Touch_Filter_Reaches_Target_Height_With_Centred_Text()
	{
		var vm = SampleControl.Presentation.SampleChooserViewModel.Instance;
		if (vm is null)
		{
			Assert.Inconclusive("Needs the SamplesApp shell.");
		}

		var wasTouch = vm.SimulateTouch;
		try
		{
			vm.SimulateTouch = true;
			var runner = await LoadRunner();
			var filter = Find<TextBox>(runner, "testFilter");

			// The TextBox template pins its text to the top padding, so extra height must come from even padding, not MinHeight.
			Assert.IsTrue(filter.MinHeight < UnitTestsControl.TouchTargetSize, $"A MinHeight of {filter.MinHeight} would leave the text above centre.");
			Assert.IsTrue(filter.ActualHeight >= UnitTestsControl.TouchTargetSize - 0.5, $"Filter height {filter.ActualHeight}.");
			Assert.AreEqual(filter.Padding.Top, filter.Padding.Bottom, 1, "Text sits in the middle of the box.");
		}
		finally
		{
			vm.SimulateTouch = wasTouch;
		}
	}
#endif

	[TestMethod]
	public async Task When_Window_Turns_Wide_During_A_Run_Layout_Waits_For_The_End()
	{
		var runner = await LoadRunner();
		var run = runner.RunTestsForInstance(new SlowFixture(), new UnitTestEngineConfig { Attempts = 1 });

		runner.Width = PhoneHeight;
		runner.Height = PhoneWidth;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(runner.IsBottomSheet, "The test host does not move under a running test.");

		await run;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsFalse(runner.IsBottomSheet);
	}

#if HAS_UNO
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Sheet_Dragged_It_Follows_The_Pointer(bool useMouse)
	{
		var runner = await LoadReachableRunner();
		var sheet = Find<FrameworkElement>(runner, "ShellSheet");
		var start = GetWindowCenter(Find<FrameworkElement>(runner, "ShellSheetStatus"));
		var top = GetBounds(sheet, runner).Y;

		var pointer = CreatePointer(useMouse);
		using var disposePointer = (IDisposable)pointer;
		pointer.Press(start);
		pointer.MoveTo(new Point(start.X, start.Y - 100), steps: 10);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(top - 100, GetBounds(sheet, runner).Y, 6, "The sheet tracks the pointer 1:1.");

		pointer.Release();
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[DataRow("ShellSheetHandle", false)]
	[DataRow("ShellSheetHandle", true)]
	[DataRow("runButton", false)]
	[DataRow("runButton", true)]
	public async Task When_Drag_Starts_On_A_Button_It_Does_Not_Click(string buttonName, bool useMouse)
	{
		var runner = await LoadReachableRunner();
		Find<TextBox>(runner, "testFilter").Text = "No_Such_Test_For_The_Sheet_Drag"; // A wrong run would be harmless.
		var start = GetWindowCenter(Find<FrameworkElement>(runner, buttonName));

		var pointer = CreatePointer(useMouse);
		using (pointer as IDisposable)
		{
			pointer.Press(start);
			pointer.MoveTo(new Point(start.X, start.Y - runner.ActualHeight * 0.6), steps: 20);
			pointer.Release();
		}

		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(100);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(runner.IsSheetExpanded, "A drag up past half way expands the sheet and stays expanded.");
		Assert.IsFalse(runner.IsSheetDragging);
		Assert.AreEqual("Ready", Find<TextBlock>(runner, "ShellSheetStatus").Text, "No run started.");
		Assert.IsFalse(Find<Button>(runner, "stopButton").IsEnabled);
	}

	// Injected input goes to whatever is on screen: inside a phone-sized runner the test host is shorter than PhoneHeight,
	// and the outer runner's own bar covers its bottom.
	private static async Task<UnitTestsControl> LoadReachableRunner()
	{
		var height = PhoneHeight;
		var diagnostics = "";
		if (TestServices.WindowHelper.EmbeddedTestRoot.control is FrameworkElement { XamlRoot: { } root } host)
		{
			var top = host.TransformToVisual(null).TransformPoint(default).Y;
			var bottom = root.Size.Height;
			if (FindAncestor<UnitTestsControl>(host) is { IsBottomSheet: true } outer)
			{
				outer.SetSheetExpanded(false);
				await Task.Delay(400); // The collapse may animate.
				await TestServices.WindowHelper.WaitForIdle();
				bottom = outer.SheetBar.TransformToVisual(null).TransformPoint(default).Y;
			}

			height = Math.Min(PhoneHeight, Math.Floor(bottom - top));
			diagnostics = $" (host top {top}, bottom {bottom}, window {root.Size})";
		}

		if (!UnitTestsControl.UsesBottomSheet(PhoneWidth, height) || height < 400)
		{
			Assert.Inconclusive($"Only {height} DIP of the test host are reachable by input{diagnostics}.");
		}

		return await LoadRunner(PhoneWidth, height);
	}

	private static Uno.UI.DevTools.Input.IInjectedPointer CreatePointer(bool useMouse)
	{
		var injector = Windows.UI.Input.Preview.Injection.InputInjector.TryCreate() ?? throw new InvalidOperationException("No InputInjector.");
		return Uno.UI.DevTools.Input.InputInjectorExtensions.GetPointer(injector, useMouse ? Microsoft.UI.Input.PointerDeviceType.Mouse : Microsoft.UI.Input.PointerDeviceType.Touch);
	}

	private static Point GetWindowCenter(FrameworkElement element)
		=> element.TransformToVisual(null).TransformPoint(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
#endif

	private static async Task<UnitTestsControl> LoadRunner(double width = PhoneWidth, double height = PhoneHeight)
	{
		var embeddedRoot = TestServices.WindowHelper.EmbeddedTestRoot;
		UnitTestsControl runner;
		try
		{
			runner = new()
			{
				Width = width,
				Height = height,
				IsAutomationActive = () => false,
				IsSheetAnimationEnabled = false,
			};
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

	private static Rect GetBounds(FrameworkElement element, UIElement root)
		=> element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));

	private static T? FindAncestor<T>(DependencyObject element) where T : class
	{
		for (var current = VisualTreeHelper.GetParent(element); current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (current is T match)
			{
				return match;
			}
		}

		return null;
	}

	private static T Find<T>(FrameworkElement root, string name) where T : class
		=> root.FindName(name) as T ?? throw new AssertFailedException($"{name} ({typeof(T).Name}) not found");

	// No [TestClass]: only run through RunTestsForInstance.
	public class SlowFixture
	{
		[TestMethod]
		public async Task Waits() => await Task.Delay(500);
	}
}
