#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.Samples.Entities;
using Uno.UI.Samples.Helper;
using Windows.Foundation;

namespace Uno.UI.Samples.Controls;

partial class SampleChooserControl
{
	private const double RailWidth = 48;
	private const double OverlayDismissGutter = 48;

	private const double MinTitleWidth = 96;

	// On a phone the sample name is the main cue, so about 16 characters stay before commands give way.
	private const double NarrowMinTitleWidth = 144;
	private const double CompactCommandWidth = 40;

	// Used until the element has been laid out in the bar once.
	private const double FallbackSeparatorWidth = 17;
	private const double FallbackBarChromeWidth = 48;
	private const double FallbackLabelPadding = 52;

	private const double TouchRowHeight = 40;

	private SampleChooserViewModel? _shellViewModel;
	private bool _syncingRail;
	private bool _isNarrow;
	private bool _isHeaderLocationQueued;
	private bool _isHeaderContentDirty = true;
	private (double Title, double Tag, double Breadcrumb) _headerContentWidths;

	private AppBarButton[] _iconOnlyCommands = [];
	private Control[] _labelledCommands = [];
	private (DependencyObject Element, string CommandId)[] _menuShortcuts = [];
	private readonly Dictionary<FrameworkElement, double> _barWidths = new();
	private double _barChromeWidth = FallbackBarChromeWidth;
	private bool _isQuickSettingsFlyoutDetached;

	/// <summary>False on touch, where keyboard shortcut hints mean nothing. Per window, so suggestions re-evaluate when it flips.</summary>
	public bool ShowShortcutHints
	{
		get => (bool)GetValue(ShowShortcutHintsProperty);
		set => SetValue(ShowShortcutHintsProperty, value);
	}

	public static DependencyProperty ShowShortcutHintsProperty { get; } =
		DependencyProperty.Register(nameof(ShowShortcutHints), typeof(bool), typeof(SampleChooserControl), new PropertyMetadata(true));

	private void InitializeShell()
	{
		_iconOnlyCommands = [ShellPreviousSampleButton, ShellNextSampleButton, ShellReloadSampleButton, OverflowSettingsButton];
		_labelledCommands = [ShellFavoriteToggle, InfoButton];
		_menuShortcuts =
		[
			(ShellPreviousSampleButton, ShellCommands.PreviousSample),
			(ShellNextSampleButton, ShellCommands.NextSample),
			(ShellReloadSampleButton, ShellCommands.ReloadSample),
			(ShellFavoriteToggle, ShellCommands.ToggleFavorite),
			(InfoButton, ShellCommands.ShowSampleInfo),
			(ShellCopyLinkButton, ShellCommands.CopySampleLink),
			(ShellFocusModeButton, ShellCommands.ToggleFocusMode),
			(ShellQuickHelpItem, ShellCommands.OpenHelp),
			(ShellQuickAllSettingsItem, ShellCommands.ShowSettings),
		];

		ApplyShortcutHints();
		InitializeRowHeight();

		// On touch a long press would select the title instead of showing its tooltip.
		ShellSampleTitle.IsTextSelectionEnabled = !ShellFunctions.IsTouchPlatform;
		ShellLayoutStates.CurrentStateChanged += (_, _) => UpdateLayoutState();
		ShellHeader.SizeChanged += (_, _) => QueueHeaderLocationUpdate();
		InitializeCommandSizes();

		ShellOpenInNewWindowButton.Visibility = ShellFunctions.Visible(SampleChooserViewModel.CanCreateNewWindow);
		ShellLogViewDumpButton.Visibility = ShellFunctions.Visible(SampleChooserViewModel.IsDebug);

		DataContextChanged += OnShellDataContextChanged;
		Loaded += OnShellLoaded;
		ActualThemeChanged += (_, _) =>
		{
			UpdateFavoriteIcon();
			SyncRailSelection();
			UpdateManualTestsChip();
		};

		ShellPaneBenchmarksButton.RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdatePaneDestinationColumns());
		UpdatePaneDestinationColumns();

		InitializeBrowserPane();
	}

	private void UpdatePaneDestinationColumns()
		=> ShellPaneBenchmarksColumn.Width = ShellPaneBenchmarksButton.Visibility == Visibility.Visible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

	private void OnShellLoaded(object sender, RoutedEventArgs e)
	{
		if (ShellRail.SettingsItem is NavigationViewItem settingsItem)
		{
			AutomationProperties.SetAutomationId(settingsItem, "ShellRailSettings");
			SetShortcutHint(settingsItem, ShellCommands.ShowSettings, "Settings");
		}

		UpdateChromeState(useTransitions: false);
		UpdateLayoutState();
		SyncRailSelection();
		UpdateFavoriteIcon();
		UpdateInputHints();
		QueueHeaderLocationUpdate(contentChanged: true);
	}

	private void OnShellDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
	{
		if (_shellViewModel is not null)
		{
			_shellViewModel.PropertyChanged -= OnShellViewModelPropertyChanged;
		}

		_shellViewModel = DataContext as SampleChooserViewModel;

		if (_shellViewModel is not null)
		{
			_shellViewModel.PropertyChanged += OnShellViewModelPropertyChanged;
		}

		UpdateChromeState(useTransitions: false);
		SyncRailSelection();
		UpdateFavoriteIcon();
		UpdateSampleCommands();
		UpdateBrowserToggle();
		UpdateRowHeight();
		UpdateSearchResults();
		UpdateManualTestsChip();
	}

	private void OnShellViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(SampleChooserViewModel.IsShellChromeVisible):
			case nameof(SampleChooserViewModel.IsAutomationRun):
				UpdateChromeState(useTransitions: false);
				UpdatePaneLength();
				SyncRailSelection();
				break;

			case nameof(SampleChooserViewModel.ShellDestination):
				SyncRailSelection();
				break;

			case nameof(SampleChooserViewModel.IsFavoritedSample):
				UpdateFavoriteIcon();
				break;

			case nameof(SampleChooserViewModel.CurrentBreadcrumb):
				UpdateSampleCommands();
				QueueHeaderLocationUpdate(contentChanged: true);
				break;

			case nameof(SampleChooserViewModel.IsSplitVisible):
				UpdateBrowserToggle();
				BringLibraryRowIntoView();
				QueueHeaderLocationUpdate(contentChanged: true);
				break;

			case nameof(SampleChooserViewModel.SearchResultsGrouped):
				UpdateSearchResults();
				break;

			case nameof(SampleChooserViewModel.SearchTerm):
				SyncSearchText();
				break;

			case nameof(SampleChooserViewModel.ManualTestsOnly):
				UpdateManualTestsChip();
				break;

			case nameof(SampleChooserViewModel.SelectedLibrarySample):
				BringLibraryRowIntoView();
				break;

			case nameof(SampleChooserViewModel.CurrentSelectedSample):
			case nameof(SampleChooserViewModel.IsHomeVisible):
				SyncSearchResultSelection();
				QueueHeaderLocationUpdate(contentChanged: true);
				break;

#if HAS_UNO
			case nameof(SampleChooserViewModel.SimulateTouch):
				UpdateRowHeight();
				UpdateInputHints();
				break;
#endif
		}
	}

	private void UpdateChromeState(bool useTransitions)
	{
		var state = _shellViewModel switch
		{
			{ IsShellChromeVisible: false } => "ChromeHiddenState",
			{ IsAutomationRun: true } => "ChromeAutomationState",
			_ => "ChromeNormalState",
		};

		VisualStateManager.GoToState(this, state, useTransitions);

		// Phones reach the rail's destinations from the browser pane; the hidden pane keeps the zero-width rail from drawing over the content.
		// Matches the chrome states' setters, so the result never depends on setter vs local precedence (WinUI kept this local True).
		var hasRail = state == "ChromeNormalState" && !_isNarrow;
		// TODO Uno: the rail is zeroed, never collapsed or raised (see uno-issues: uno--collapsed-navigationview-breaks-highcontrast-adjustment.md, uno--zero-width-navigationview-above-content-swallows-touch-exit.md)
		ShellRailColumn.Width = new GridLength(hasRail ? RailWidth : 0);
		ShellRail.IsPaneVisible = hasRail;

		// Without the rail the host runs edge to edge, so only its top stroke remains.
		ShellHostLayer.CornerRadius = !_isNarrow && Resources.TryGetValue("ShellHostCornerRadius", out var radius) && radius is CornerRadius r ? r : default;
		ShellHostEdge.CornerRadius = ShellHostLayer.CornerRadius;
		ShellHostEdge.BorderThickness = new Thickness(_isNarrow ? 0 : 1, 1, 0, 0);

		// A hidden overlay pane would keep its light-dismiss layer over the sample.
		if (state == "ChromeHiddenState"
			&& SplitView.DisplayMode is SplitViewDisplayMode.Overlay or SplitViewDisplayMode.CompactOverlay
			&& _shellViewModel is { IsSplitVisible: true } vm)
		{
			vm.IsSplitVisible = false;
		}
	}

	// Tool pages (Help, Playground, the runner...) are not part of a category, so the sample navigation does not apply.
	private void UpdateSampleCommands()
	{
		var visibility = ShellFunctions.Visible(_shellViewModel?.CurrentBreadcrumb is not ["Tools"]);

		ShellPreviousSampleButton.Visibility = visibility;
		ShellNextSampleButton.Visibility = visibility;
		ShellReloadSampleButton.Visibility = visibility;
		ShellFavoriteToggle.Visibility = visibility;
		UpdateCommandSeparators();
	}

	private void SyncRailSelection()
	{
		if (_shellViewModel is not { } vm)
		{
			return;
		}

		object? item = vm.ShellDestination switch
		{
			ShellDestination.Home => ShellRailHome,
			ShellDestination.RuntimeTests => ShellRailRuntimeTests,
			ShellDestination.Benchmarks => ShellRailBenchmarks,
			ShellDestination.Playground => ShellRailPlayground,
			ShellDestination.Help => ShellRailHelp,
			ShellDestination.Settings => ShellRail.SettingsItem,
			_ => ShellRailSamples,
		};

		_syncingRail = true;
		try
		{
			ShellRail.SelectedItem = item;
		}
		finally
		{
			_syncingRail = false;
		}

		var current = vm.ShellDestination.ToString();
		foreach (var button in PaneDestinationButtons)
		{
			if (Equals(button.Tag, current) && ShellThemeBrushes.Get("SubtleFillColorSecondaryBrush", ActualTheme) is { } selected)
			{
				button.Background = selected;
				AutomationProperties.SetItemStatus(button, "Current");
			}
			else
			{
				button.ClearValue(BackgroundProperty);
				button.ClearValue(AutomationProperties.ItemStatusProperty);
			}
		}
	}

	private Button[] PaneDestinationButtons =>
		[ShellPaneHomeButton, ShellPaneRuntimeTestsButton, ShellPaneBenchmarksButton, ShellPanePlaygroundButton, ShellPaneHelpButton, ShellPaneSettingsButton];

	// Items act as commands, so invoking the selected item again (e.g. Runtime tests) re-runs it.
	private void ShellRail_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (_shellViewModel is not { } vm)
		{
			return;
		}

		Navigate(vm, args.IsSettingsInvoked ? "Settings" : (args.InvokedItemContainer as FrameworkElement)?.Tag as string);
	}

	private void ShellDestinationButton_Click(object sender, RoutedEventArgs e)
	{
		if (_shellViewModel is not { } vm || (sender as FrameworkElement)?.Tag is not string destination)
		{
			return;
		}

		Navigate(vm, destination);

		// Settings lives in the pane itself; every other destination replaces the content behind the overlay.
		if (destination != "Settings" && SplitView.DisplayMode is SplitViewDisplayMode.Overlay or SplitViewDisplayMode.CompactOverlay)
		{
			vm.IsSplitVisible = false;
		}
	}

	private static void Navigate(SampleChooserViewModel vm, string? destination)
	{
		switch (destination)
		{
			case "Settings":
				Run(vm.ShowSettingsCommand);
				break;
			case "Home":
				Run(vm.ShowHomeCommand);
				break;
			case "Samples":
				vm.BrowserView = BrowserView.Samples;
				vm.IsSplitVisible = true;
				break;
			case "RuntimeTests":
				Run(vm.OpenRuntimeTestsCommand);
				break;
			case "Benchmarks":
				Run(vm.OpenBenchmarksCommand);
				break;
			case "Playground":
				Run(vm.OpenPlaygroundCommand);
				break;
			case "Help":
				Run(vm.OpenHelpCommand);
				break;
		}
	}

	// The rail selects what was invoked; the destination may differ (Samples only opens the browser), so it snaps back.
	private void ShellRail_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		if (!_syncingRail)
		{
			DispatcherQueue.TryEnqueue(SyncRailSelection);
		}
	}

	private void ShellRoot_SizeChanged(object sender, SizeChangedEventArgs e) => UpdatePaneLength();

	private void UpdateLayoutState()
	{
		var isNarrow = ShellLayoutStates.CurrentState?.Name == "NarrowState";
		if (isNarrow != _isNarrow)
		{
			_isNarrow = isNarrow;
			UpdateChromeState(useTransitions: false);
			UpdatePaneLength();
			UpdateBrowserToggle();
			UpdateBrowserTabs();
		}

		UpdateInputHints();
		UpdateCommandSizes();
		QueueHeaderLocationUpdate(contentChanged: true);
	}

	private void InitializeCommandSizes()
	{
		// IsInOverflow raises no change notification, so sizes follow the overflow menu and its item moves.
		ShellCommandBar.Opening += (_, _) =>
		{
			ReleaseOverflowCommandWidths();
			DetachQuickSettingsFlyoutInOverflow();
		};
		ShellCommandBar.Closed += (_, _) =>
		{
			UpdateCommandSizes();

			// The bar closes inside the overflowed button's click, before the button opens its flyout as a submenu.
			DispatcherQueue.TryEnqueue(ReattachQuickSettingsFlyout);
		};
		ShellCommandBar.DynamicOverflowItemsChanging += (_, _) => DispatcherQueue.TryEnqueue(OnOverflowItemsChanged);
		UpdateCommandSizes();
	}

	// The event fires before the items move, so this runs once they have.
	private void OnOverflowItemsChanged()
	{
		UpdateCommandSizes();
		if (ShellCommandBar.IsOpen)
		{
			ReleaseOverflowCommandWidths();
		}

		UpdateCommandSeparators();
		QueueHeaderLocationUpdate();
	}

	// Icon-only commands are square in the bar; Favorite and Info carry their label only in the wide layout.
	private void UpdateCommandSizes()
	{
		foreach (var command in _iconOnlyCommands)
		{
			command.Width = CompactCommandWidth;
		}

		var isWide = IsWideLayout;
		foreach (var command in _labelledCommands)
		{
			if (isWide)
			{
				command.ClearValue(WidthProperty);
			}
			else
			{
				command.Width = CompactCommandWidth;
			}
		}
	}

	private bool IsWideLayout => ShellLayoutStates.CurrentState?.Name == "WideState";

	// A local width would beat the overflow menu's stretching style and leave the item a blank 40 px cell.
	private void ReleaseOverflowCommandWidths()
	{
		foreach (var command in ShellCommandBar.PrimaryCommands)
		{
			if (command is AppBarButton { IsInOverflow: true } or AppBarToggleButton { IsInOverflow: true })
			{
				((Control)command).ClearValue(WidthProperty);
			}
		}
	}

	// Separators the bar did not move would otherwise dangle next to "..." or double up.
	private void UpdateCommandSeparators()
	{
		var commands = ShellCommandBar.PrimaryCommands;
		var layout = new CommandSlot[commands.Count];
		for (var i = 0; i < commands.Count; i++)
		{
			layout[i] = commands[i] is AppBarSeparator
				? CommandSlot.Separator
				: IsShownInBar(commands[i]) ? CommandSlot.Shown : CommandSlot.Hidden;
		}

		var needed = GetNeededSeparators(layout);
		for (var i = 0; i < commands.Count; i++)
		{
			if (commands[i] is AppBarSeparator separator)
			{
				separator.Visibility = ShellFunctions.Visible(needed[i]);
			}
		}
	}

	private static bool IsShownInBar(ICommandBarElement command)
		=> command is FrameworkElement { Visibility: Visibility.Visible }
			and not AppBarButton { IsInOverflow: true }
			and not AppBarToggleButton { IsInOverflow: true };

	internal enum CommandSlot
	{
		Hidden,
		Shown,
		Separator,
	}

	/// <summary>A separator is needed between two shown commands, and only once.</summary>
	internal static bool[] GetNeededSeparators(IReadOnlyList<CommandSlot> layout)
	{
		var needed = new bool[layout.Count];
		var hasCommandBefore = false;
		var pending = -1;

		for (var i = 0; i < layout.Count; i++)
		{
			switch (layout[i])
			{
				case CommandSlot.Separator when hasCommandBefore && pending < 0:
					pending = i;
					break;
				case CommandSlot.Shown:
					if (pending >= 0)
					{
						needed[pending] = true;
						pending = -1;
					}

					hasCommandBefore = true;
					break;
			}
		}

		return needed;
	}

	// An overflowed flyout button opens a submenu that cascades over the overflow menu, which phones cannot fit.
	private void DetachQuickSettingsFlyoutInOverflow()
	{
		if (OverflowSettingsButton.IsInOverflow)
		{
			OverflowSettingsButton.Flyout = null;
			_isQuickSettingsFlyoutDetached = true;
		}
	}

	private void ReattachQuickSettingsFlyout()
	{
		if (_isQuickSettingsFlyoutDetached)
		{
			OverflowSettingsButton.Flyout = ShellQuickSettingsFlyout;
			_isQuickSettingsFlyoutDetached = false;
		}
	}

	private void OverflowSettingsButton_Click(object sender, RoutedEventArgs e)
	{
		// Wait for the overflow menu to close so the flyout opens on its own; a detached flyout would lose the shell theme.
		if (_isQuickSettingsFlyoutDetached)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				ReattachQuickSettingsFlyout();
				ShellQuickSettingsFlyout.ShowAt(GetCommandAnchor(OverflowSettingsButton));
			});
		}
	}

	// Bindings update the title, tag and breadcrumb on the same notifications, so measure once they have.
	private void QueueHeaderLocationUpdate(bool contentChanged = false)
	{
		_isHeaderContentDirty |= contentChanged;
		if (!_isHeaderLocationQueued)
		{
			_isHeaderLocationQueued = DispatcherQueue.TryEnqueue(() =>
			{
				_isHeaderLocationQueued = false;
				UpdateHeaderLocation();
			});
		}
	}

	// Priority: the commands, a minimum title, the Manual tag, the rest of the title, then the breadcrumb (shown only whole).
	private void UpdateHeaderLocation()
	{
		var state = ShellLayoutStates.CurrentState?.Name;
		var titleMax = state switch
		{
			"WideState" => 480,
			"TabletState" => 260,
			_ => 200,
		};

		if (_isHeaderContentDirty)
		{
			_isHeaderContentDirty = false;
			ShellSampleTitle.MaxWidth = titleMax;
			_headerContentWidths = (
				MeasureWidth(ShellSampleTitle),
				MeasureWidth(ShellManualTag),
				state == "WideState" ? MeasureWidth(ShellBreadcrumbPanel) : 0);
		}

		var search = SearchBox.Visibility == Visibility.Visible ? SearchBox.Width : ShellSearchButton.Width;
		var toggle = ShellBrowserToggle.Visibility == Visibility.Visible ? ShellBrowserToggle.Width : 0;
		var available = ShellHeader.ActualWidth - ShellHeader.Padding.Left - ShellHeader.Padding.Right - 3 * ShellHeader.ColumnSpacing - toggle - search;

		var (title, tag, breadcrumb) = _headerContentWidths;
		var minTitle = state is "WideState" or "TabletState" ? MinTitleWidth : NarrowMinTitleWidth;
		var fit = FitHeaderLocation(available - GetCommandsReserve(), title, tag, breadcrumb, titleMax, minTitle);
		ShellBreadcrumbHost.Visibility = ShellFunctions.Visible(fit.ShowBreadcrumb);
		ShellManualTagHost.Visibility = ShellFunctions.Visible(fit.ShowTag);
		ShellSampleTitle.MaxWidth = fit.TitleMaxWidth;
	}

	// The width the shown commands need in the bar, so the title only gets what they leave.
	private double GetCommandsReserve()
	{
		if (ShellCommandBar.Visibility != Visibility.Visible)
		{
			return 0;
		}

		var isWide = IsWideLayout;
		var commands = ShellCommandBar.PrimaryCommands;
		var layout = new CommandSlot[commands.Count];
		for (var i = 0; i < commands.Count; i++)
		{
			// Every command counts as shown here: the reserve is what they need, not what currently fits.
			layout[i] = commands[i] switch
			{
				AppBarSeparator => CommandSlot.Separator,
				FrameworkElement { Visibility: Visibility.Visible } => CommandSlot.Shown,
				_ => CommandSlot.Hidden,
			};
		}

		var needed = GetNeededSeparators(layout);
		double reserve = 0;
		double displayed = 0;
		var isAnyInOverflow = false;

		for (var i = 0; i < commands.Count; i++)
		{
			if (commands[i] is not FrameworkElement element
				|| (layout[i] == CommandSlot.Separator ? !needed[i] : layout[i] == CommandSlot.Hidden))
			{
				continue;
			}

			var isDisplayed = IsShownInBar(commands[i]) && element.ActualWidth > 0;
			if (isDisplayed)
			{
				displayed += element.ActualWidth;
			}
			else if (layout[i] == CommandSlot.Shown)
			{
				isAnyInOverflow = true;
			}

			reserve += GetBarWidth(element, isWide, isDisplayed);
		}

		// The bar's padding and its "..." button, measured while every command is in the bar.
		if (!isAnyInOverflow && ShellCommandBar.ActualWidth > displayed)
		{
			_barChromeWidth = ShellCommandBar.ActualWidth - displayed;
		}

		// Slack so the last command does not overflow over a rounding error.
		return reserve + _barChromeWidth + 2;
	}

	private double GetBarWidth(FrameworkElement element, bool isWide, bool isDisplayed)
	{
		if (element is not AppBarSeparator && (!isWide || Array.IndexOf(_labelledCommands, element) < 0))
		{
			return CompactCommandWidth;
		}

		if (isDisplayed)
		{
			_barWidths[element] = element.ActualWidth;
		}

		if (_barWidths.TryGetValue(element, out var width))
		{
			return width;
		}

		return element switch
		{
			AppBarButton button => MeasureLabel(button.Label) + FallbackLabelPadding,
			AppBarToggleButton toggle => MeasureLabel(toggle.Label) + FallbackLabelPadding,
			_ => FallbackSeparatorWidth,
		};
	}

	private static double MeasureLabel(string? label) => MeasureWidth(new TextBlock { Text = label ?? string.Empty, FontSize = 14 });

	/// <param name="budget">The width left for the location once the commands have theirs.</param>
	/// <param name="title">The title's width, capped at <paramref name="titleMax"/>.</param>
	/// <param name="tag">The Manual tag's width, 0 when the sample has none.</param>
	/// <param name="breadcrumb">The breadcrumb's width, 0 when it does not apply.</param>
	/// <param name="minTitle">The width the title keeps even when the commands need it.</param>
	internal static (bool ShowBreadcrumb, bool ShowTag, double TitleMaxWidth) FitHeaderLocation(double budget, double title, double tag, double breadcrumb, double titleMax, double minTitle = MinTitleWidth)
	{
		var showTag = tag > 0 && Math.Min(title, minTitle) + tag <= budget;
		var tagWidth = showTag ? tag : 0;
		var showBreadcrumb = breadcrumb > 0 && breadcrumb + title + tagWidth <= budget;
		var titleMaxWidth = Math.Min(titleMax, Math.Max(minTitle, budget - tagWidth - (showBreadcrumb ? breadcrumb : 0)));

		return (showBreadcrumb, showTag, titleMaxWidth);
	}

	private static double MeasureWidth(UIElement element)
	{
		element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		return element.DesiredSize.Width;
	}

	// Without the rail the toggle leads to every destination, so it reads as the app menu.
	private void UpdateBrowserToggle()
	{
		var label = _isNarrow ? "Menu" : "Sample browser";
		ShellBrowserToggleIcon.Glyph = _isNarrow ? "\uE700" : ShellFunctions.BrowserToggleGlyph(_shellViewModel?.IsSplitVisible ?? false);
		AutomationProperties.SetName(ShellBrowserToggle, label);
		SetShortcutHint(ShellBrowserToggle, ShellCommands.ToggleBrowser, label);
	}

	// Shortcut hints mean nothing without a keyboard.
	private void UpdateInputHints()
	{
		var touch = ShellFunctions.IsTouchShell;
		ShowShortcutHints = !touch;
		var tablet = ShellLayoutStates.CurrentState?.Name == "TabletState";

		SearchBox.PlaceholderText = (touch, tablet) switch
		{
			(true, _) => "Search samples",
			(false, true) => "Search (Ctrl+F)",
			_ => "Search samples (Ctrl+F)",
		};
		ShellHomeHint.Text = touch ? "Search or open the menu to browse samples." : "Search with Ctrl+F or browse with Ctrl+B.";
		ShellHostEmptyHint.Text = touch ? "Search to find one" : "Press Ctrl+F to find one";
		ShellFavoritesEmptyHint.Text = touch ? "Star a sample to keep it here." : "Star a sample (Ctrl+Shift+D) to keep it here.";

		foreach (var (element, commandId) in _menuShortcuts)
		{
			SetMenuShortcut(element, commandId, showText: !touch);
		}
	}

	private ListView[] PaneLists => [ShellCategoriesList, ShellSamplesList, ShellFavoritesList, ShellRecentsList, ShellSearchResultsList];

	private double DesktopRowHeight => (double)Resources["ShellRowMinHeight"];

	private double PaneRowHeight => ShellFunctions.IsTouchShell ? TouchRowHeight : DesktopRowHeight;

	// The stock ListViewItem style only reads ListViewItemMinHeight when a container is styled, so containers are also stamped as they are (re)used.
	private void InitializeRowHeight()
	{
		ShellBrowserPane.Resources["ListViewItemMinHeight"] = PaneRowHeight;
		foreach (var list in PaneLists)
		{
			list.ContainerContentChanging += (sender, args) =>
			{
				args.ItemContainer.MinHeight = PaneRowHeight;

				// Search headings are labels, not rows: no hover, press, focus or menu.
				var isSample = args.Item is SampleChooserContent;
				var isRow = isSample || args.Item is SampleChooserCategory;
				args.ItemContainer.ContextFlyout = isSample ? SampleRowFlyout : null;
				args.ItemContainer.IsHitTestVisible = isRow;
				args.ItemContainer.IsTabStop = isRow;

				if (sender == ShellSearchResultsList)
				{
					PrepareSearchResultContainer(args);
				}
			};
		}
	}

	private void UpdateRowHeight()
	{
		// The pane's small controls grow to a finger-sized target too.
		var target = ShellFunctions.IsTouchShell ? TouchRowHeight : 0;
		foreach (var control in new Control[] { ShellManualTestsChip, ShellCloseSearchButton, ShellDescriptionMoreButton, ShellCollapseDescriptionButton })
		{
			control.MinWidth = target;
			control.MinHeight = target;
		}

		ShellDescriptionStrip.MinHeight = ShellFunctions.IsTouchShell ? TouchRowHeight : DesktopRowHeight;

		var height = PaneRowHeight;
		ShellBrowserPane.Resources["ListViewItemMinHeight"] = height;
		foreach (var list in PaneLists)
		{
			if (list.ItemsPanelRoot is { } panel)
			{
				foreach (var child in panel.Children)
				{
					if (child is ListViewItem item)
					{
						item.MinHeight = height;
					}
				}
			}
		}
	}

	// On a phone the overlay pane leaves a strip of the content column to tap away; it takes no room while the chrome is hidden.
	private void UpdatePaneLength()
		=> SplitView.OpenPaneLength = _shellViewModel is { IsShellChromeVisible: false }
			? 0
			: Math.Max(0, Math.Min((double)Resources["ShellPaneWidth"], ShellRoot.ActualWidth - ShellRailColumn.Width.Value - OverlayDismissGutter));

	private void ShellBreadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
	{
		if (_shellViewModel is not { } vm || vm.CurrentBreadcrumb is not { Count: > 0 } crumbs || crumbs[0] != "Library")
		{
			return;
		}

		if (args.Index == 0)
		{
			Run(vm.ShowLibraryCommand);
		}
		else if (crumbs.Count > 1)
		{
			vm.ShowCategoryCommand.Execute(crumbs[1]);
		}
	}

	private void ShellPaneBreadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
	{
		if (args.Index == 0)
		{
			_shellViewModel?.ShowBrowserSection(Section.Library);
		}
	}

	private void ShellBrowserTabs_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		if (_shellViewModel is not { } vm || sender.SelectedItem?.Tag is not string tag)
		{
			return;
		}

		// IsSelected follows the view model, so only a user pick that differs from it changes the section.
		var isCurrent = tag switch
		{
			"Recents" => vm.RecentsVisibility,
			"Favorites" => vm.FavoritesVisibility,
			_ => vm.CategoryVisibility || vm.SampleVisibility,
		};

		if (!isCurrent)
		{
			vm.ShowBrowserSection(tag switch
			{
				"Recents" => Section.Recents,
				"Favorites" => Section.Favorites,
				_ => Section.Library,
			});
		}
	}

	private void ShellSearchButton_Click(object sender, RoutedEventArgs e)
	{
		if (_shellViewModel is { } vm)
		{
			_ = FocusSearchAsync(vm);
		}
	}

	private void ShellDescriptionMoreButton_Click(object sender, RoutedEventArgs e) => ShowSampleInfo();

	private void ShellCollapseDescriptionButton_Click(object sender, RoutedEventArgs e)
	{
		if (_shellViewModel is { } vm)
		{
			vm.IsDescriptionCollapsed = true;
		}
	}

	private void ShellQuickSettingsFlyout_Opening(object? sender, object e)
	{
		ShellQuickDescriptionItem.IsChecked = _shellViewModel is { IsDescriptionCollapsed: false };
		ShellQuickDescriptionItem.IsEnabled = _shellViewModel is { } vm && ShellFunctions.HasDescription(vm.CurrentSelectedSample?.Description, vm.IsHomeVisible);
	}

	private void ShellQuickDescriptionItem_Click(object sender, RoutedEventArgs e)
	{
		if (_shellViewModel is { } vm)
		{
			vm.IsDescriptionCollapsed = !ShellQuickDescriptionItem.IsChecked;
		}
	}

	private void ShellCloseSettingsButton_Click(object sender, RoutedEventArgs e)
	{
		if (_shellViewModel is { } vm)
		{
			vm.BrowserView = BrowserView.Samples;
		}
	}

	private void ShellClearLink_Click(object sender, RoutedEventArgs e) => FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);

	private void ShellClearFavorites_Click(object sender, RoutedEventArgs e)
	{
		ShellClearFavoritesFlyout.Hide();
		if (_shellViewModel is { } vm)
		{
			Run(vm.ClearFavoritesCommand);
		}
	}

	private void ShellClearRecents_Click(object sender, RoutedEventArgs e)
	{
		ShellClearRecentsFlyout.Hide();
		if (_shellViewModel is { } vm)
		{
			Run(vm.ClearRecentsCommand);
		}
	}

	// The checked favorite keeps a neutral label and only its star takes the accent colour.
	private void UpdateFavoriteIcon()
	{
		var isFavorite = _shellViewModel?.IsFavoritedSample ?? false;

		if (isFavorite && ShellThemeBrushes.Get("AccentTextFillColorPrimaryBrush", ActualTheme) is { } accent)
		{
			ShellFavoriteIcon.Foreground = accent;
		}
		else
		{
			ShellFavoriteIcon.ClearValue(IconElement.ForegroundProperty);
		}

		SetShortcutHint(ShellFavoriteToggle, ShellCommands.ToggleFavorite, ShellFunctions.FavoriteLabel(isFavorite));
	}

	// Per-element accelerators are gone, so tooltips and AcceleratorKey come from the shortcut catalogue.
	private void ApplyShortcutHints()
	{
		SetShortcutHint(ShellBrowserToggle, ShellCommands.ToggleBrowser, "Sample browser");
		SetShortcutHint(ShellPaneHomeButton, ShellCommands.ShowHome, "Home");
		SetShortcutHint(ShellPaneRuntimeTestsButton, ShellCommands.OpenRuntimeTests, "Runtime tests");
		SetShortcutHint(ShellPanePlaygroundButton, ShellCommands.OpenPlayground, "Playground");
		SetShortcutHint(ShellPaneHelpButton, ShellCommands.OpenHelp, "Help");
		SetShortcutHint(ShellPaneSettingsButton, ShellCommands.ShowSettings, "Settings");
		ToolTipService.SetToolTip(ShellPaneBenchmarksButton, "Benchmarks");
		SetShortcutHint(ShellPreviousSampleButton, ShellCommands.PreviousSample, "Previous sample");
		SetShortcutHint(ShellNextSampleButton, ShellCommands.NextSample, "Next sample");
		SetShortcutHint(ShellReloadSampleButton, ShellCommands.ReloadSample, "Reload sample");
		SetShortcutHint(ShellFavoriteToggle, ShellCommands.ToggleFavorite, ShellFunctions.FavoriteLabel(false));
		SetShortcutHint(InfoButton, ShellCommands.ShowSampleInfo, "Sample info");
		SetShortcutHint(ShellSearchButton, ShellCommands.FocusSearch, "Search samples");
		SetShortcutHint(ShellRailHome, ShellCommands.ShowHome, "Home");
		SetShortcutHint(ShellRailSamples, ShellCommands.ToggleBrowser, "Samples");
		SetShortcutHint(ShellRailRuntimeTests, ShellCommands.OpenRuntimeTests, "Runtime tests");
		SetShortcutHint(ShellRailPlayground, ShellCommands.OpenPlayground, "Playground");
		SetShortcutHint(ShellRailHelp, ShellCommands.OpenHelp, "Help");
		SetShortcutHint(ShellRecentsTab, ShellCommands.ShowRecents, "Recent");
		SetShortcutHint(ShellFavoritesTab, ShellCommands.ShowFavorites, "Favorites");
		SetShortcutHint(ShellLibraryTab, ShellCommands.ShowLibrary, "Library");
		AutomationProperties.SetAcceleratorKey(SearchBox, ShellCommands.Describe(ShellCommands.FocusSearch));
		AutomationProperties.SetAcceleratorKey(ShellPaneSearchBox, ShellCommands.Describe(ShellCommands.FocusSearch));

		foreach (var (element, commandId) in _menuShortcuts)
		{
			SetMenuShortcut(element, commandId, showText: true);
		}
	}

	// A command in the overflow menu (or a hidden bar) has no on-screen spot of its own to anchor a flyout to.
	internal FrameworkElement GetCommandAnchor(AppBarButton button)
	{
		if (!button.IsInOverflow && button.Visibility == Visibility.Visible && button.ActualWidth > 0)
		{
			return button;
		}

		return ShellCommandBar.Visibility == Visibility.Visible && ShellCommandBar.ActualWidth > 0 ? ShellCommandBar : ShellHeader;
	}

	private static void SetShortcutHint(DependencyObject element, string commandId, string label)
	{
		var shortcut = ShellCommands.Describe(commandId);
		ToolTipService.SetToolTip(element, $"{label} ({shortcut})");
		AutomationProperties.SetAcceleratorKey(element, shortcut);
	}

	private static void SetMenuShortcut(DependencyObject element, string commandId, bool showText)
	{
		var shortcut = ShellCommands.Describe(commandId);
		AutomationProperties.SetAcceleratorKey(element, shortcut);

		var text = showText ? shortcut : string.Empty;
		switch (element)
		{
			case MenuFlyoutItem item:
				item.KeyboardAcceleratorTextOverride = text;
				break;
			case AppBarButton button:
				button.KeyboardAcceleratorTextOverride = text;
				break;
			case AppBarToggleButton toggle:
				toggle.KeyboardAcceleratorTextOverride = text;
				break;
		}
	}

	private static void Run(System.Windows.Input.ICommand command)
	{
		if (command.CanExecute(null))
		{
			command.Execute(null);
		}
	}
}
