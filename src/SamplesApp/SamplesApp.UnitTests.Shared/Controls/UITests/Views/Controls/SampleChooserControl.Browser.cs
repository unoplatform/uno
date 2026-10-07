#nullable enable

using System;
using System.Collections;
using System.Linq;
using System.Windows.Input;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using SampleControl.Entities;
using SampleControl.Presentation;
using Uno.UI.Samples.Helper;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Samples.Controls;

partial class SampleChooserControl
{
	private IconElement?[] _browserTabIcons = [];
	private MenuFlyout? _sampleRowFlyout;
	private SampleChooserContent? _flyoutSample;

	private void InitializeBrowserPane()
		=> _browserTabIcons = [ShellRecentsTab.Icon, ShellFavoritesTab.Icon, ShellLibraryTab.Icon];

	private int[] _searchRowPositions = [];
	private int _searchSampleCount;

	private void UpdateSearchResults()
	{
		var rows = ShellFunctions.FlattenSearchResults(_shellViewModel?.SearchResultsGrouped);

		// Screen readers count samples only, so headings do not shift "item X of N".
		_searchRowPositions = new int[rows.Count];
		_searchSampleCount = 0;
		for (var i = 0; i < rows.Count; i++)
		{
			_searchRowPositions[i] = rows[i] is SampleChooserContent ? ++_searchSampleCount : 0;
		}

		ShellSearchResultsList.ItemsSource = rows;
		DispatcherQueue.TryEnqueue(SyncSearchResultSelection);
	}

	private void PrepareSearchResultContainer(ContainerContentChangingEventArgs args)
	{
		var container = args.ItemContainer;
		var position = args.ItemIndex >= 0 && args.ItemIndex < _searchRowPositions.Length ? _searchRowPositions[args.ItemIndex] : 0;
		if (position > 0)
		{
			container.ClearValue(AutomationProperties.AccessibilityViewProperty);
			AutomationProperties.SetPositionInSet(container, position);
			AutomationProperties.SetSizeOfSet(container, _searchSampleCount);
		}
		else
		{
			// The heading text inside keeps its HeadingLevel; only the row wrapper leaves the tree.
			AutomationProperties.SetAccessibilityView(container, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
			container.ClearValue(AutomationProperties.PositionInSetProperty);
			container.ClearValue(AutomationProperties.SizeOfSetProperty);
		}
	}

	// The results only mark the shown sample; opening one goes through ItemClick.
	private void SyncSearchResultSelection()
	{
		var current = _shellViewModel is { IsHomeVisible: false } vm ? vm.CurrentSelectedSample : null;
		var item = current is not null && ShellSearchResultsList.ItemsSource is IList rows && rows.Contains(current) ? current : null;
		if (!Equals(ShellSearchResultsList.SelectedItem, item))
		{
			ShellSearchResultsList.SelectedItem = item;
		}
	}

	private void ShellSearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ShellSearchResultsList.SelectedItem is SearchResultsHeader)
		{
			ShellSearchResultsList.SelectedItem = e.RemovedItems.FirstOrDefault();
		}
	}

	private void ShellSearchResultsList_ItemClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is SampleChooserContent sample && _shellViewModel is { } vm)
		{
			vm.SelectedSearchSample = sample;
		}
	}

	// Both boxes edit the same term; a term cleared from code (close button, Esc) clears them too.
	private void SyncSearchText()
	{
		var term = _shellViewModel?.SearchTerm ?? string.Empty;
		foreach (var box in new[] { SearchBox, ShellPaneSearchBox })
		{
			if (box.Text != term)
			{
				box.Text = term;
			}
		}
	}

	// A phone-width pane cannot fit three labelled tabs with icons.
	private void UpdateBrowserTabs()
	{
		SelectorBarItem[] tabs = [ShellRecentsTab, ShellFavoritesTab, ShellLibraryTab];
		for (var i = 0; i < tabs.Length && i < _browserTabIcons.Length; i++)
		{
			tabs[i].Icon = _isNarrow ? null : _browserTabIcons[i];
		}
	}

	// A subtle checked fill alone barely shows, so the active filter also lights its icon.
	private void UpdateManualTestsChip()
	{
		if (_shellViewModel is { ManualTestsOnly: true } && ShellThemeBrushes.Get("AccentTextFillColorPrimaryBrush", ActualTheme) is { } accent)
		{
			ShellManualTestsChipIcon.Foreground = accent;
		}
		else
		{
			ShellManualTestsChipIcon.ClearValue(IconElement.ForegroundProperty);
		}
	}

	// A revealed row can sit below the fold, e.g. on a phone in landscape. Waits for the list to take its new items.
	private void BringLibraryRowIntoView()
	{
		if (_shellViewModel is { IsSplitVisible: true, SelectedLibrarySample: { } sample })
		{
			DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (Equals(ShellSamplesList.SelectedItem, sample))
				{
					ShellSamplesList.ScrollIntoView(sample);
				}
			});
		}
	}

	private static bool IsControlDown
		=> InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

	private static bool IsEnterDown
		=> InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Enter).HasFlag(CoreVirtualKeyStates.Down);

	private void CloseOverlayBrowser(SampleChooserViewModel vm)
	{
		if (SplitView.DisplayMode is SplitViewDisplayMode.Overlay or SplitViewDisplayMode.CompactOverlay)
		{
			vm.IsSplitVisible = false;
		}
	}

	// The box handles Esc itself while its suggestions are open.
	private void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key != VirtualKey.Escape || sender is not AutoSuggestBox box || _shellViewModel is not { } vm)
		{
			return;
		}

		e.Handled = true;

		if (!string.IsNullOrEmpty(box.Text))
		{
			box.Text = string.Empty;
			vm.SearchTerm = string.Empty;
		}
		else if (FocusManager.FindFirstFocusableElement(ShellHostLayer) is Control host)
		{
			host.Focus(FocusState.Keyboard);
		}
	}

	private void ShellCloseSearchButton_Click(object sender, RoutedEventArgs e)
	{
		if (_shellViewModel is { } vm)
		{
			// An empty term also leaves the results (view model).
			vm.SearchTerm = string.Empty;
			vm.CloseSearchResults();
		}
	}

	private MenuFlyout SampleRowFlyout => _sampleRowFlyout ??= CreateSampleRowFlyout();

	private MenuFlyout CreateSampleRowFlyout()
	{
		MenuFlyout flyout = new();

		var open = Item("ShellRowOpenItem", "Open", "", vm => vm.OpenSampleCommand);
		var openInNewWindow = Item("ShellRowOpenInNewWindowItem", "Open in new window", "", vm => vm.OpenSampleInNewWindowCommand);
		var favorite = Item("ShellRowFavoriteItem", ShellFunctions.FavoriteLabel(false), "", vm => vm.ToggleFavoriteCommand);
		var copyLink = Item("ShellRowCopyLinkItem", "Copy link", "", vm => vm.CopySampleLinkCommand);
		var copyTypeName = Item("ShellRowCopyTypeNameItem", "Copy type name", "", vm => vm.CopyTypeNameCommand);
		var viewSource = Item("ShellRowViewSourceItem", "View source on GitHub", "", vm => vm.OpenSourceOnGitHubCommand);

		flyout.Items.Add(open);
		flyout.Items.Add(openInNewWindow);
		flyout.Items.Add(new MenuFlyoutSeparator());
		flyout.Items.Add(favorite);
		flyout.Items.Add(copyLink);
		flyout.Items.Add(copyTypeName);
		flyout.Items.Add(viewSource);

		flyout.Opening += (_, _) =>
		{
			_flyoutSample = flyout.Target switch
			{
				SelectorItem { Content: SampleChooserContent sample } => sample,
				FrameworkElement { DataContext: SampleChooserContent sample } => sample,
				_ => null,
			};

			var isFavorite = _flyoutSample?.IsFavorite ?? false;
			favorite.Text = ShellFunctions.FavoriteLabel(isFavorite);
			((FontIcon)favorite.Icon).Glyph = ShellFunctions.FavoriteGlyph(isFavorite);
			openInNewWindow.Visibility = ShellFunctions.Visible(SampleChooserViewModel.CanCreateNewWindow);
			viewSource.IsEnabled = !string.IsNullOrEmpty(_flyoutSample?.GitHubSourceUrl);
		};

		return flyout;

		MenuFlyoutItem Item(string automationId, string text, string glyph, Func<SampleChooserViewModel, ICommand> command)
		{
			MenuFlyoutItem item = new()
			{
				Text = text,
				Icon = new FontIcon { Glyph = glyph },
			};
			if (Application.Current.Resources.TryGetValue("SymbolThemeFontFamily", out var font) && font is Microsoft.UI.Xaml.Media.FontFamily fontFamily)
			{
				item.Icon.SetValue(FontIcon.FontFamilyProperty, fontFamily);
			}

			AutomationProperties.SetAutomationId(item, automationId);
			item.Click += (_, _) =>
			{
				if (_shellViewModel is { } vm && _flyoutSample is { } sample && command(vm) is { } cmd && cmd.CanExecute(sample))
				{
					cmd.Execute(sample);
				}
			};
			return item;
		}
	}
}
