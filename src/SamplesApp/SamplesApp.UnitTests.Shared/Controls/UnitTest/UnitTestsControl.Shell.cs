#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SampleControl.Presentation;
using Uno.UI.Samples.Helper;
using Windows.Foundation;

namespace Uno.UI.Samples.Tests;

// Dashboard presentation of the runner: status InfoBar, stat cards, failure details and result rows.
public sealed partial class UnitTestsControl
{
	internal const double ResultRowHeight = 28;
	internal const double FailureDetailsHeight = 100;

	// Output column widths below which the filter gets its own row and the cards wrap 2x2, then labels and gutters shrink.
	internal const double CompactOutputWidth = 472;
	internal const double TinyOutputWidth = 332;

	private static FontFamily? _symbolFontFamily;

	private readonly List<string> _failures = new();
	private int _failureIndex = -1;
	private ClassHeaderInfo? _currentClassHeader;
	private bool _isRunningOnCICache;
	private int _progressCount;
	private readonly int?[] _shownCounts = new int?[6];
	private RunLayout? _runLayout;
	private StatsMode _statsMode;
	private bool _isFittingHeader;
	private Size? _headerFitSize;
	private Dictionary<UnitTestClassInfo, UnitTestMethodInfo[]> _plannedTests = new();

	/// <summary>
	/// Number of test cases (plus skipped methods) the current run will report, set after discovery and before
	/// execution. 0 while unknown, which leaves the progress bar indeterminate. Not computed on CI.
	/// </summary>
	public int PlannedTestCount
	{
		get => (int)GetValue(PlannedTestCountProperty);
		set => SetValue(PlannedTestCountProperty, value);
	}

	public static DependencyProperty PlannedTestCountProperty { get; } =
		DependencyProperty.Register(nameof(PlannedTestCount), typeof(int), typeof(UnitTestsControl), new PropertyMetadata(0, OnPlannedTestCountChanged));

	/// <summary>Shows the "Runtime tests" title above the toolbar (embedded hosts); the shell page hides it.</summary>
	public bool ShowHeader
	{
		get => (bool)GetValue(ShowHeaderProperty);
		set => SetValue(ShowHeaderProperty, value);
	}

	public static DependencyProperty ShowHeaderProperty { get; } =
		DependencyProperty.Register(nameof(ShowHeader), typeof(bool), typeof(UnitTestsControl), new PropertyMetadata(true, OnShowHeaderChanged));

	private static void OnPlannedTestCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UnitTestsControl)d).UpdateProgress();

	private static void OnShowHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((UnitTestsControl)d).ApplyShowHeader();

	internal InfoBar? RunInfoBar => ShellRunInfoBar;

	internal IReadOnlyList<string> Failures => _failures;

	internal int FailureIndex => _failureIndex;

	internal RowDefinition FailureDetailsRow => failedTestDetailsRow;

	private void InitializeShell()
	{
		InitializeSheet();
		ApplyShowHeader();
		UpdateFailureNavigation();

		ShellRowSplitter.KeyboardResize += (_, delta) => ResizeFailureDetails(delta);
		ShellRowSplitter.ResetRequested += (_, _) =>
		{
			failedTestDetailsRow.Height = new GridLength(_failures.Count > 0 ? DefaultFailureDetailsHeight : 0);
			UpdateHeaderMaxHeight();
		};
		ShellColumnSplitter.KeyboardResize += (_, delta) => ResizeOutput(delta);
		ShellColumnSplitter.ResetRequested += (_, _) => outputColumn.Width = new GridLength(1, GridUnitType.Star);

		ActualThemeChanged += (_, _) => ApplyResultRowBrushes();
		SizeChanged += (_, _) => UpdateHeaderMaxHeight();

		if (ShellFunctions.IsTouchShell)
		{
			foreach (var control in new Control[] { runButton, stopButton, ShellRunOptionsButton, ShellFailedOnlyToggle, ShellCopyResultsButton, ShellPreviousFailureButton, ShellNextFailureButton, ShellCopyFailuresButton })
			{
				control.MinWidth = control.MinHeight = TouchTargetSize;
			}

			testFilter.MinHeight = TouchTargetSize;
		}
	}

	internal const double TouchTargetSize = 40;

	// The header (toolbar, status, cards) takes at most 60% of what the failure details leave, the results keep the rest.
	internal static double GetHeaderMaxHeight(double controlHeight, double failureDetailsHeight)
		=> double.IsNaN(controlHeight) || double.IsInfinity(controlHeight) || controlHeight <= 0
			? double.PositiveInfinity
			: Math.Max(120, (controlHeight - failureDetailsHeight) * 0.6);

	internal const double MinResultsHeight = 88;

	internal static double GetHeaderLastResortHeight(double maxHeight, double controlHeight, double failureDetailsHeight)
		=> Math.Max(maxHeight, controlHeight - failureDetailsHeight - MinResultsHeight);

	// Cuts an overflowing header at the last whole section, never through the middle of one.
	internal static double SnapHeaderHeight(double maxHeight, IReadOnlyList<double> sectionBottoms, double bottomPadding)
	{
		if (sectionBottoms.Count == 0 || sectionBottoms[^1] + bottomPadding <= maxHeight)
		{
			return maxHeight;
		}

		var fitting = sectionBottoms.LastOrDefault(bottom => bottom <= maxHeight);
		return fitting > 0 ? Math.Min(maxHeight, fitting + bottomPadding) : maxHeight;
	}

	private void UpdateHeaderMaxHeight(bool contentChanged = false)
	{
		UpdateRowSplitterVisibility();
		if (_isFittingHeader)
		{
			return;
		}

		_isFittingHeader = true;
		try
		{
			// The sheet shows the counters as chips in its bar; its header only has to leave the results room.
			if (_isSheet)
			{
				ApplyStatCards(StatsMode.Strip);
				var sheetMaxHeight = GetSheetHeaderMaxHeight();
				if (ShellRunHeaderScroller.ActualWidth > 0)
				{
					ShellRunHeaderPanel.Measure(new Size(ShellRunHeaderScroller.ActualWidth, double.PositiveInfinity));
					sheetMaxHeight = SnapHeaderHeight(sheetMaxHeight, GetHeaderSectionBottoms(), ShellRunHeaderPanel.Padding.Bottom);
				}

				ShellRunHeaderScroller.MaxHeight = sheetMaxHeight;
				_headerFitSize = null;
				return;
			}

			var maxHeight = GetHeaderMaxHeight(ActualHeight, failedTestDetailsRow.Height.Value);
			var width = ShellRunHeaderScroller.ActualWidth;
			if (double.IsInfinity(maxHeight) || width <= 0)
			{
				ApplyStatCards(StatsMode.Cards);
				ShellRunHeaderScroller.MaxHeight = maxHeight;
				_headerFitSize = null;
				return;
			}

			// Richest stats presentation whose header still fits, measured for real. Richer modes are only
			// tried when the header got more room or new content since the last fit.
			ShellRunHeaderPanel.Measure(new Size(width, double.PositiveInfinity));
			var fits = ShellRunHeaderPanel.DesiredSize.Height <= maxHeight;
			var hasMoreRoom = contentChanged
				|| _headerFitSize is not { } last
				|| width > last.Width
				|| maxHeight > last.Height;
			// Fitting: the richer modes, then the current one again. Overflowing: the poorer ones.
			var current = (int)_statsMode;
			var candidates = fits
				? (hasMoreRoom && _statsMode != StatsMode.Cards ? Enumerable.Range(0, current + 1) : Enumerable.Empty<int>())
				: Enumerable.Range(current + 1, (int)StatsMode.Hidden - current);
			foreach (var mode in candidates)
			{
				ApplyStatCards((StatsMode)mode);
				ShellRunHeaderPanel.Measure(new Size(width, double.PositiveInfinity));
				if (ShellRunHeaderPanel.DesiredSize.Height <= maxHeight)
				{
					break;
				}
			}

			_headerFitSize = new Size(width, maxHeight);

			// Without the cards the status may take more than its share, as long as the results keep a minimum.
			if (_statsMode == StatsMode.Hidden)
			{
				maxHeight = GetHeaderLastResortHeight(maxHeight, ActualHeight, failedTestDetailsRow.Height.Value);
			}

			ShellRunHeaderScroller.MaxHeight = SnapHeaderHeight(maxHeight, GetHeaderSectionBottoms(), ShellRunHeaderPanel.Padding.Bottom);
		}
		finally
		{
			_isFittingHeader = false;
		}
	}

	private List<double> GetHeaderSectionBottoms()
	{
		List<double> bottoms = new();
		var top = ShellRunHeaderPanel.Padding.Top;
		foreach (var child in ShellRunHeaderPanel.Children)
		{
			if (child.Visibility == Visibility.Visible)
			{
				bottoms.Add(top + child.DesiredSize.Height);
				top = bottoms[^1] + ShellRunHeaderPanel.Spacing;
			}
		}

		return bottoms;
	}

	private static void OnIsRunningOnCIChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var control = (UnitTestsControl)d;
		control._isRunningOnCICache = (bool)e.NewValue;
		control.UpdateSheetLock();
	}

	private void ApplyShowHeader()
	{
		if (ShellRunTitle is { } title)
		{
			title.Visibility = ShowHeader ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	internal enum RunLayout
	{
		Wide,
		Compact,
		Tiny,
	}

	internal static RunLayout GetRunLayout(double outputWidth)
		=> outputWidth < TinyOutputWidth ? RunLayout.Tiny
		: outputWidth < CompactOutputWidth ? RunLayout.Compact
		: RunLayout.Wide;

	/// <summary>How the stat cards show: full cards, label-less chips, or hidden (counts move to the Results caption).</summary>
	internal enum StatsMode
	{
		Cards,
		Strip,
		Hidden,
	}

	internal static (int Row, int Column) GetStatCardCell(int index, RunLayout layout, StatsMode mode = StatsMode.Cards)
		=> IsSingleStatRow(layout, mode) ? (0, index) : (index / 2, index % 2);

	private static bool IsSingleStatRow(RunLayout layout, StatsMode mode)
		=> layout == RunLayout.Wide || (mode == StatsMode.Strip && layout == RunLayout.Compact);

	internal RunLayout? CurrentRunLayout => _runLayout;

	internal StatsMode CurrentStatsMode => _statsMode;

	internal static double GetDetailMaxWidth(RunLayout layout) => layout switch
	{
		RunLayout.Wide => 180,
		RunLayout.Compact => 120,
		_ => double.PositiveInfinity,
	};

	private void OnHeaderPanelSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var widthChanged = e.PreviousSize.Width != e.NewSize.Width;
		ApplyRunLayout(GetRunLayout(e.NewSize.Width));

		// Status line swaps (Ready / InfoBar) change the height too.
		if (!widthChanged && e.PreviousSize.Height != e.NewSize.Height)
		{
			UpdateHeaderMaxHeight(contentChanged: true);
		}
	}

	private void ApplyRunLayout(RunLayout layout)
	{
		if (layout == _runLayout)
		{
			return;
		}

		_runLayout = layout;
		var isWide = layout == RunLayout.Wide;
		var isTiny = layout == RunLayout.Tiny;

		// Filter on its own row below Wide; in the sheet, Run and Stop are in the bar and the filter takes their place.
		Grid.SetRow(testFilter, isWide || _isSheet ? 0 : 1);
		Grid.SetColumn(testFilter, isWide ? 2 : 0);
		Grid.SetColumnSpan(testFilter, isWide ? 1 : _isSheet ? 3 : 4);

		ApplyStatCards(_statsMode, force: true);

		// Running status: count next to the test name, or next to the bar when Tiny leaves no room for the name.
		if (ShellRunInfoBar is not null)
		{
			ApplyProgressLayout(isTiny);
		}

		foreach (var child in testResults.Children)
		{
			if (child is FrameworkElement { Tag: ResultRowInfo info })
			{
				ApplyResultRowLayout(info, layout, _isSheet);
			}
		}

		// Labels: buttons keep theirs down to Compact, "Failed only" only in Wide; Tiny also drops the section captions.
		var buttonLabels = isTiny ? Visibility.Collapsed : Visibility.Visible;
		ShellRunLabel.Visibility = ShellStopLabel.Visibility = _isSheet ? Visibility.Collapsed : buttonLabels;
		ShellRunOptionsLabel.Visibility = buttonLabels;
		ShellFailedOnlyLabel.Visibility = isWide ? Visibility.Visible : Visibility.Collapsed;
		ShellResultsCaption.Visibility = buttonLabels;
		ShellFailureTitle.Visibility = buttonLabels;
		ToolTipService.SetToolTip(runButton, isTiny || _isSheet ? "Run" : null);
		ToolTipService.SetToolTip(stopButton, isTiny || _isSheet ? "Stop" : null);
		ToolTipService.SetToolTip(ShellRunOptionsButton, isTiny ? "Options" : null);
		ToolTipService.SetToolTip(ShellFailedOnlyToggle, isWide ? null : "Failed only");

		var gutter = isTiny ? 8 : 16;
		ShellRunHeaderPanel.Padding = new Thickness(gutter, _isSheet ? 4 : 12, gutter, 8);
		ShellFailureLayer.Padding = new Thickness(gutter, 0, gutter, 4);
		ShellResultsLayer.Padding = new Thickness(gutter, 4, gutter, gutter);
		ShellRowSplitter.Padding = new Thickness(gutter, 0, gutter, 0);

		UpdateFailureNavigation();
		UpdateHeaderMaxHeight(contentChanged: true);
	}

	// One row of four in Wide (and as chips in Compact), else 2x2. Labels show only on full cards outside Tiny;
	// they stay in the automation names.
	private void ApplyStatCards(StatsMode mode, bool force = false)
	{
		if (mode == _statsMode && !force)
		{
			return;
		}

		_statsMode = mode;
		var layout = _runLayout ?? RunLayout.Wide;
		var isCompactCard = _isSheet || mode != StatsMode.Cards || layout == RunLayout.Tiny;

		ShellRunStats.Visibility = mode == StatsMode.Hidden ? Visibility.Collapsed : Visibility.Visible;
		ShellResultsStatsSummary.Visibility = mode == StatsMode.Hidden ? Visibility.Visible : Visibility.Collapsed;

		// The sheet's bar shows the chips in one row.
		var columnCount = _isSheet || IsSingleStatRow(layout, mode) ? 4 : 2;
		var columns = ShellRunStats.ColumnDefinitions;
		while (columns.Count > columnCount)
		{
			columns.RemoveAt(columns.Count - 1);
		}

		while (columns.Count < columnCount)
		{
			columns.Add(new ColumnDefinition());
		}

		foreach (var column in columns)
		{
			column.Width = new GridLength(1, GridUnitType.Star);
		}

		ShellRunStats.ColumnSpacing = _isSheet ? 4 : 8;
		ShellRunStats.RowSpacing = _isSheet ? 0 : 8;

		var cards = new (Border Card, TextBlock Number, TextBlock Label)[]
		{
			(ShellPassedCard, succeededTestCount, ShellPassedLabel),
			(ShellFailedCard, failedTestCount, ShellFailedLabel),
			(ShellInconclusiveCard, inconclusiveTestCount, ShellInconclusiveLabel),
			(ShellRetriedCard, retriedTestCount, ShellRetriedLabel),
		};
		for (var i = 0; i < cards.Length; i++)
		{
			var (card, number, label) = cards[i];
			var (row, column) = _isSheet ? (0, i) : GetStatCardCell(i, layout, mode);
			Grid.SetRow(card, row);
			Grid.SetColumn(card, column);
			card.Padding = _isSheet ? new Thickness(8, 2, 8, 2) : isCompactCard ? new Thickness(8, 6, 8, 6) : new Thickness(12, 8, 12, 8);
			label.Visibility = isCompactCard ? Visibility.Collapsed : Visibility.Visible;
			number.FontSize = _isSheet ? 14 : isCompactCard ? 16 : 20;
			if (_isSheet)
			{
				number.LineHeight = 20;
			}
			else
			{
				number.ClearValue(TextBlock.LineHeightProperty);
			}
			ToolTipService.SetToolTip(card, isCompactCard ? label.Text : null);
		}
	}

	private void ApplyProgressLayout(bool isTiny)
	{
		Grid.SetRow(ShellRunProgressText, isTiny ? 1 : 0);
		Grid.SetColumnSpan(ShellRunMessageText, isTiny ? 2 : 1);
		Grid.SetColumnSpan(ShellRunProgress, isTiny ? 1 : 2);
	}

	private void ResetRunView()
	{
		testResults.Children.Clear();
		_failures.Clear();
		_failureIndex = -1;
		_currentClassHeader = null;
		failedTestDetailsRow.Height = new GridLength(0);
		UpdateHeaderMaxHeight();
		UpdateFailureNavigation();
		OnRunStarting();
	}

	private void ResizeFailureDetails(double delta)
	{
		failedTestDetailsRow.Height = new GridLength(Math.Max(0, failedTestDetailsRow.ActualHeight + delta));
		UpdateHeaderMaxHeight();
	}

	private void ResizeOutput(double delta)
		=> outputColumn.Width = new GridLength(Math.Max(0, outputColumn.ActualWidth + delta));

	#region Status
	internal static (InfoBarSeverity Severity, string Title) GetRunOutcome(int failed, int inconclusive, bool isStopped) => failed switch
	{
		< 0 => (InfoBarSeverity.Error, "Runner failed"),
		> 0 => (InfoBarSeverity.Error, $"{failed.ToString("N0", CultureInfo.CurrentCulture)} failed"),
		_ when isStopped => (InfoBarSeverity.Warning, "Stopped"),
		_ when inconclusive > 0 => (InfoBarSeverity.Warning, $"{inconclusive.ToString("N0", CultureInfo.CurrentCulture)} inconclusive"),
		_ => (InfoBarSeverity.Success, "Passed"),
	};

	internal static string FormatProgress(int done, int planned)
		=> planned > 0
			? $"{Math.Min(done, planned).ToString("N0", CultureInfo.CurrentCulture)} of {planned.ToString("N0", CultureInfo.CurrentCulture)}"
			: done.ToString("N0", CultureInfo.CurrentCulture);

	internal static string FormatRunSummary(int run, int skipped, TimeSpan elapsed)
		=> $"{run.ToString("N0", CultureInfo.CurrentCulture)} run · {skipped.ToString("N0", CultureInfo.CurrentCulture)} skipped · {FormatDuration(elapsed)}";

	/// <summary>
	/// Counts the cases the run will report. The filtered method infos go to <paramref name="plannedTests"/> so the run
	/// reuses them instead of querying the data sources (DataRow, DynamicData) a second time.
	/// </summary>
	internal static int CountPlannedTests(IEnumerable<UnitTestClassInfo> testClasses, UnitTestEngineConfig config, IDictionary<UnitTestClassInfo, UnitTestMethodInfo[]>? plannedTests = null)
	{
		var count = 0;
		foreach (var testClass in testClasses)
		{
			if (testClass.Type is not { } type)
			{
				continue;
			}

			var tests = FilterTests((testClass.Tests ?? Array.Empty<MethodInfo>()).Select(method => new UnitTestMethodInfo(type, method)), config.Filters).ToArray();
			if (plannedTests is not null)
			{
				plannedTests[testClass] = tests;
			}

			foreach (var test in tests)
			{
				count += test.IsIgnored(out _) && !config.IsRunningIgnored
					? 1
					: test.GetMatchingCases(config.Filters).Count();
			}
		}

		return count;
	}

	private InfoBar? EnsureRunInfoBar()
	{
		if (IsRunningOnCI)
		{
			return null;
		}

		if (ShellRunInfoBar is null)
		{
			FindName(nameof(ShellRunInfoBar));
			if (ShellRunInfoBar is not null)
			{
				ApplyProgressLayout(_runLayout == RunLayout.Tiny);
			}
		}

		return ShellRunInfoBar;
	}

	private void UpdateRunInfoBar(string message, string? infoMessage, bool isRunning)
	{
		if (EnsureRunInfoBar() is not { } infoBar)
		{
			return;
		}

		runStatus.Visibility = Visibility.Collapsed;
		if (isRunning)
		{
			infoBar.Severity = InfoBarSeverity.Informational;
			infoBar.Title = "Running";
			infoBar.Message = "";
			ShellRunMessageText.Text = infoMessage ?? message;
			ShellRunProgressPanel.Visibility = Visibility.Visible;
			UpdateProgress();
		}
		else
		{
			infoBar.Message = infoMessage ?? message;
			ShellRunProgressPanel.Visibility = Visibility.Collapsed;
		}
	}

	private void UpdateProgress()
	{
		UpdateSheetStatus();
		if (ShellRunInfoBar is null)
		{
			return;
		}

		var planned = PlannedTestCount;
		var done = Volatile.Read(ref _progressCount);
		ShellRunProgress.IsIndeterminate = planned <= 0;
		ShellRunProgress.Maximum = Math.Max(1, planned);
		ShellRunProgress.Value = Math.Min(done, Math.Max(1, planned));
		ShellRunProgressText.Text = FormatProgress(done, planned);
	}

	private void ApplyRunOutcome(bool isStopped)
	{
		if (_currentRun is not { } run)
		{
			OnRunEnded(null);
			return;
		}

		var (severity, title) = GetRunOutcome(run.Failed, run.Inconclusive, isStopped);
		OnRunEnded(title);
		if (EnsureRunInfoBar() is not { } infoBar)
		{
			return;
		}

		infoBar.Severity = severity;
		infoBar.Title = title;
		infoBar.Message = FormatRunSummary(run.Run, run.Ignored, DateTimeOffset.UtcNow - run.StartTime);
		ShellRunProgressPanel.Visibility = Visibility.Collapsed;

		if (FrameworkElementAutomationPeer.FromElement(infoBar) is { } peer)
		{
			peer.RaiseNotificationEvent(AutomationNotificationKind.ActionCompleted, AutomationNotificationProcessing.ImportantMostRecent, $"Tests {title.ToLowerInvariant()}. {infoBar.Message}", "RuntimeTestsFinished");
		}
	}

	private void UpdateCounters()
	{
		if (_currentRun is not { } run)
		{
			return;
		}

		// Called twice per test: only touch what changed.
		var changed = SetCount(0, succeededTestCount, "Passed", run.Succeeded);
		changed |= SetCount(1, failedTestCount, "Failed", run.Failed);
		changed |= SetCount(2, inconclusiveTestCount, "Inconclusive", run.Inconclusive);
		SetCount(3, retriedTestCount, "Retried", run.Retried);
		SetCount(4, runTestCount, "Total", run.Run);
		SetCount(5, ignoredTestCount, "Skipped", run.Ignored);
		if (changed)
		{
			ShellResultsStatsSummary.Text = FormatStatsSummary(run.Succeeded, run.Failed, run.Inconclusive);
		}

		bool SetCount(int slot, TextBlock text, string label, int value)
		{
			if (_shownCounts[slot] == value)
			{
				return false;
			}

			_shownCounts[slot] = value;
			text.Text = value.ToString("N0", CultureInfo.CurrentCulture);
			AutomationProperties.SetName(text, $"{label}: {text.Text}");
			return true;
		}
	}

	internal static string FormatStatsSummary(int passed, int failed, int inconclusive)
		=> $"· {passed.ToString("N0", CultureInfo.CurrentCulture)} passed · {failed.ToString("N0", CultureInfo.CurrentCulture)} failed"
			+ (inconclusive > 0 ? $" · {inconclusive.ToString("N0", CultureInfo.CurrentCulture)} inconclusive" : "");
	#endregion

	#region Failure details
	private void AddFailure(string details)
	{
		_failures.Add(details);
		if (_failureIndex < 0)
		{
			_failureIndex = 0;
		}

		UpdateFailureNavigation();
	}

	private void ShowFailure(int index)
	{
		if (index >= 0 && index < _failures.Count)
		{
			_failureIndex = index;
			if (failedTestDetailsRow.Height.Value == 0)
			{
				failedTestDetailsRow.Height = new GridLength(DefaultFailureDetailsHeight);
				UpdateHeaderMaxHeight();
			}

			UpdateFailureNavigation();
		}
	}

	internal static string FormatFailurePosition(int index, int count, bool isShort = false)
		=> count == 0 ? ""
		: $"{(index + 1).ToString("N0", CultureInfo.CurrentCulture)}{(isShort ? "/" : " of ")}{count.ToString("N0", CultureInfo.CurrentCulture)}";

	private void UpdateFailureNavigation()
	{
		ShellFailurePosition.Text = FormatFailurePosition(_failureIndex, _failures.Count, isShort: _runLayout == RunLayout.Tiny);
		ShellFailureText.Text = _failureIndex >= 0 && _failureIndex < _failures.Count ? _failures[_failureIndex] : "";
		ShellPreviousFailureButton.IsEnabled = _failureIndex > 0;
		ShellNextFailureButton.IsEnabled = _failureIndex < _failures.Count - 1;
		ShellCopyFailuresButton.IsEnabled = _failures.Count > 0;
	}

	private void OnPreviousFailure(object sender, RoutedEventArgs e) => ShowFailure(_failureIndex - 1);

	private void OnNextFailure(object sender, RoutedEventArgs e) => ShowFailure(_failureIndex + 1);
	#endregion

	#region Result rows
	private sealed class ClassHeaderInfo
	{
		public int FailedCount { get; set; }

		public UIElement? Element { get; set; }
	}

	private sealed class ResultRowInfo
	{
		public TestResult Result { get; init; }

		public bool IsRetried { get; init; }

		public ClassHeaderInfo? Header { get; init; }

		public TextBlock Glyph { get; init; } = null!;

		public TextBlock Detail { get; init; } = null!;

		public TextBlock Name { get; init; } = null!;

		public bool IsFailure => Result is TestResult.Failed or TestResult.Error;
	}

	internal static bool IsFailure(TestResult result) => result is TestResult.Failed or TestResult.Error;

	internal static string GetResultGlyph(TestResult result, bool isRetried) => result switch
	{
		TestResult.Passed when isRetried => "\uE72C",
		TestResult.Passed => "\uE73E",
		// Uno heads use Uno.Fonts.Fluent, which has no E733 (Blocked).
#if HAS_UNO
		TestResult.Skipped => "\uE738",
#else
		TestResult.Skipped => "\uE733",
#endif
		TestResult.Inconclusive => "\uE7BA",
		_ => "\uE711",
	};

	internal static string GetResultBrushKey(TestResult result, bool isRetried) => result switch
	{
		TestResult.Passed when isRetried => "ShellTestWarningBrush",
		TestResult.Passed => "ShellTestPassedBrush",
		TestResult.Skipped => "ShellTestNeutralBrush",
		TestResult.Inconclusive => "ShellTestWarningBrush",
		_ => "ShellTestFailedBrush",
	};

	internal static string FormatDuration(TimeSpan duration)
		=> duration.TotalSeconds < 1 ? $"{Math.Round(duration.TotalMilliseconds).ToString(CultureInfo.CurrentCulture)} ms"
		: duration.TotalMinutes < 1 ? $"{duration.TotalSeconds.ToString("0.0", CultureInfo.CurrentCulture)} s"
		: $"{(int)duration.TotalMinutes}m {duration.Seconds:00}s";

	internal static string GetResultDetail(TestResult result, TimeSpan duration, int retries, string? errorMessage, string? message) => result switch
	{
		TestResult.Passed when retries > 0 => $"passed after {retries} {(retries == 1 ? "retry" : "retries")}",
		TestResult.Passed => FormatDuration(duration),
		TestResult.Skipped => "skipped",
		TestResult.Inconclusive => FirstLine(message) ?? "inconclusive",
		_ => FirstLine(errorMessage ?? message) ?? "failed",
	};

	internal static string GetDisplayName(string testName)
		=> testName.EndsWith("()", StringComparison.Ordinal) ? testName[..^2] : testName;

	private static string? FirstLine(string? text)
	{
		var line = text?
			.Split('\n')
			.Select(l => l.Trim())
			.FirstOrDefault(l => l.Length > 0);

		return line is { Length: > 80 } ? line[..79] + "…" : line;
	}

	private static FontFamily SymbolFontFamily
		=> _symbolFontFamily ??= Application.Current.Resources.TryGetValue("SymbolThemeFontFamily", out var value) && value is FontFamily family
			? family
			: new FontFamily("Segoe Fluent Icons");

	private void AddClassHeaderRow(string className)
	{
		_currentClassHeader = new ClassHeaderInfo();

		var header = new TextBlock
		{
			Text = className,
			FontWeight = FontWeights.SemiBold,
			MinHeight = 32,
			Padding = new Thickness(0, 8, 0, 4),
			TextTrimming = TextTrimming.CharacterEllipsis,
			IsTextSelectionEnabled = true,
			Tag = _currentClassHeader,
			Visibility = ShellFailedOnlyToggle.IsChecked is true ? Visibility.Collapsed : Visibility.Visible,
		};
		AutomationProperties.SetHeadingLevel(header, AutomationHeadingLevel.Level3);
		_currentClassHeader.Element = header;

		testResults.Children.Add(header);
	}

	/// <summary>Message and console output a non-failed row expands to; null when there is nothing beyond the row itself.</summary>
	internal static string? GetExpandedOutput(string? message, string? console)
	{
		var parts = new List<string>();
		if (message?.Trim() is { Length: > 0 } trimmedMessage)
		{
			parts.Add(trimmedMessage);
		}

		if (console?.TrimEnd() is { Length: > 0 } trimmedConsole)
		{
			parts.Add(ConsoleSeparator + "\n" + trimmedConsole);
		}

		return parts.Count > 0 ? string.Join("\n", parts) : null;
	}

	internal const string ConsoleSeparator = "--- Console ---";

	internal static string FormatFailure(string? className, string testName, string? details, string? console)
		=> $"{(className is null ? "" : className + ".")}{testName}\n{details}"
			+ (console?.TrimEnd() is { Length: > 0 } output ? $"\n{ConsoleSeparator}\n{output}" : "");

	private void AddResultRow(string testName, TimeSpan duration, TestResult result, int retries, string? errorMessage, string? message, string? console)
	{
		var detail = GetResultDetail(result, duration, retries, errorMessage, message);
		var info = new ResultRowInfo
		{
			Result = result,
			IsRetried = retries > 0,
			Header = _currentClassHeader,
			Glyph = new TextBlock
			{
				Text = GetResultGlyph(result, retries > 0),
				FontFamily = SymbolFontFamily,
				FontSize = 12,
				VerticalAlignment = VerticalAlignment.Center,
			},
			// Two lines before trimming, so narrow (phone) columns still show most of the name.
			Name = new TextBlock
			{
				Text = GetDisplayName(testName),
				TextWrapping = TextWrapping.Wrap,
				MaxLines = 2,
				TextTrimming = TextTrimming.CharacterEllipsis,
				VerticalAlignment = VerticalAlignment.Center,
			},
			Detail = new TextBlock
			{
				Text = detail,
				FontSize = 12,
				TextTrimming = TextTrimming.CharacterEllipsis,
				VerticalAlignment = VerticalAlignment.Center,
			},
		};
		AutomationProperties.SetAccessibilityView(info.Glyph, AccessibilityView.Raw);

		var row = new Grid
		{
			MinHeight = ResultRowHeight,
			ColumnSpacing = 8,
		};
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		row.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		Grid.SetColumn(info.Name, 1);
		row.Children.Add(info.Glyph);
		row.Children.Add(info.Name);
		row.Children.Add(info.Detail);
		ApplyResultRowLayout(info, _runLayout ?? RunLayout.Wide, _isSheet);

		var automationName = $"{testName}: {detail}";
		// The detail column trims the message; the full text is in the failure card or the expanded output.
		if ((errorMessage ?? message)?.Trim() is { Length: > 0 } fullMessage)
		{
			ToolTipService.SetToolTip(row, fullMessage.Length > 500 ? fullMessage[..500] + "…" : fullMessage);
		}

		var output = info.IsFailure ? null : GetExpandedOutput(errorMessage ?? message, console);
		FrameworkElement element = row;
		if (info.IsFailure || output is not null)
		{
			// A button, so keyboard, touch and screen readers can open what the row trims away.
			var button = new Button
			{
				Content = row,
				Style = (Style)Resources["ShellResultRowButtonStyle"],
			};
			AutomationProperties.SetName(button, automationName);

			var container = new StackPanel { Children = { button } };
			if (info.IsFailure)
			{
				var failureIndex = _failures.Count - 1;
				button.Click += (_, _) => ShowFailure(failureIndex);
				AutomationProperties.SetHelpText(button, "Shows the failure details");
				if (info.Header is { } header)
				{
					header.FailedCount++;
				}
			}
			else
			{
				var outputText = new TextBlock
				{
					Text = output,
					Style = (Style)Resources["ShellMonospaceTextStyle"],
					TextWrapping = TextWrapping.Wrap,
					IsTextSelectionEnabled = true,
					Margin = new Thickness(40, 0, 0, 8),
					Visibility = Visibility.Collapsed,
				};
				container.Children.Add(outputText);
				AutomationProperties.SetHelpText(button, "Shows the test output");
				button.Click += (_, _) => outputText.Visibility = outputText.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
			}

			element = container;
		}
		else
		{
			AutomationProperties.SetName(row, automationName);
		}

		element.Tag = info;
		ApplyResultRowBrushes(info);
		testResults.Children.Add(element);
		ApplyFailedOnlyFilter(element);
	}

	// Tiny puts the detail under the name, so the name keeps the whole column. The sheet keeps every row on one line.
	private static void ApplyResultRowLayout(ResultRowInfo info, RunLayout layout, bool isSheet)
	{
		var isTiny = layout == RunLayout.Tiny && !isSheet;
		if (info.Detail.Parent is Grid row)
		{
			row.Padding = new Thickness(isTiny || isSheet ? 4 : 16, 2, 0, 2);
		}

		Grid.SetRow(info.Detail, isTiny ? 1 : 0);
		Grid.SetColumn(info.Detail, isTiny ? 1 : 2);
		info.Detail.MaxWidth = isSheet ? GetDetailMaxWidth(RunLayout.Compact) : GetDetailMaxWidth(layout);
		info.Name.MaxLines = isSheet ? 1 : 2;
		info.Name.TextWrapping = isSheet ? TextWrapping.NoWrap : TextWrapping.Wrap;
	}

	private void ApplyResultRowBrushes()
	{
		foreach (var child in testResults.Children)
		{
			if (child is FrameworkElement { Tag: ResultRowInfo info })
			{
				ApplyResultRowBrushes(info);
			}
		}
	}

	private void ApplyResultRowBrushes(ResultRowInfo info)
	{
		var statusBrush = ShellThemeBrushes.Get(GetResultBrushKey(info.Result, info.IsRetried), ActualTheme);
		var secondaryBrush = ShellThemeBrushes.Get("TextFillColorSecondaryBrush", ActualTheme);

		info.Glyph.Foreground = statusBrush;
		info.Detail.Foreground = info.Result is TestResult.Failed or TestResult.Error ? statusBrush : secondaryBrush;
		info.Name.Foreground = info.Result == TestResult.Skipped ? secondaryBrush : ShellThemeBrushes.Get("TextFillColorPrimaryBrush", ActualTheme);
	}

	private void OnFailedOnlyChanged(object sender, RoutedEventArgs e)
	{
		foreach (var child in testResults.Children)
		{
			ApplyFailedOnlyFilter(child);
		}
	}

	private void ApplyFailedOnlyFilter(UIElement element)
	{
		var failedOnly = ShellFailedOnlyToggle.IsChecked is true;
		var isVisible = element switch
		{
			FrameworkElement { Tag: ResultRowInfo info } => !failedOnly || info.IsFailure,
			FrameworkElement { Tag: ClassHeaderInfo header } => !failedOnly || header.FailedCount > 0,
			_ => true,
		};

		element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

		if (failedOnly && element is FrameworkElement { Tag: ResultRowInfo { IsFailure: true, Header.Element: { } headerElement } })
		{
			headerElement.Visibility = Visibility.Visible;
		}
	}
	#endregion

	private static void CopyText(string? text) => SampleChooserViewModel.CopyToClipboard(text);
}
