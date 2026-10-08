#nullable enable

using System;
using System.Threading.Tasks;
using Benchmarks.Shared.Controls;
using BenchmarkDotNet.Loggers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.Samples.Helper;

namespace SamplesApp.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_BenchmarkDotNetControl
{
	[TestMethod]
	public async Task When_Loaded_Keeps_Automation_Contract()
	{
		var (_, control) = await Load(1000, 700);

		Assert.AreEqual("benchmarkControl", control.Name);
		Assert.AreEqual("Not initialized", Find<TextBlock>(control, "runStatus").Text);
		Assert.AreEqual("", Find<TextBlock>(control, "runCount").Text);
		Assert.AreEqual("", control.ResultsAsBase64);
		Assert.IsTrue(Find<Button>(control, "runButton").IsEnabled);
		Assert.IsFalse(Find<Button>(control, "downloadResults").IsEnabled);
		Assert.IsNotNull(Find<CheckBox>(control, "debugLog"));
		Assert.IsNotNull(Find<TextBlock>(control, "runLogs"));
		Assert.IsNotNull(Find<ContentControl>(control, "testHost"));
		Assert.IsFalse(Find<ProgressRing>(control, "ShellBenchProgress").IsActive);
	}

	[TestMethod]
	[DataRow(1000, 0, 0)]
	[DataRow(360, 2, 1)]
	public async Task When_Width_Changes_Controls_Reflow(double width, int expectedRunRow, int expectedHostRow)
	{
		var (host, control) = await Load(width, 740);

		var run = Find<Button>(control, "runButton");
		Assert.AreEqual(expectedRunRow, Grid.GetRow(run));
		Assert.AreEqual(expectedHostRow, Grid.GetRow(Find<Border>(control, "ShellBenchHostCard")));

		var bounds = host.ActualWidth;
		foreach (var name in new[] { "ShellBenchFilter", "debugLog", "runButton", "downloadResults", "runStatus", "ShellBenchLogCard", "ShellBenchHostCard" })
		{
			var element = Find<FrameworkElement>(control, name);
			var rect = element.TransformToVisual(host).TransformBounds(new(0, 0, element.ActualWidth, element.ActualHeight));
			Assert.IsTrue(rect.Left >= 0 && rect.Right <= bounds + 0.5, $"{name} spills out horizontally at width {width}: {rect}");
		}

		var isTouch = ShellFunctions.IsTouchShell;
		if (width < 600 || isTouch)
		{
			Assert.IsTrue(run.ActualHeight >= 40, $"Run button is {run.ActualHeight} tall at width {width}");
			Assert.IsTrue(Find<Button>(control, "downloadResults").MinHeight >= 40);
		}

		// A taller CheckBox misaligns the Fluent template's box and label unless the label is centred too.
		var debugLog = Find<CheckBox>(control, "debugLog");
		if (!isTouch)
		{
			Assert.AreEqual(DependencyProperty.UnsetValue, debugLog.ReadLocalValue(FrameworkElement.MinHeightProperty));
		}
	}

	[TestMethod]
	[DataRow(1000)]
	[DataRow(360)]
	public async Task When_Idle_Status_Starts_At_Card_Content_Edge(double width)
	{
		var (_, control) = await Load(width, 740);

		var statusGrid = Find<Grid>(control, "ShellBenchStatusGrid");
		var status = Find<TextBlock>(control, "runStatus");
		var origin = status.TransformToVisual(statusGrid).TransformPoint(new(0, 0));

		Assert.AreEqual(0, origin.X, 0.5, "The collapsed progress ring must not reserve space");
	}

	[TestMethod]
	public async Task When_Narrow_Test_Host_Gets_Finite_Height()
	{
		var (_, control) = await Load(360, 740);

		var output = Find<Grid>(control, "ShellBenchOutput");
		Assert.IsTrue(output.RowDefinitions[1].Height.IsAbsolute, "The host row must not be Auto (benchmarks would measure with infinite height)");
		Assert.AreEqual(BenchmarkDotNetControl.NarrowHostHeight, Find<Border>(control, "ShellBenchHostCard").ActualHeight, 0.5);
	}

	[TestMethod]
	public async Task When_ShowHeader_False_Title_Hides_And_Caption_Stays()
	{
		var (_, control) = await Load(1000, 700);

		control.ShowHeader = false;

		Assert.AreEqual(Visibility.Collapsed, Find<TextBlock>(control, "ShellBenchTitle").Visibility);
		Assert.AreEqual(Visibility.Visible, Find<StackPanel>(control, "ShellBenchTitleHost").Visibility);
	}

	[TestMethod]
	public async Task When_Output_Does_Not_Fit_Page_Scrolls()
	{
		var (_, control) = await Load(360, 300);

		var scroller = Find<ScrollViewer>(control, "ShellBenchScroller");
		Assert.IsTrue(scroller.ScrollableHeight > 0, "The page should scroll when the viewport is too short");
		Assert.IsTrue(Find<Border>(control, "ShellBenchLogCard").ActualHeight >= 200);
	}

	[TestMethod]
	public async Task When_Test_Host_Content_Changes_Page_Does_Not_React()
	{
		var (_, control) = await Load(1000, 700);
		var hint = Find<TextBlock>(control, "ShellBenchHostEmpty");
		var testHost = Find<ContentControl>(control, "testHost");

		// Benchmarks swap testHost.Content inside their measured loops.
		Assert.AreEqual(HorizontalAlignment.Left, testHost.HorizontalContentAlignment);
		Assert.AreEqual(VerticalAlignment.Top, testHost.VerticalContentAlignment);
		Assert.AreEqual(Visibility.Visible, hint.Visibility);

		testHost.Content = new Border();
		Assert.AreEqual(Visibility.Visible, hint.Visibility);

		testHost.Content = null;
		Assert.AreEqual(Visibility.Visible, hint.Visibility);
	}

	[TestMethod]
	public async Task When_Idle_Log_Shows_Empty_State()
	{
		var (_, control) = await Load(1000, 700);

		Assert.AreEqual(Visibility.Visible, Find<TextBlock>(control, "ShellBenchLogEmpty").Visibility);
		Assert.AreEqual("Benchmark log", AutomationProperties.GetName(Find<ScrollViewer>(control, "ShellBenchLogScroller")));
		Assert.IsTrue(string.IsNullOrEmpty(AutomationProperties.GetName(Find<TextBlock>(control, "runLogs"))), "A name would hide the log text");
		Assert.IsTrue(string.IsNullOrEmpty(AutomationProperties.GetName(Find<TextBlock>(control, "runCount"))), "A fixed name would hide the count");
	}

	[TestMethod]
	public async Task When_Filter_Matches_Nothing_Status_Explains()
	{
		var (_, control) = await Load(1000, 700);
		control.ClassFilter = "NoSuchBenchmarkClass";

		await control.Run();

		Assert.AreEqual("No benchmarks match \"NoSuchBenchmarkClass\"", Find<TextBlock>(control, "runStatus").Text);
		Assert.AreEqual("0", Find<TextBlock>(control, "runCount").Text);
		Assert.AreEqual(0, Find<TextBlock>(control, "runLogs").Inlines.Count, "No error should be logged");
		Assert.AreEqual("", control.ResultsAsBase64);
		Assert.IsFalse(Find<Button>(control, "downloadResults").IsEnabled);
		Assert.IsTrue(Find<Button>(control, "runButton").IsEnabled);
	}

	[TestMethod]
	public void When_Log_Kind_Is_Hint_It_Is_Not_A_Warning()
		=> Assert.AreEqual("TextFillColorSecondaryBrush", BenchmarkDotNetControl.GetLogBrushKey(LogKind.Hint));

	[TestMethod]
	[DataRow(LogKind.Default)]
	[DataRow(LogKind.Help)]
	[DataRow(LogKind.Header)]
	[DataRow(LogKind.Result)]
	[DataRow(LogKind.Statistic)]
	[DataRow(LogKind.Info)]
	[DataRow(LogKind.Error)]
	[DataRow(LogKind.Hint)]
	public void When_Log_Kind_Is_Themed_Brush_Resolves_Per_Theme(LogKind kind)
	{
		var key = BenchmarkDotNetControl.GetLogBrushKey(kind);

		var light = ShellThemeBrushes.Get(key, ElementTheme.Light) as SolidColorBrush;
		var dark = ShellThemeBrushes.Get(key, ElementTheme.Dark) as SolidColorBrush;

		Assert.IsNotNull(light, $"{kind} -> {key} has no light brush");
		Assert.IsNotNull(dark, $"{kind} -> {key} has no dark brush");
	}

	[TestMethod]
	public void When_Log_Kinds_Differ_Errors_Stand_Out()
	{
		var error = BenchmarkDotNetControl.GetLogBrushKey(LogKind.Error);
		Assert.AreNotEqual(error, BenchmarkDotNetControl.GetLogBrushKey(LogKind.Info));
		Assert.AreNotEqual(error, BenchmarkDotNetControl.GetLogBrushKey(LogKind.Result));
		Assert.AreNotEqual(BenchmarkDotNetControl.GetLogBrushKey(LogKind.Header), BenchmarkDotNetControl.GetLogBrushKey(LogKind.Info));
	}

	private static async Task<(Border Host, BenchmarkDotNetControl Control)> Load(double width, double height)
	{
		BenchmarkDotNetControl control = new();
		Border host = new() { Width = width, Height = height, Child = control };
		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(control);
		await TestServices.WindowHelper.WaitForIdle();
		return (host, control);
	}

	private static T Find<T>(BenchmarkDotNetControl control, string name) where T : class
		=> (control.FindName(name) as T) ?? throw new AssertFailedException($"{name} is missing.");
}
