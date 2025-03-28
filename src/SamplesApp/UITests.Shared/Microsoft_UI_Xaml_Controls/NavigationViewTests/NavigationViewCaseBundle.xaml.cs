// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls;

using NavigationViewDisplayMode = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationView;
using NavigationViewSelectionChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs;
using NavigationViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemSeparator = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewItemSeparator;
using NavigationViewDisplayModeChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp
{
	//[Sample("NavigationView", "MUX")]
	public sealed partial class NavigationViewCaseBundle : TestPage
	{
		public NavigationViewCaseBundle()
		{
			this.InitializeComponent();
			NavigationViewPage.Click += delegate { Frame.Navigate(typeof(NavigationViewPage), 0); };
			NavigationViewCompactPaneLengthTestPage.Click += delegate { Frame.Navigate(typeof(NavigationViewCompactPaneLengthTestPage), 0); };
			NavigationViewRS4Page.Click += delegate { Frame.Navigate(typeof(NavigationViewRS4Page), 0); };
			NavigationViewPageDataContext.Click += delegate { Frame.Navigate(typeof(NavigationViewPageDataContext), 0); };
			NavigationViewTopNavPage.Click += delegate { Frame.Navigate(typeof(NavigationViewTopNavPage), 0); };
			NavigationViewTopNavOnlyPage.Click += delegate { Frame.Navigate(typeof(NavigationViewTopNavOnlyPage), 0); };
			NavigationViewTopNavStorePage.Click += delegate { Frame.Navigate(typeof(NavigationViewTopNavStorePage), 0); };
			NavigateToTopNavOverflowButtonPage.Click += delegate { Frame.Navigate(typeof(NavigationViewTopNavOverflowButtonPage), 0); };
			NavigateToSelectedItemEdgeCasePage.Click += delegate { Frame.Navigate(typeof(NavigationViewSelectedItemEdgeCasePage), 0); };
			NavigateToInitPage.Click += delegate { Frame.Navigate(typeof(NavigationViewInitPage), 0); };
			NavigateToStretchPage.Click += delegate { Frame.Navigate(typeof(NavigationViewStretchPage), 0); };
			NavigateToItemTemplatePage.Click += delegate { Frame.Navigate(typeof(NavigationViewItemTemplatePage), 0); };
			NavigateToRS3Page.Click += delegate { Frame.Navigate(typeof(NavigationViewRS3Page), 0); };
			NavigateToAnimationPage.Click += delegate { Frame.Navigate(typeof(NavigationViewAnimationPage), 0); };
			NavigateToIsPaneOpenPage.Click += delegate { Frame.Navigate(typeof(NavigationViewIsPaneOpenPage), 0); };
			NavigateToMinimalPage.Click += delegate { Frame.Navigate(typeof(NavigationViewMinimalPage), 0); };
			NavigateToCustomThemeResourcesPage.Click += delegate { Frame.Navigate(typeof(NavigationViewCustomThemeResourcesPage), 0); };
			NavigationViewBlankPage1.Click += delegate { Frame.Navigate(typeof(NavigationViewBlankPage1), 0); };
			NavigationViewMenuItemStretchPageButton.Click += delegate { Frame.Navigate(typeof(NavigationViewMenuItemStretchPage), 0); };
			NavigateToHierarchicalNavigationViewMarkupPage.Click += delegate { Frame.Navigate(typeof(HierarchicalNavigationViewMarkup), 0); };
			NavigateToHierarchicalNavigationViewDataBindingPage.Click += delegate { Frame.Navigate(typeof(HierarchicalNavigationViewDataBinding), 0); };
			PaneLayoutTestPageButton.Click += delegate { Frame.Navigate(typeof(PaneLayoutTestPage), 0); };
		}
	}
}
