// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewItem.cpp file from WinUI controls.
//

#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NavigationViewItem : NavigationViewItemBase
	{
		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public double CompactPaneLength
			=> (double)GetValue(CompactPaneLengthProperty);

		public bool SelectsOnInvoked
		{
			get => (bool)GetValue(SelectsOnInvokedProperty);
			set => SetValue(SelectsOnInvokedProperty, value);
		}

		[Uno.NotImplemented]
		public static DependencyProperty SelectsOnInvokedProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectsOnInvoked), typeof(bool),
			typeof(NavigationViewItem),
			new FrameworkPropertyMetadata(true));

		public static DependencyProperty CompactPaneLengthProperty { get; } =
		DependencyProperty.Register(
			nameof(CompactPaneLength), typeof(double),
			typeof(NavigationViewItem),
			new FrameworkPropertyMetadata(defaultValue: 48.0)
		);

		public static DependencyProperty IconProperty { get; } =
		DependencyProperty.Register(
			name: nameof(Icon),
			propertyType: typeof(IconElement),
			ownerType: typeof(NavigationViewItem),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: null)
		);
	}
}
