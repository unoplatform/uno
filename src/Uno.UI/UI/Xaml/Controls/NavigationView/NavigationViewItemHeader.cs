// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemHeader.cpp, commit bac7a9c33

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

public partial class NavigationViewItemHeader : NavigationViewItemBase
{
	private bool m_isClosedCompact = false;

	private readonly SerialDisposable m_splitViewIsPaneOpenChangedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_splitViewDisplayModeChangedRevoker = new SerialDisposable();

	private Grid m_rootGrid = null;
	private const string c_rootGrid = "NavigationViewItemHeaderRootGrid";

	public NavigationViewItemHeader()
	{
		this.SetDefaultStyleKey();
	}

	protected override void OnApplyTemplate()
	{

		var splitView = GetSplitView();
		if (splitView != null)
		{
			// Uno specific: registered weakly, as WinUI's handler doesn't own the header, so the SplitView doesn't root it once removed.
			m_splitViewIsPaneOpenChangedRevoker.Disposable = splitView.RegisterDisposablePropertyChangedCallback(
				SplitView.IsPaneOpenProperty,
				(s, e) => OnSplitViewPropertyChanged(s, e.Property));
			m_splitViewDisplayModeChangedRevoker.Disposable = splitView.RegisterDisposablePropertyChangedCallback(
				SplitView.DisplayModeProperty,
				(s, e) => OnSplitViewPropertyChanged(s, e.Property));

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
