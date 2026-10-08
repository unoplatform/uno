#nullable enable

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Threading;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using SampleControl.Presentation;
using Uno.UI.Samples.Helper;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;

namespace Uno.UI.Samples.Tests;

// Narrow portrait windows: the dashboard becomes a bottom sheet over a full-width test host.
public sealed partial class UnitTestsControl
{
	internal const double SheetMaxWidth = 600;
	internal const double SheetExpandedRatio = 0.75;
	internal const double SheetFailureDetailsHeight = 120;
	internal const double SheetMinFailureDetailsHeight = 48;
	internal const double SheetMinHeaderHeight = 56;
	internal const int SheetMinResultRows = 3;
	private const double SheetFlickVelocity = 0.3;
	private const double SheetDragThreshold = 8;
	private const double SheetAnimationMilliseconds = 250;
	// Results card: header row estimate, layer padding (4 + 16) and list padding (8).
	private const double SheetResultsHeaderEstimate = 44;
	private const double SheetResultsChrome = 28;

	private static readonly Brush _transparentBrush = new SolidColorBrush(Colors.Transparent);

	private bool _isSheet;
	private bool _isSheetExpanded;
	private bool _isSheetContentShown = true;
	private bool _isRunActive;
	private bool _hasSheetLayout;
	private bool _isSheetLayoutPending;
	private string? _runOutcomeTitle;
	private double _sheetBarHeight;
	private double _sheetExpandedHeight;
	private Storyboard? _sheetStoryboard;
	private readonly Stopwatch _sheetAnimationClock = new();
	private double _sheetAnimationFrom;
	private double _sheetAnimationTo;
	private EasingFunctionBase? _sheetAnimationEasing;
	private Func<bool> _isAutomationActive = () => SampleChooserViewModel.Instance?.IsAutomationRun ?? false;

	// Drag state: a drag that starts on Run, Stop or the handle must not also click them.
	private bool _isSheetDragging;
	private double _sheetDragStartOffset;
	private double _sheetDragStartY;
	private uint? _sheetPointerId;
	private double _sheetDragLastOffset;
	private double _sheetDragVelocity;
	private readonly Stopwatch _sheetDragClock = new();

	internal bool IsBottomSheet => _isSheet;

	internal bool IsSheetExpanded => _isSheetExpanded;

	internal bool IsSheetDragging => _isSheetDragging;

	internal FrameworkElement SheetBar => ShellSheetBar;

	/// <summary>Automation runs (--runtime-tests, UI tests) keep the sheet collapsed; tests swap this out.</summary>
	internal Func<bool> IsAutomationActive
	{
		get => _isAutomationActive;
		set
		{
			_isAutomationActive = value;
			UpdateSheetLock();
		}
	}

	internal bool IsSheetAnimationEnabled { get; set; } = true;

	internal bool IsSheetLocked => IsRunningOnCI || IsAutomationActive();

	internal static bool UsesBottomSheet(double width, double height) => width > 0 && width < SheetMaxWidth && height > width;

	private double SheetCollapsedOffset => Math.Max(0, _sheetExpandedHeight - _sheetBarHeight);

	private double DefaultFailureDetailsHeight => _isSheet ? GetSheetFailureDetailsHeight() : FailureDetailsHeight;

	private void InitializeSheet()
	{
		ShellSheetHandle.ExpandRequested += (_, expand) => SetSheetExpanded(expand);
		ShellSheet.KeyDown += OnSheetKeyDown;

		// Run, Stop and the handle handle their presses; a drag that starts on them still has to see the pointer.
		ShellSheetBar.AddHandler(PointerPressedEvent, new PointerEventHandler(OnSheetBarPointerPressed), handledEventsToo: true);
		ShellSheetBar.AddHandler(PointerMovedEvent, new PointerEventHandler(OnSheetBarPointerMoved), handledEventsToo: true);
		ShellSheetBar.AddHandler(PointerReleasedEvent, new PointerEventHandler(OnSheetBarPointerEnded), handledEventsToo: true);
		ShellSheetBar.AddHandler(PointerCanceledEvent, new PointerEventHandler(OnSheetBarPointerEnded), handledEventsToo: true);
		ShellSheetBar.AddHandler(PointerCaptureLostEvent, new PointerEventHandler(OnSheetBarPointerCaptureLost), handledEventsToo: true);
		SizeChanged += (_, e) => ApplySheetLayout(e.NewSize);

		// The header's height cap needs its width, which a collapsed sheet does not have.
		ShellRunHeaderScroller.SizeChanged += (_, e) =>
		{
			if (_isSheet && e.PreviousSize.Width != e.NewSize.Width)
			{
				UpdateHeaderMaxHeight();
			}
		};

		Loaded += (_, _) =>
		{
			if (SampleChooserViewModel.Instance is { } vm)
			{
				vm.PropertyChanged -= OnViewModelPropertyChanged;
				vm.PropertyChanged += OnViewModelPropertyChanged;
			}

			UpdateSheetLock();
		};
		Unloaded += (_, _) =>
		{
			if (SampleChooserViewModel.Instance is { } vm)
			{
				vm.PropertyChanged -= OnViewModelPropertyChanged;
			}
		};
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(SampleChooserViewModel.IsAutomationRun) or null or "")
		{
			UpdateSheetLock();
		}
	}

	// Automation can start while the sheet is open; a locked sheet also stops offering itself.
	private void UpdateSheetLock()
	{
		var isLocked = IsSheetLocked;
		ShellSheetHandle.IsEnabled = !isLocked;
		if (isLocked)
		{
			SetSheetExpanded(false, animate: false);
		}
	}

	internal void SetSheetExpanded(bool isExpanded) => SetSheetExpanded(isExpanded, animate: true);

	private void ApplySheetLayout(Size size)
	{
		var isSheet = UsesBottomSheet(size.Width, size.Height);
		if (isSheet != _isSheet)
		{
			// A running test keeps its host where it is; a resize or rotation applies once the run ends.
			if (_isRunActive && _hasSheetLayout)
			{
				_isSheetLayoutPending = true;
			}
			else
			{
				var previousFailureHeight = DefaultFailureDetailsHeight;
				_isSheet = isSheet;
				ApplySheetMode();

				if (failedTestDetailsRow.Height.Value == previousFailureHeight)
				{
					failedTestDetailsRow.Height = new GridLength(DefaultFailureDetailsHeight);
				}
			}
		}
		else
		{
			_isSheetLayoutPending = false;
		}

		_hasSheetLayout |= size.Width > 0;

		if (_isSheet)
		{
			UpdateSheetGeometry(size);
		}
	}

	private void ApplySheetMode()
	{
		var isSheet = _isSheet;
		_isSheetLayoutPending = false;
		StopSheetAnimation();
		if (isSheet)
		{
			MoveFocusOutOfSheetBody();
		}

		_isSheetExpanded = false;
		ShellSheetHandle.IsExpanded = false;
		ShellSheetHandleIcon.Glyph = "";
		ToolTipService.SetToolTip(ShellSheetHandle, "Show test details");

		MoveRunControls(isSheet);

		Grid.SetColumnSpan(ShellSheet, isSheet ? 3 : 1);
		ShellSheet.VerticalAlignment = isSheet ? VerticalAlignment.Bottom : VerticalAlignment.Stretch;
		Canvas.SetZIndex(ShellSheet, isSheet ? 1 : 0);
		ShellSheetBar.Visibility = ShellSheetSurface.Visibility = isSheet ? Visibility.Visible : Visibility.Collapsed;
		ShellColumnSplitter.Visibility = isSheet ? Visibility.Collapsed : Visibility.Visible;

		Grid.SetColumn(unitTestContentRoot, isSheet ? 0 : 2);
		Grid.SetColumnSpan(unitTestContentRoot, isSheet ? 3 : 1);
		Grid.SetRowSpan(unitTestContentRoot, isSheet ? 1 : 2);

		// The sheet is translucent, so the automation-only texts go transparent instead of hiding behind opaque layers.
		foreach (var layer in new Panel[] { ShellFailureLayer, ShellResultsLayer })
		{
			if (isSheet)
			{
				layer.Background = _transparentBrush;
			}
			else
			{
				layer.ClearValue(Panel.BackgroundProperty);
			}
		}

		foreach (var text in new[] { failedTestDetails, failedTests, runningState })
		{
			if (isSheet)
			{
				text.Foreground = _transparentBrush;
			}
			else
			{
				text.ClearValue(TextBlock.ForegroundProperty);
			}
		}

		if (isSheet && Application.Current.Resources.TryGetValue("OverlayCornerRadius", out var value) && value is CornerRadius radius)
		{
			ShellSheetSurface.CornerRadius = new CornerRadius(radius.TopLeft, radius.TopRight, 0, 0);
		}

		ShellSheetSurface.Shadow = isSheet ? new ThemeShadow() : null;
		ShellSheetSurface.Translation = isSheet ? new Vector3(0, 0, 32) : Vector3.Zero;

		// The trace wraps in the sheet: horizontal scrolling on a phone hides most of each line.
		ShellFailureScroller.HorizontalScrollMode = isSheet ? ScrollMode.Disabled : ScrollMode.Auto;
		ShellFailureScroller.HorizontalScrollBarVisibility = isSheet ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;

		// The bar sits right above the header: no gap for a title that is not there.
		var headerPadding = ShellRunHeaderPanel.Padding;
		ShellRunHeaderPanel.Padding = new Thickness(headerPadding.Left, isSheet ? 4 : 12, headerPadding.Right, headerPadding.Bottom);

		if (!isSheet)
		{
			ShellSheet.ClearValue(HeightProperty);
			ShellSheetTransform.Y = 0;
			ShellSheetPeekRow.Height = new GridLength(0);
			ShellRunnerRoot.Clip = null;
			ShellSheetSurface.Margin = default;
		}

		SetSheetContentShown(!isSheet);

		// The bar already says "Ready"; the InfoBar replaces this line once a run has started.
		runStatus.Visibility = isSheet || ShellRunInfoBar is not null ? Visibility.Collapsed : Visibility.Visible;

		_runLayout = null;
		if (ShellRunHeaderPanel.ActualWidth > 0)
		{
			ApplyRunLayout(GetRunLayout(ShellRunHeaderPanel.ActualWidth));
		}

		ApplyStatCards(_statsMode, force: true);
		UpdateSheetStatus();
		UpdateSheetRunHint();
		UpdateSheetLock();
	}

	// Run and Stop, and the counters, live in the always-visible bar of the sheet; the same elements, not copies.
	private void MoveRunControls(bool toSheet)
	{
		foreach (var element in new FrameworkElement[] { runButton, stopButton, ShellRunStats })
		{
			if (element.Parent is Panel parent)
			{
				parent.Children.Remove(element);
			}
		}

		// The chips line up under the status text, next to Run and Stop.
		Grid.SetRow(runButton, toSheet ? 1 : 0);
		Grid.SetRow(stopButton, toSheet ? 1 : 0);
		Grid.SetRow(ShellRunStats, toSheet ? 2 : 0);
		Grid.SetColumn(ShellRunStats, toSheet ? 2 : 0);
		Grid.SetColumnSpan(ShellRunStats, toSheet ? 2 : 1);
		Grid.SetRowSpan(runButton, toSheet ? 2 : 1);
		Grid.SetRowSpan(stopButton, toSheet ? 2 : 1);

		if (toSheet)
		{
			// After the grip, before the status and the handle: Tab follows the visual order.
			ShellSheetBar.Children.Insert(1, runButton);
			ShellSheetBar.Children.Insert(2, stopButton);
			ShellSheetBar.Children.Add(ShellRunStats);
		}
		else
		{
			ShellRunToolbar.Children.Insert(0, runButton);
			ShellRunToolbar.Children.Insert(1, stopButton);
			ShellRunHeaderPanel.Children.Add(ShellRunStats);
		}

		foreach (var button in new[] { runButton, stopButton })
		{
			button.VerticalAlignment = toSheet ? VerticalAlignment.Center : VerticalAlignment.Stretch;
			button.MinWidth = button.MinHeight = toSheet || ShellFunctions.IsTouchShell ? TouchTargetSize : 0;
		}
	}

	private void OnSheetBarSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_sheetBarHeight = e.NewSize.Height;
		if (_isSheet)
		{
			UpdateSheetGeometry(new Size(ActualWidth, ActualHeight));
		}
	}

	private void UpdateSheetGeometry(Size size)
	{
		_sheetExpandedHeight = Math.Max(_sheetBarHeight, Math.Round(size.Height * SheetExpandedRatio));
		ShellSheet.Height = _sheetExpandedHeight;
		ShellSheetPeekRow.Height = new GridLength(_sheetBarHeight);

		// The surface runs on under the system navigation bar; the controls stay above it.
		var bottomInset = GetBottomInset(size.Height);
		ShellSheetSurface.Margin = new Thickness(0, 0, 0, -bottomInset);
		ShellRunnerRoot.Clip = new RectangleGeometry { Rect = new Rect(0, 0, size.Width, size.Height + bottomInset) };

		if (_sheetStoryboard is null && !_isSheetDragging)
		{
			ShellSheetTransform.Y = _isSheetExpanded ? 0 : SheetCollapsedOffset;
		}

		UpdateHeaderMaxHeight();
	}

	// Space between the runner's bottom and the window's, when only the visible-bounds inset is there.
	private double GetBottomInset(double height)
	{
#if WINAPPSDK
		return 0;
#else
		try
		{
			if (XamlRoot is not { } root || _applicationView is not { } view)
			{
				return 0;
			}

			var windowInset = root.Size.Height - view.VisibleBounds.Bottom;
			if (windowInset < 1)
			{
				return 0;
			}

			var bottom = TransformToVisual(null).TransformPoint(new Point(0, height)).Y;
			var gap = root.Size.Height - bottom;
			return gap > 0 && gap <= windowInset + 1 ? gap : 0;
		}
		catch (Exception)
		{
			return 0;
		}
#endif
	}

	private void SetSheetExpanded(bool isExpanded, bool animate)
	{
		if (!_isSheet || (isExpanded && IsSheetLocked))
		{
			return;
		}

		if (!isExpanded)
		{
			// Before the body is disabled: focus must not stay on hidden, disabled content.
			MoveFocusOutOfSheetBody();
		}

		_isSheetExpanded = isExpanded;
		ShellSheetHandle.IsExpanded = isExpanded;
		ShellSheetHandleIcon.Glyph = isExpanded ? "" : "";
		ToolTipService.SetToolTip(ShellSheetHandle, isExpanded ? "Hide test details" : "Show test details");
		UpdateSheetRunHint();

		if (isExpanded)
		{
			SetSheetContentShown(true);
		}

		AnimateSheetTo(isExpanded ? 0 : SheetCollapsedOffset, animate && !IsSheetLocked);
	}

	private void MoveFocusOutOfSheetBody()
	{
		if (XamlRoot is not { } root || FocusManager.GetFocusedElement(root) is not DependencyObject focused || !IsInSheetBody(focused))
		{
			return;
		}

		var state = focused is Control { FocusState: not FocusState.Unfocused and var current } ? current : FocusState.Programmatic;
		foreach (var target in new Control[] { ShellSheetHandle, runButton, stopButton })
		{
			if (target.Focus(state))
			{
				return;
			}
		}
	}

	private bool IsInSheetBody(DependencyObject element)
	{
		for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
		{
			if (current == ShellSheetBody)
			{
				return true;
			}
		}

		return false;
	}

	private void AnimateSheetTo(double offset, bool animate)
	{
		StopSheetAnimation();

		var from = ShellSheetTransform.Y;
		ShellSheetTransform.Y = offset;
		if (!animate || !IsSheetAnimationEnabled || !AreAnimationsEnabled() || Math.Abs(from - offset) < 1)
		{
			OnSheetSettled();
			return;
		}

		ExponentialEase easing = new() { EasingMode = EasingMode.EaseOut, Exponent = 6 };
		DoubleAnimation animation = new()
		{
			From = from,
			To = offset,
			Duration = TimeSpan.FromMilliseconds(SheetAnimationMilliseconds),
			EasingFunction = easing,
		};
		Storyboard.SetTarget(animation, ShellSheetTransform);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.Y));

		Storyboard storyboard = new() { Children = { animation } };
		storyboard.Completed += (_, _) =>
		{
			if (_sheetStoryboard == storyboard)
			{
				_sheetStoryboard = null;
				storyboard.Stop();
				OnSheetSettled();
			}
		};
		_sheetStoryboard = storyboard;
		_sheetAnimationFrom = from;
		_sheetAnimationTo = offset;
		_sheetAnimationEasing = easing;
		_sheetAnimationClock.Restart();
		storyboard.Begin();
	}

	// A collapsed sheet keeps its content out of the tab order and away from screen readers.
	private void OnSheetSettled() => SetSheetContentShown(!_isSheet || _isSheetExpanded);

	// failedTestDetails, failedTests and runningState stay: automation reads them even while the sheet is collapsed.
	private void SetSheetContentShown(bool isShown)
	{
		_isSheetContentShown = isShown;
		ShellSheetBody.IsEnabled = isShown;
		var visibility = isShown ? Visibility.Visible : Visibility.Collapsed;
		ShellRunHeaderScroller.Visibility = ShellFailureLayer.Visibility = ShellResultsLayer.Visibility = visibility;
		UpdateRowSplitterVisibility();
	}

	// In the sheet, the splitter only shows when there are failure details to resize.
	private void UpdateRowSplitterVisibility()
		=> ShellRowSplitter.Visibility = !_isSheet || (_isSheetContentShown && failedTestDetailsRow.Height.Value > 0)
			? Visibility.Visible
			: Visibility.Collapsed;

	private void StopSheetAnimation()
	{
		if (_sheetStoryboard is { } storyboard)
		{
			// Read the eased position from the clock: an independent animation may report its target as the value.
			var current = GetAnimatedSheetOffset();
			_sheetStoryboard = null;
			storyboard.Stop();
			ShellSheetTransform.Y = current;
		}
	}

	private double GetAnimatedSheetOffset()
	{
		var progress = Math.Clamp(_sheetAnimationClock.Elapsed.TotalMilliseconds / SheetAnimationMilliseconds, 0, 1);
		var eased = _sheetAnimationEasing?.Ease(progress) ?? progress;
		return _sheetAnimationFrom + (_sheetAnimationTo - _sheetAnimationFrom) * eased;
	}

	private static bool AreAnimationsEnabled()
	{
		try
		{
			return new UISettings().AnimationsEnabled;
		}
		catch (Exception)
		{
			return true;
		}
	}

	private void OnSheetHandleClick(object sender, RoutedEventArgs e)
	{
		if (!_isSheetDragging)
		{
			SetSheetExpanded(!_isSheetExpanded);
		}
	}

	private void OnSheetBarTapped(object sender, TappedRoutedEventArgs e)
	{
		// Run, Stop and the handle act on their own.
		for (var element = e.OriginalSource as DependencyObject; element is not null && element != ShellSheetBar; element = VisualTreeHelper.GetParent(element))
		{
			if (element is ButtonBase)
			{
				return;
			}
		}

		e.Handled = true;
		SetSheetExpanded(!_isSheetExpanded);
	}

	private void OnSheetKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Escape && _isSheetExpanded)
		{
			e.Handled = true;
			SetSheetExpanded(false);
			ShellSheetHandle.Focus(FocusState.Keyboard);
		}
	}

	// A drag is pointer-driven: manipulation positions are relative to the moving bar and differ between Uno and WinUI.
	private void OnSheetBarPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		var point = e.GetCurrentPoint(null);
		if (!_isSheet || IsSheetLocked || _sheetPointerId is not null || (point.PointerDeviceType == PointerDeviceType.Mouse && !point.Properties.IsLeftButtonPressed))
		{
			return;
		}

		_sheetPointerId = e.Pointer.PointerId;
		_sheetDragStartY = point.Position.Y;
		_isSheetDragging = false;
	}

	private void OnSheetBarPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (e.Pointer.PointerId != _sheetPointerId)
		{
			return;
		}

		var y = e.GetCurrentPoint(null).Position.Y;
		if (!_isSheetDragging)
		{
			if (Math.Abs(y - _sheetDragStartY) < SheetDragThreshold)
			{
				return;
			}

			// Uno does not hand over a capture that Run, Stop or the handle hold; their moves still bubble here, and their Click checks the drag.
			ShellSheetBar.CapturePointer(e.Pointer);

			StopSheetAnimation();
			SetSheetContentShown(true);
			_isSheetDragging = true;
			_sheetDragStartOffset = _sheetDragLastOffset = ShellSheetTransform.Y;
			_sheetDragVelocity = 0;
			_sheetDragClock.Restart();
		}

		e.Handled = true;

		var offset = _sheetDragStartOffset + y - _sheetDragStartY;
		var elapsed = _sheetDragClock.Elapsed.TotalMilliseconds;
		if (elapsed > 0)
		{
			_sheetDragVelocity = 0.6 * ((offset - _sheetDragLastOffset) / elapsed) + 0.4 * _sheetDragVelocity;
		}

		_sheetDragLastOffset = offset;
		_sheetDragClock.Restart();
		ShellSheetTransform.Y = Math.Clamp(offset, 0, SheetCollapsedOffset);
	}

	// Run, Stop and the handle lose their capture when a drag starts; only the bar's own loss ends the drag.
	private void OnSheetBarPointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
		if (ReferenceEquals(e.OriginalSource, ShellSheetBar))
		{
			OnSheetBarPointerEnded(sender, e);
		}
	}

	private void OnSheetBarPointerEnded(object sender, PointerRoutedEventArgs e)
	{
		if (e.Pointer.PointerId != _sheetPointerId)
		{
			return;
		}

		_sheetPointerId = null;
		if (!_isSheetDragging)
		{
			return;
		}

		e.Handled = true;
		ShellSheetBar.ReleasePointerCapture(e.Pointer);

		// The Click that may follow the release still sees the drag; the next one does not.
		DispatcherQueue.TryEnqueue(() => _isSheetDragging = false);

		if (_isSheet && !IsSheetLocked)
		{
			var velocity = _sheetDragClock.Elapsed.TotalMilliseconds > 100 ? 0 : _sheetDragVelocity;
			SetSheetExpanded(GetSheetSnapExpanded(ShellSheetTransform.Y, SheetCollapsedOffset, velocity));
		}
	}

	/// <summary>A flick decides by its direction, a slow drag by the half-way point.</summary>
	internal static bool GetSheetSnapExpanded(double offset, double collapsedOffset, double velocityY)
		=> Math.Abs(velocityY) >= SheetFlickVelocity ? velocityY < 0 : offset < collapsedOffset / 2;

	private void OnRunStarting()
	{
		_isRunActive = true;
		_runOutcomeTitle = null;

		// Injected test input must never land on the sheet.
		SetSheetExpanded(false, animate: false);
		UpdateSheetStatus();
	}

	private void OnRunEnded(string? outcomeTitle)
	{
		_isRunActive = false;
		_runOutcomeTitle = outcomeTitle;

		if (_isSheetLayoutPending)
		{
			ApplySheetLayout(new Size(ActualWidth, ActualHeight));
		}

		UpdateSheetStatus();
		UpdateSheetRunHint();
	}

	internal static string FormatSheetStatus(bool isRunning, int done, int planned, string? outcome)
		=> isRunning
			? planned > 0
				? $"Running {Math.Min(done, planned).ToString("N0", CultureInfo.CurrentCulture)}/{planned.ToString("N0", CultureInfo.CurrentCulture)}"
				: "Running"
			: outcome ?? "Ready";

	private void UpdateSheetStatus()
	{
		if (!_isSheet)
		{
			return;
		}

		var planned = PlannedTestCount;
		var done = Volatile.Read(ref _progressCount);
		ShellSheetStatus.Text = FormatSheetStatus(_isRunActive, done, planned, _runOutcomeTitle);

		// No progress on CI: nothing there updates it per test.
		var showProgress = _isRunActive && !_isRunningOnCICache;
		ShellSheetProgress.Opacity = showProgress ? 1 : 0;
		ShellSheetProgress.IsIndeterminate = showProgress && planned <= 0;
		ShellSheetProgress.Maximum = Math.Max(1, planned);
		ShellSheetProgress.Value = Math.Min(done, Math.Max(1, planned));
	}

	private void UpdateSheetRunHint()
		=> ShellSheetRunHint.Visibility = _isSheet && _isSheetExpanded && _isRunActive ? Visibility.Visible : Visibility.Collapsed;

	private double SheetBodyHeight => _sheetExpandedHeight - _sheetBarHeight;

	/// <summary>The results card keeps a header and whole rows.</summary>
	internal double SheetResultsHeight
		=> (ShellResultsHeader.ActualHeight > 0 ? ShellResultsHeader.ActualHeight : SheetResultsHeaderEstimate)
			+ SheetMinResultRows * ResultRowHeight
			+ SheetResultsChrome;

	// Failure details shrink before the results do.
	private double GetSheetFailureDetailsHeight()
		=> _sheetExpandedHeight <= 0
			? SheetFailureDetailsHeight
			: Math.Clamp(SheetBodyHeight - SheetResultsHeight - SheetMinHeaderHeight, SheetMinFailureDetailsHeight, SheetFailureDetailsHeight);

	private double GetSheetHeaderMaxHeight()
		=> Math.Max(SheetMinHeaderHeight, SheetBodyHeight - failedTestDetailsRow.Height.Value - SheetResultsHeight);
}
