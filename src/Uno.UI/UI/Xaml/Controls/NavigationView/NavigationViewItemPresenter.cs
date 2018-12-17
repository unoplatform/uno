// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewItemPresenter.cpp file from WinUI controls.
//

using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public  partial class NavigationViewItemPresenter : global::Windows.UI.Xaml.Controls.ContentControl
	{
		NavigationViewItemHelper<NavigationViewItemPresenter> m_helper = new NavigationViewItemHelper<NavigationViewItemPresenter>();

		public global::Windows.UI.Xaml.Controls.IconElement Icon
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.IconElement)this.GetValue(IconProperty);
			}
			set
			{
				this.SetValue(IconProperty, value);
			}
		}

		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } = 
			Windows.UI.Xaml.DependencyProperty.Register(
				"Icon",
				typeof(global::Windows.UI.Xaml.Controls.IconElement), 
				typeof(global::Windows.UI.Xaml.Controls.Primitives.NavigationViewItemPresenter), 
				new FrameworkPropertyMetadata(
					default(global::Windows.UI.Xaml.Controls.IconElement)
				)
			);

		public NavigationViewItemPresenter() : base()
		{
			DefaultStyleKey = typeof(NavigationViewItemPresenter);
		}
		
		protected override void OnApplyTemplate()
		{
			// Retrieve pointers to stable controls 
			m_helper.Init(this);
			var navigationViewItem = GetNavigationViewItem();
			if (navigationViewItem != null)
			{
				navigationViewItem.UpdateVisualStateNoTransition();
			}
		}

		internal UIElement GetSelectionIndicator()
		{
			return m_helper.GetSelectionIndicator();  
		}

		protected override bool GoToElementStateCore(string state, bool useTransitions)
		{
			// GoToElementStateCore: Update visualstate for itself.
			// VisualStateManager::GoToState: update visualstate for it's first child.

			// If NavigationViewItemPresenter is used, two sets of VisualStateGroups are supported. One set is help to switch the style and it's NavigationViewItemPresenter itself and defined in NavigationViewItem
			// Another set is defined in style for NavigationViewItemPresenter.
			// OnLeftNavigation, OnTopNavigationPrimary, OnTopNavigationOverflow only apply to itself.
			if (state == NavigationViewItemHelper.c_OnLeftNavigation || state == NavigationViewItemHelper.c_OnLeftNavigationReveal || state == NavigationViewItemHelper.c_OnTopNavigationPrimary
				|| state == NavigationViewItemHelper.c_OnTopNavigationPrimaryReveal || state == NavigationViewItemHelper.c_OnTopNavigationOverflow)
			{
				return base.GoToElementStateCore(state, useTransitions);
			}
			return VisualStateManager.GoToState(this, state, useTransitions);
		}

		NavigationViewItem GetNavigationViewItem()
		{
			NavigationViewItem navigationViewItem = null;

			var item = SharedHelpers.GetAncestorOfType<NavigationViewItem>(VisualTreeHelper.GetParent(this));
			if (item != null)
			{
				navigationViewItem = item;
			}
			return navigationViewItem;
		}
	}
}
