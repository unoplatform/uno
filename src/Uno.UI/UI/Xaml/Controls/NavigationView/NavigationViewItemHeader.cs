// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewHeader.cpp file from WinUI controls.
//

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
#else
using Windows.UI.Xaml.Hosting;
#endif

namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewItemHeader : NavigationViewItemBase
	{
		private long m_splitViewIsPaneOpenChangedRevoker;
		private long m_splitViewDisplayModeChangedRevoker;
		private bool m_isClosedCompact;

		public NavigationViewItemHeader() 
		{
			DefaultStyleKey = typeof(NavigationViewItemHeader);

			Loaded += NavigationViewItemHeader_Loaded;
		}

		private void NavigationViewItemHeader_Loaded(object sender, RoutedEventArgs e)
		{
			var splitView = GetSplitView();
			if (splitView != null)
			{
				m_splitViewIsPaneOpenChangedRevoker = splitView.RegisterPropertyChangedCallback(
					SplitView.IsPaneOpenProperty,
					OnSplitViewPropertyChanged
				);
				m_splitViewDisplayModeChangedRevoker = splitView.RegisterPropertyChangedCallback(
					SplitView.DisplayModeProperty,
					OnSplitViewPropertyChanged
				);

				UpdateIsClosedCompact();
			}

			UpdateLocalVisualState(false /*useTransitions*/);
		}

		protected override void OnApplyTemplate()
		{

			var visual = ElementCompositionPreview.GetElementVisual(this);
			NavigationView.CreateAndAttachHeaderAnimation(visual);
		}

		void OnSplitViewPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			if (args == SplitView.IsPaneOpenProperty ||
				args == SplitView.DisplayModeProperty)
			{
				UpdateIsClosedCompact();
			}
		}

		void UpdateIsClosedCompact()
		{
			var splitView = GetSplitView();
			if (splitView != null)
			{
				// Check if the pane is closed and if the splitview is in either compact mode.
				m_isClosedCompact = !splitView.IsPaneOpen && (splitView.DisplayMode == SplitViewDisplayMode.CompactOverlay || splitView.DisplayMode == SplitViewDisplayMode.CompactInline);
				UpdateLocalVisualState(true /*useTransitions*/);
			}
		}

		void UpdateLocalVisualState(bool useTransitions)
		{
			VisualStateManager.GoToState(this, m_isClosedCompact ? "HeaderTextCollapsed" : "HeaderTextVisible", useTransitions);
		}
	}
}
