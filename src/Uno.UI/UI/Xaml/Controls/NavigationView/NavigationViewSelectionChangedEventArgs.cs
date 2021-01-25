// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewSelectionChangedEventArgs.cpp file from WinUI controls.
//

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
#else
using Windows.UI.Xaml.Media.Animation;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewSelectionChangedEventArgs
	{
		public bool IsSettingsSelected
		{
			get; internal set;
		}

		public object SelectedItem
		{
			get; internal set;
		}

		public NavigationTransitionInfo RecommendedNavigationTransitionInfo
		{
			get; internal set;
		}

		public NavigationViewItemBase SelectedItemContainer
		{
			get; internal set;
		}
	}
}
