// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewItemInvokedEventArgs.cpp file from WinUI controls.
//

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Media.Animation;
#else
using Windows.UI.Xaml.Media.Animation;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewItemInvokedEventArgs 
	{
		public object InvokedItem { get; internal set; }

		public  bool IsSettingsInvoked { get; internal set; }

		public NavigationViewItemBase InvokedItemContainer { get; internal set; }

		public NavigationTransitionInfo RecommendedNavigationTransitionInfo { get; internal set; }

		public NavigationViewItemInvokedEventArgs()
		{
		}
	}
}
