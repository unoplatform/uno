// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using MUXControlsTestApp.Utilities;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using NavigationView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView;
using NavigationViewItemInvokedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using NavigationViewPaneDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewPaneDisplayMode;

namespace MUXControlsTestApp
{
	[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewAnimationPage : TestPage
	{
		public NavigationViewAnimationPage()
		{
			this.InitializeComponent();
		}
		private void NavView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
			NavigateToPage(args.InvokedItemContainer.Tag);
		}

		private void NavigateToPage(object pageTag)
		{
			if (pageTag == null)
			{
				pageTag = "BlankPage1";
			}
			var pageName = "MUXControlsTestApp.NavigationView" + pageTag;
			var pageType = Type.GetType(pageName);

			ContentFrame.Navigate(pageType);
		}

		private void FlipOrientation_Click(object sender, RoutedEventArgs e)
		{
			NavView.PaneDisplayMode = NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Top ? NavigationViewPaneDisplayMode.Auto : NavigationViewPaneDisplayMode.Top;
		}
	}
}
