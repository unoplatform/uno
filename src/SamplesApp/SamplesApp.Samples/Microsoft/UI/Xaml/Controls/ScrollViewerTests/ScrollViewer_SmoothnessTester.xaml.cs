#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests;

[Sample("Scrolling", Name = "ScrollSmoothnessTester", IsManualTest = true,
	Description = "Stress-tests scroll smoothness with many 300x300 colored rectangles in ScrollViewer and ScrollView hosts. " +
	"Run replays a synthetic wheel/touchpad/touch/keyboard/ChangeView gesture and reports frame pacing and motion judder. " +
	"Launch with 'sample=Scrolling/ScrollSmoothnessTester&scrollprobe=all' (or a comma list of scenarios, plus &scrolltabs=0,4) to run unattended; " +
	"results are printed as '[scroll-probe] {json}' lines.")]
public sealed partial class ScrollViewer_SmoothnessTester : Page, INotifyPropertyChanged
{
	private const int ItemCount = 200;

	private readonly List<TextBlock> _stackPanelIndexLabels = new();
	private readonly List<string> _jsonResults = new();

	public sealed record RectangleItem(int Index, Brush Brush);

	public event PropertyChangedEventHandler? PropertyChanged;

	private Visibility _indexLabelVisibility = Visibility.Collapsed;

	// Collapsed by default: this sample is about scroll smoothness, so the initial UI stays as
	// lightweight as possible. The toggle lets you superpose the index labels on demand.
	public Visibility IndexLabelVisibility
	{
		get => _indexLabelVisibility;
		private set
		{
			_indexLabelVisibility = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IndexLabelVisibility)));
		}
	}

#if HAS_UNO
	private ScrollSmoothnessProbe? _manualProbe;
	private static bool _consoleTrace;
	private bool _isRunning;
	private bool _holdResults;
	private CancellationTokenSource? _runCts;
#endif

	public ScrollViewer_SmoothnessTester()
	{
		this.InitializeComponent();

		var random = new Random(1234567890);

		for (var i = 0; i < ItemCount; i++)
		{
			VerticalStack.Children.Add(CreateRectangleWithIndex(i, random));
			HorizontalStack.Children.Add(CreateRectangleWithIndex(i, random));
			ScrollViewStack.Children.Add(CreateRectangleWithIndex(i, random));
		}

		ListViewHost.ItemsSource = Enumerable.Range(0, ItemCount).Select(i => new RectangleItem(i, RandomBrush(random))).ToList();
		ItemsRepeaterHost.ItemsSource = Enumerable.Range(0, ItemCount).Select(i => new RectangleItem(i, RandomBrush(random))).ToList();
		ScrollViewRepeaterHost.ItemsSource = Enumerable.Range(0, ItemCount).Select(i => new RectangleItem(i, RandomBrush(random))).ToList();

#if HAS_UNO
		ScenarioPicker.ItemsSource = ScrollSmoothnessDrivers.Scenarios;
		ScenarioPicker.SelectedIndex = 0;
		Loaded += OnLoaded;
		Unloaded += (_, _) => _runCts?.Cancel();
#else
		ScenarioPicker.IsEnabled = RunScenarioButton.IsEnabled = RunAllButton.IsEnabled = RecordToggle.IsEnabled = false;
		ResultsText.Text = "The frame probe needs Uno Platform internals; it is not available on this head.";
#endif
	}

	private void ShowIndexToggle_Click(object sender, RoutedEventArgs e)
	{
		IndexLabelVisibility = ShowIndexToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

		foreach (var label in _stackPanelIndexLabels)
		{
			label.Visibility = IndexLabelVisibility;
		}
	}

	private UIElement CreateRectangleWithIndex(int index, Random random)
	{
		var label = new TextBlock
		{
			Text = index.ToString(),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = 24,
			Foreground = new SolidColorBrush(Colors.White),
			Visibility = IndexLabelVisibility,
		};
		_stackPanelIndexLabels.Add(label);

		return new Grid
		{
			Width = 300,
			Height = 300,
			Children =
			{
				new Rectangle
				{
					Fill = RandomBrush(random)
				},
				label
			}
		};
	}

	private static SolidColorBrush RandomBrush(Random random) =>
		new(Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));

	private void CopyResults_Click(object sender, RoutedEventArgs e)
	{
		var package = new DataPackage();
		package.SetText("[" + string.Join(",\n", _jsonResults) + "]");
		Clipboard.SetContent(package);
	}

#if HAS_UNO
	private static string Platform
		=> OperatingSystem.IsBrowser() ? "wasm"
			: OperatingSystem.IsAndroid() ? "android"
			: OperatingSystem.IsIOS() ? "ios"
			: OperatingSystem.IsMacOS() ? "macos"
			: OperatingSystem.IsLinux() ? "linux"
			: OperatingSystem.IsWindows() ? "windows"
			: "unknown";

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (SamplesApp.App.LaunchQuery is not { } query || !query.TryGetValue("scrollprobe", out var scenarios))
		{
			return;
		}

		var names = scenarios is "" or "all" ? ScrollSmoothnessDrivers.Scenarios : scenarios.Split(',');
		_consoleTrace = query.TryGetValue("scrolltrace", out var trace) && trace == "1";
		if (query.TryGetValue("scrollexternalms", out var externalMs) && int.TryParse(externalMs, out var ms))
		{
			ScrollSmoothnessDrivers.ExternalDurationMs = ms;
		}
		var tabs = query.TryGetValue("scrolltabs", out var tabList)
			? tabList.Split(',').Select(int.Parse).ToArray()
			: new[] { 0 };

		// Let the first frames and any startup work settle before measuring.
		await Task.Delay(1500);

		_holdResults = true;
		foreach (var tab in tabs)
		{
			Tabs.SelectedIndex = tab;
			await Task.Delay(500);
			await RunScenariosAsync(names);
		}

		_holdResults = false;
		FlushResults();
		Console.WriteLine("[scroll-probe] done");
	}

	private async void RunScenario_Click(object sender, RoutedEventArgs e)
	{
		if (ScenarioPicker.SelectedItem is string scenario)
		{
			await RunScenariosAsync(new[] { scenario });
		}
	}

	private async void RunAll_Click(object sender, RoutedEventArgs e)
		=> await RunScenariosAsync(ScrollSmoothnessDrivers.Scenarios);

	private void RecordToggle_Click(object sender, RoutedEventArgs e)
	{
		if (RecordToggle.IsChecked == true)
		{
			if (FindScroller() is { } scroller)
			{
				_manualProbe = new ScrollSmoothnessProbe(scroller);
				_manualProbe.Start();
			}
		}
		else if (_manualProbe is { } probe)
		{
			_manualProbe = null;
			probe.Stop();
			Report(probe.Analyze("manual"));
		}
	}

	private async Task RunScenariosAsync(IEnumerable<string> scenarios)
	{
		_runCts?.Cancel();
		var cts = _runCts = new CancellationTokenSource();
		RunScenarioButton.IsEnabled = RunAllButton.IsEnabled = false;
		_isRunning = true;
		try
		{
			foreach (var scenario in scenarios)
			{
				if (FindScroller() is not { } scroller)
				{
					AppendLine("No scroller found in the selected tab.");
					return;
				}

				Report(await MeasureAsync(scenario, scroller, cts.Token));
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			AppendLine($"Failed: {ex}");
			Console.WriteLine($"[scroll-probe] error {ex}");
		}
		finally
		{
			_isRunning = false;
			if (!_holdResults)
			{
				FlushResults();
			}
			RunScenarioButton.IsEnabled = RunAllButton.IsEnabled = true;
		}
	}

	private static async Task<ScrollSmoothnessResult> MeasureAsync(string scenario, Control scroller, CancellationToken ct)
	{
		ScrollSmoothnessDrivers.ScrollToOrigin(scroller);
		await Task.Delay(400, ct);

		var probe = new ScrollSmoothnessProbe(scroller);
		probe.Start();
		try
		{
			var started = Stopwatch.GetTimestamp();
			await ScrollSmoothnessDrivers.RunAsync(scenario, scroller, probe, ct);

			// Settled once nothing has moved for 400 ms, giving up after 8 s (a stuck animation is itself a finding).
			while (Stopwatch.GetElapsedTime(started).TotalSeconds < 8)
			{
				await Task.Delay(50, ct);
				if (probe.LastMotionTimestamp is { } last && Stopwatch.GetElapsedTime(last).TotalMilliseconds > 400)
				{
					break;
				}

				if (probe.LastMotionTimestamp is null && Stopwatch.GetElapsedTime(started).TotalSeconds > 2)
				{
					break;
				}
			}
		}
		finally
		{
			probe.Stop();
		}

		return probe.Analyze(scenario);
	}

	private Control? FindScroller()
	{
		var content = (Tabs.SelectedItem as FrameworkElement) is { } item
			? (item as ContentControl)?.Content as DependencyObject ?? item
			: null;

		return content is null ? null : FindScroller(content);
	}

	private static Control? FindScroller(DependencyObject root)
	{
		if (root is ScrollViewer or ScrollView)
		{
			return (Control)root;
		}

		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			if (FindScroller(VisualTreeHelper.GetChild(root, i)) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private void Report(ScrollSmoothnessResult result)
	{
		var tab = (Tabs.SelectedItem as Microsoft.UI.Xaml.Controls.TabViewItem)?.Header as string ?? "?";
		var summary = $"[{tab}] {result.ToSummary()}";
		AppendLine(summary);

		var json = result.ToJson(Platform, includeTrace: true).Insert(1, $"\"tab\":\"{tab}\",");
		_jsonResults.Add(json);

		// Console readers (logcat) cut long lines, so the trace goes to the console only when asked (&scrolltrace=1).
		Console.WriteLine("[scroll-probe] " + result.ToJson(Platform, includeTrace: _consoleTrace).Insert(1, $"\"tab\":\"{tab}\","));
	}

	private readonly StringBuilder _resultsBuffer = new();

	// Buffered while a run is in progress: moving the TextBox caret scrolls its inner ScrollViewer, which disturbs
	// the frames being measured (it triggers ~1 s Metal drawable stalls on macOS).
	private void AppendLine(string line)
	{
		if (_resultsBuffer.Length > 0)
		{
			_resultsBuffer.Append('\n');
		}

		_resultsBuffer.Append(line);
		if (!_isRunning && !_holdResults)
		{
			FlushResults();
		}
	}

	private void FlushResults()
	{
		ResultsText.Text = _resultsBuffer.ToString();
		ResultsText.SelectionStart = ResultsText.Text.Length;
	}
#else
	private void RunScenario_Click(object sender, RoutedEventArgs e) { }

	private void RunAll_Click(object sender, RoutedEventArgs e) { }

	private void RecordToggle_Click(object sender, RoutedEventArgs e) { }
#endif
}
