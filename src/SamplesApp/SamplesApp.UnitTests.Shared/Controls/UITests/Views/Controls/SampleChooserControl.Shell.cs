#nullable enable

using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SampleControl.Presentation;
using Uno.UI.Samples.Entities;
using Uno.UI.Samples.Helper;

namespace Uno.UI.Samples.Controls;

partial class SampleChooserControl
{
	private const double RailWidth = 48;
	private const double OverlayDismissGutter = 48;

	private const double TouchRowHeight = 40;

	private SampleChooserViewModel? _shellViewModel;
	private bool _syncingRail;

	private void InitializeShell()
	{
		ApplyShortcutHints();
		InitializeRowHeight();

		ShellOpenInNewWindowButton.Visibility = ShellFunctions.Visible(SampleChooserViewModel.CanCreateNewWindow);
		ShellLogViewDumpButton.Visibility = ShellFunctions.Visible(SampleChooserViewModel.IsDebug);

		DataContextChanged += OnShellDataContextChanged;
		Loaded += OnShellLoaded;
		ActualThemeChanged += (_, _) => UpdateFavoriteIcon();
	}

	private void OnShellLoaded(object sender, RoutedEventArgs e)
	{
		if (ShellRail.SettingsItem is NavigationViewItem settingsItem)
		{
			AutomationProperties.SetAutomationId(settingsItem, "ShellRailSettings");
			SetShortcutHint(settingsItem, ShellCommands.ShowSettings, "Settings");
		}

		UpdateChromeState(useTransitions: false);
		SyncRailSelection();
		UpdateFavoriteIcon();
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
		UpdateRowHeight();
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
				break;

#if HAS_UNO
			case nameof(SampleChooserViewModel.SimulateTouch):
				UpdateRowHeight();
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

		ShellRailColumn.Width = new GridLength(state == "ChromeNormalState" ? RailWidth : 0);

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
		ShellSampleCommandsSeparator.Visibility = visibility;
		ShellFavoriteToggle.Visibility = visibility;
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
	}

	// Items act as commands, so invoking the selected item again (e.g. Runtime tests) re-runs it.
	private void ShellRail_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (_shellViewModel is not { } vm)
		{
			return;
		}

		if (args.IsSettingsInvoked)
		{
			Run(vm.ShowSettingsCommand);
			return;
		}

		switch ((args.InvokedItemContainer as FrameworkElement)?.Tag as string)
		{
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

	private ListView[] PaneLists => [ShellCategoriesList, ShellSamplesList, ShellFavoritesList, ShellRecentsList];

	private double DesktopRowHeight => (double)Resources["ShellRowMinHeight"];

	private double PaneRowHeight => ShellFunctions.IsTouchShell ? TouchRowHeight : DesktopRowHeight;

	// The stock ListViewItem style only reads ListViewItemMinHeight when a container is styled, so containers are also stamped as they are (re)used.
	private void InitializeRowHeight()
	{
		ShellBrowserPane.Resources["ListViewItemMinHeight"] = PaneRowHeight;
		foreach (var list in PaneLists)
		{
			list.ContainerContentChanging += (_, args) => args.ItemContainer.MinHeight = PaneRowHeight;
		}
	}

	private void UpdateRowHeight()
	{
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

		SetMenuShortcut(ShellCopyLinkButton, ShellCommands.CopySampleLink);
		SetMenuShortcut(ShellFocusModeButton, ShellCommands.ToggleFocusMode);
		SetMenuShortcut(ShellQuickHelpItem, ShellCommands.OpenHelp);
	}

	private static void SetShortcutHint(DependencyObject element, string commandId, string label)
	{
		var shortcut = ShellCommands.Describe(commandId);
		ToolTipService.SetToolTip(element, $"{label} ({shortcut})");
		AutomationProperties.SetAcceleratorKey(element, shortcut);
	}

	private static void SetMenuShortcut(DependencyObject element, string commandId)
	{
		var shortcut = ShellCommands.Describe(commandId);
		AutomationProperties.SetAcceleratorKey(element, shortcut);

		switch (element)
		{
			case MenuFlyoutItem item:
				item.KeyboardAcceleratorTextOverride = shortcut;
				break;
			case AppBarButton button:
				button.KeyboardAcceleratorTextOverride = shortcut;
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
