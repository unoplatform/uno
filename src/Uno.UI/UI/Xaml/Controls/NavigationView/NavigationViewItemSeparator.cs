// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemSeparator.cpp, commit bac7a9c33

using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class NavigationViewItemSeparator : NavigationViewItemBase
{
	private bool m_appliedTemplate = false;
	private bool m_isClosedCompact = false;
	private Grid m_rootGrid = null;
	private readonly SerialDisposable m_splitViewIsPaneOpenChangedRevoker = new SerialDisposable();
	private readonly SerialDisposable m_splitViewDisplayModeChangedRevoker = new SerialDisposable();
	private const string c_rootGrid = "NavigationViewItemSeparatorRootGrid";

	public NavigationViewItemSeparator()
	{
		this.SetDefaultStyleKey();
	}

	private new void UpdateVisualState(bool useTransitions)
	{
		if (m_appliedTemplate)
		{
			var groupName = "NavigationSeparatorLineStates";
			var stateName = (Position != NavigationViewRepeaterPosition.TopPrimary && Position != NavigationViewRepeaterPosition.TopFooter)
			   ? m_isClosedCompact
				   ? "HorizontalLineCompact"
				   : "HorizontalLine"
			   : "VerticalLine";

			VisualStateUtil.GoToStateIfGroupExists(this, groupName, stateName, false /*useTransitions*/);
		}
	}

	protected override void OnApplyTemplate()
	{
#if !UNO_HAS_ENHANCED_LIFECYCLE
		// Native Android/iOS only: ElementPrepared fires after OnApplyTemplate there (no enhanced lifecycle),
		// so the parent NavigationView may not be set yet; postpone and reapply once it is.
		if (GetNavigationView() is null)
		{
			// Postpone template application for later
			return;
		}
#endif

		// Stop UpdateVisualState before template is applied. Otherwise the visual may not the same as we expect
		m_appliedTemplate = false;
		base.OnApplyTemplate();

		var rootGrid = GetTemplateChild(c_rootGrid) as Grid;
		if (rootGrid != null)
		{
			m_rootGrid = rootGrid;
		}

		var splitView = GetSplitView();
		if (splitView != null)
		{
			var isPaneOpenToken = splitView.RegisterPropertyChangedCallback(
				SplitView.IsPaneOpenProperty,
				OnSplitViewPropertyChanged);
			m_splitViewIsPaneOpenChangedRevoker.Disposable = Disposable.Create(() => splitView.UnregisterPropertyChangedCallback(SplitView.IsPaneOpenProperty, isPaneOpenToken));
			var displayModeToken = splitView.RegisterPropertyChangedCallback(
				SplitView.DisplayModeProperty,
				OnSplitViewPropertyChanged);
			m_splitViewDisplayModeChangedRevoker.Disposable = Disposable.Create(() => splitView.UnregisterPropertyChangedCallback(SplitView.DisplayModeProperty, displayModeToken));

			UpdateIsClosedCompact(false);
		}

		m_appliedTemplate = true;
		UpdateVisualState(false /*useTransition*/);
		UpdateItemIndentation();

#if !UNO_HAS_ENHANCED_LIFECYCLE
		_fullyInitialized = true;
#endif
	}

	protected override void OnNavigationViewItemBaseDepthChanged()
	{
		UpdateItemIndentation();
	}

	protected override void OnNavigationViewItemBasePositionChanged()
	{
		UpdateVisualState(false /*useTransition*/);
	}

	private void OnSplitViewPropertyChanged(DependencyObject sender, DependencyProperty args)
	{
		UpdateIsClosedCompact(true);
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

	private void UpdateIsClosedCompact(bool updateVisualState)
	{
		var splitView = GetSplitView();
		if (splitView != null)
		{
			// Check if the pane is closed and if the splitview is in either compact mode
			m_isClosedCompact = !splitView.IsPaneOpen
				&& (splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay || splitView.DisplayMode == SplitViewDisplayMode.CompactInline);

			if (updateVisualState)
			{
				UpdateVisualState(false /*useTransition*/);
			}
		}
	}
}
