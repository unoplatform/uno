// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemHeader.cpp, commit f2df41d

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class NavigationViewItemHeader : NavigationViewItemBase
{
	private Grid m_rootGrid = null;
	private bool m_isClosedCompact = false;
	private const string c_rootGrid = "NavigationViewItemHeaderRootGrid";

	public NavigationViewItemHeader()
	{
		DefaultStyleKey = typeof(NavigationViewItemHeader);
	}

	protected override void OnApplyTemplate()
	{
		// TODO: Uno specific: NavigationView may not be set yet, wait for later #4689
		if (GetNavigationView() is null)
		{
			// Postpone template application for later
			return;
		}

		var splitView = GetSplitView();
		if (splitView != null)
		{
			//TODO: MZ: Probably should be unsubscribed
			splitView.RegisterPropertyChangedCallback(
				SplitView.IsPaneOpenProperty,
				OnSplitViewPropertyChanged);
			splitView.RegisterPropertyChangedCallback(
				SplitView.DisplayModeProperty,
				OnSplitViewPropertyChanged);

			UpdateIsClosedCompact();
		}

		var rootGrid = GetTemplateChild(c_rootGrid) as Grid;
		if (rootGrid != null)
		{
			m_rootGrid = rootGrid;
		}

		UpdateVisualState(false /*useTransitions*/);
		UpdateItemIndentation();

		var visual = ElementCompositionPreview.GetElementVisual(this);
		NavigationView.CreateAndAttachHeaderAnimation(visual);

		_fullyInitialized = true;
	}

	private void OnSplitViewPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		if (args == SplitView.IsPaneOpenProperty ||
			args == SplitView.DisplayModeProperty)
		{
			UpdateIsClosedCompact();
		}
	}

	private void UpdateIsClosedCompact()
	{
		var splitView = GetSplitView();
		if (splitView != null)
		{
			// Check if the pane is closed and if the splitview is in either compact mode.
			m_isClosedCompact = !splitView.IsPaneOpen && (splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay || splitView.DisplayMode == SplitViewDisplayMode.CompactInline);
			UpdateVisualState(true /*useTransitions*/);
		}
	}

	private new void UpdateVisualState(bool useTransitions)
	{
		VisualStateManager.GoToState(this, m_isClosedCompact && IsTopLevelItem ? "HeaderTextCollapsed" : "HeaderTextVisible", useTransitions);

		if (GetNavigationView() is { } navigationView)
		{
			VisualStateManager.GoToState(this, navigationView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? "TopMode" : "LeftMode", useTransitions);
		}
	}

	protected override void OnNavigationViewItemBaseDepthChanged()
	{
		UpdateItemIndentation();
	}

	private void UpdateItemIndentation()
	{
		// Update item indentation based on its depth
		var rootGrid = m_rootGrid;
		if (rootGrid != null)
		{
			var oldMargin = rootGrid.Margin;
			var newLeftMargin = Depth * c_itemIndentation;
			rootGrid.Margin = new Thickness(
				(double)(newLeftMargin),
				oldMargin.Top,
				oldMargin.Right,
				oldMargin.Bottom);
		}
	}
}
