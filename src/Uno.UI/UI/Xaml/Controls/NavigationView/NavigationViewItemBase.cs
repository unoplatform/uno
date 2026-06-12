// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemBase.cpp, commit bac7a9c33 

using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

public partial class NavigationViewItemBase : ContentControl
{
#if HAS_UNO // Uno: replaces the WinUI attached-DP s_NavigationViewItemBaseRevokersProperty (stored on NavigationView) by holding the revokers directly on the item.
	internal CompositeDisposable EventRevokers { get; set; }
#endif

	public NavigationViewItemBase()
	{
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

#if !UNO_HAS_ENHANCED_LIFECYCLE
	// Native Android/iOS only: ElementPrepared fires after OnApplyTemplate there (no enhanced lifecycle),
	// so an item may be prepared before its template was applied; reapply it on demand.
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
