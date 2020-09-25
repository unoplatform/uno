// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

//
// This file is a C# translation of the NavigationViewItem.cpp file from WinUI controls.
//

using System;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

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

		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectsOnInvokedProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(SelectsOnInvoked), typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.NavigationViewItem),
			new FrameworkPropertyMetadata(true));

		public static global::Windows.UI.Xaml.DependencyProperty CompactPaneLengthProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CompactPaneLength), typeof(double),
			typeof(NavigationViewItem),
			new FrameworkPropertyMetadata(defaultValue: 48.0)
		);

		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			name: nameof(Icon),
			propertyType: typeof(IconElement),
			ownerType: typeof(NavigationViewItem),
			typeMetadata: new FrameworkPropertyMetadata(defaultValue: null)
		);
	}
}
