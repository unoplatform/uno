// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBadgeTemplateSettings.properties.cpp, commit 76bd573

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class InfoBadgeTemplateSettings : DependencyObject
	{
		public InfoBadgeTemplateSettings()
		{
		}

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
