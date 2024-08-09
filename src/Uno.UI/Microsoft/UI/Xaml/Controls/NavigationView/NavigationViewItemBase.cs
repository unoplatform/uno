// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemBase.cpp, commit 574e5ed 

using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class NavigationViewItemBase : ContentControl
{
	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		// TODO Uno specific - unsubscribe Loaded event handler to avoid multiple subscriptions
		// as OnApplyTemplate will be called repeatedly.
		Loaded -= OnLoaded;
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		// If the NavViewItem is not prepared in an ItemPresenter it will be missing a reference to NavigationView, so adding that here.
		if (m_navigationView == null)
		{
			SetNavigationViewParent(SharedHelpers.GetAncestorOfType<NavigationView>(this));
		}
	}

	internal NavigationViewRepeaterPosition Position
	{
		get => m_position;
		set
		{
			if (m_position != value)
			{
				m_position = value;
				OnNavigationViewItemBasePositionChanged();
			}
		}
	}

	internal NavigationView GetNavigationView()
	{
		return m_navigationView;
	}

	// TODO: MZ Uno specific: existing Depth property inherited from base class
	internal
#if __NETSTD_REFERENCE__ || __SKIA__ || __WASM__
		new
#endif
		int Depth
	{
		get => m_depth;
		set
		{
			if (m_depth != value)
			{
				m_depth = value;
				OnNavigationViewItemBaseDepthChanged();
			}
		}
	}

	protected SplitView GetSplitView()
	{
		SplitView splitView = null;
		var navigationView = GetNavigationView();
		if (navigationView != null)
		{
			splitView = navigationView.GetSplitView();
		}
		return splitView;
	}

	internal void SetNavigationViewParent(NavigationView navigationView)
	{
		m_navigationView = navigationView;
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (args.Property == IsSelectedProperty)
		{
			OnNavigationViewItemBaseIsSelectedChanged();
		}
	}

#if IS_UNO
	// TODO: Uno specific: Remove when #4689 is fixed

	protected bool _fullyInitialized = false;

	internal void Reinitialize()
	{
		if (!_fullyInitialized)
		{
			OnApplyTemplate();
		}
		UpdateVisualState(false);
	}
#endif
}
