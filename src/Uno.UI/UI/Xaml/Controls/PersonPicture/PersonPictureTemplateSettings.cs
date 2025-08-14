// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PersonPictureTemplateSettings.properties.cpp, tag winui3/release/1.4.2

using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

[Microsoft.UI.Xaml.Data.Bindable]

public partial class PersonPictureTemplateSettings : DependencyObject
{
	public ImageBrush ActualImageBrush
	{
		get => (ImageBrush)GetValue(ActualImageBrushProperty);
		internal set => SetValue(ActualImageBrushProperty, value);
	}

	public static DependencyProperty ActualImageBrushProperty { get; } =
		DependencyProperty.Register(nameof(ActualImageBrush), typeof(ImageBrush), typeof(PersonPictureTemplateSettings), new FrameworkPropertyMetadata(null));

	public string ActualInitials
	{
		get => (string)GetValue(ActualInitialsProperty);
		internal set => SetValue(ActualInitialsProperty, value);
	}

	[DynamicDependency(nameof(ActualInitials))]
	public static readonly DependencyProperty ActualInitialsProperty =
		DependencyProperty.Register(nameof(ActualInitials), typeof(string), typeof(PersonPictureTemplateSettings), new FrameworkPropertyMetadata(""));
}
