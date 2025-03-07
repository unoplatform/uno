// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: TabViewItemTemplateSettings.properties.cpp, commit 8aaf7f8

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as {TemplateBinding}
	/// markup extension sources when defining templates for a TabViewItem control.
	/// </summary>
	public partial class TabViewItemTemplateSettings : DependencyObject
	{
		/// <summary>
		/// Gets an object that provides calculated values that can be referenced as {TemplateBinding}
		/// markup extension sources when defining templates for a TabViewItem control.
		/// </summary>
		public IconElement IconElement
		{
			get => (IconElement)GetValue(IconElementProperty);
			set => SetValue(IconElementProperty, value);
		}

		/// <summary>
		/// Identifies the IconElement dependency property.
		/// </summary>
		public static DependencyProperty IconElementProperty { get; } =
			DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(TabViewItemTemplateSettings), new FrameworkPropertyMetadata(null));
	}
}
