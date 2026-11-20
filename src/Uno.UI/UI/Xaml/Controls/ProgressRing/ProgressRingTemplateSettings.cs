// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressRingTemplateSettings.cpp and ProgressRingTemplateSettings.properties.cpp

#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls;

public partial class ProgressRingTemplateSettings : DependencyObject
{
	internal ProgressRingTemplateSettings()
	{
	}

	public double EllipseDiameter
	{
		get => (double)GetValue(EllipseDiameterProperty);
		internal set => SetValue(EllipseDiameterProperty, Boxer.Box(value));
	}

	internal static DependencyProperty EllipseDiameterProperty { get; } =
		DependencyProperty.Register(nameof(EllipseDiameter), typeof(double), typeof(ProgressRingTemplateSettings), new FrameworkPropertyMetadata(DoubleBoxes.Zero));

	public Thickness EllipseOffset
	{
		get => (Thickness)GetValue(EllipseOffsetProperty);
		internal set => SetValue(EllipseOffsetProperty, value);
	}

	internal static DependencyProperty EllipseOffsetProperty { get; } =
		DependencyProperty.Register(nameof(EllipseOffset), typeof(Thickness), typeof(ProgressRingTemplateSettings), new FrameworkPropertyMetadata(default(Thickness)));

	public double MaxSideLength
	{
		get => (double)GetValue(MaxSideLengthProperty);
		internal set => SetValue(MaxSideLengthProperty, Boxer.Box(value));
	}

	internal static DependencyProperty MaxSideLengthProperty { get; } =
		DependencyProperty.Register(nameof(MaxSideLength), typeof(double), typeof(ProgressRingTemplateSettings), new FrameworkPropertyMetadata(DoubleBoxes.Zero));
}
