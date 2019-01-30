// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewSelectionChangedEventArgs.cpp file from WinUI controls.
//

namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewSelectionChangedEventArgs 
	{
		public  bool IsSettingsSelected
		{
			get; internal set;
		}

		public  object SelectedItem
		{
			get; internal set;
		}

		public global::Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo RecommendedNavigationTransitionInfo
		{
			get; internal set;
		}

		public global::Windows.UI.Xaml.Controls.NavigationViewItemBase SelectedItemContainer
		{
			get; internal set;
		}
	}
}
