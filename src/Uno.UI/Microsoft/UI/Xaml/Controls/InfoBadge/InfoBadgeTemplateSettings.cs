// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference InfoBadgeTemplateSettings.cpp & InfoBadgeTemplateSettings.properties.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBadgeTemplateSettings : DependencyObject
	{
		public IconElement IconElement
		{
			get => (IconElement)GetValue(IconElementProperty);
			set => SetValue(IconElementProperty, value);
		}

		public static DependencyProperty IconElementProperty { get; } =
			DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(InfoBadgeTemplateSettings), new FrameworkPropertyMetadata(null));

		public CornerRadius InfoBadgeCornerRadius
		{
			get => (CornerRadius)GetValue(InfoBadgeCornerRadiusProperty);
			set => SetValue(InfoBadgeCornerRadiusProperty, value);
		}

		public static DependencyProperty InfoBadgeCornerRadiusProperty { get; } =
			DependencyProperty.Register(nameof(InfoBadgeCornerRadius), typeof(CornerRadius), typeof(InfoBadgeTemplateSettings), new FrameworkPropertyMetadata(default(CornerRadius)));
	}
}
