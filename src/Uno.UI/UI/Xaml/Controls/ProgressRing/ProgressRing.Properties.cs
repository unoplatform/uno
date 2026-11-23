// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressRing.properties.cpp

#nullable enable

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls;

partial class ProgressRing
{
	public IAnimatedVisualSource? DeterminateSource
	{
		get => (IAnimatedVisualSource?)GetValue(DeterminateSourceProperty);
		set => SetValue(DeterminateSourceProperty, value);
	}

	public static DependencyProperty DeterminateSourceProperty { get; } =
		DependencyProperty.Register(nameof(DeterminateSource), typeof(IAnimatedVisualSource), typeof(ProgressRing), new FrameworkPropertyMetadata(null, (s, e) => ((ProgressRing)s).OnDeterminateSourcePropertyChanged(e)));

	public IAnimatedVisualSource? IndeterminateSource
	{
		get => (IAnimatedVisualSource?)GetValue(IndeterminateSourceProperty);
		set => SetValue(IndeterminateSourceProperty, value);
	}

	public static DependencyProperty IndeterminateSourceProperty { get; } =
		DependencyProperty.Register(nameof(IndeterminateSource), typeof(IAnimatedVisualSource), typeof(ProgressRing), new FrameworkPropertyMetadata(null, (s, e) => ((ProgressRing)s).OnIndeterminateSourcePropertyChanged(e)));

	// Hides the internal DependencyObject.IsActive (live-tree state).
	public new bool IsActive
	{
		get => (bool)GetValue(IsActiveProperty);
		set => SetValue(IsActiveProperty, value);
	}

	public static DependencyProperty IsActiveProperty { get; } =
		DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(ProgressRing), new FrameworkPropertyMetadata(BoolBoxes.True, (s, e) => ((ProgressRing)s).OnIsActivePropertyChanged(e)));

	public bool IsIndeterminate
	{
		get => (bool)GetValue(IsIndeterminateProperty);
		set => SetValue(IsIndeterminateProperty, value);
	}

	public static DependencyProperty IsIndeterminateProperty { get; } =
		DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(ProgressRing), new FrameworkPropertyMetadata(BoolBoxes.True, (s, e) => ((ProgressRing)s).OnIsIndeterminatePropertyChanged(e)));

	public double Maximum
	{
		get => (double)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, Boxer.Box(value));
	}

	public static DependencyProperty MaximumProperty { get; } =
		DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ProgressRing), new FrameworkPropertyMetadata(Boxer.Box(100.0), (s, e) => ((ProgressRing)s).OnMaximumPropertyChanged(e)));

	public double Minimum
	{
		get => (double)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, Boxer.Box(value));
	}

	public static DependencyProperty MinimumProperty { get; } =
		DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(ProgressRing), new FrameworkPropertyMetadata(DoubleBoxes.Zero, (s, e) => ((ProgressRing)s).OnMinimumPropertyChanged(e)));

	public ProgressRingTemplateSettings TemplateSettings
	{
		get => (ProgressRingTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	internal static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(ProgressRingTemplateSettings), typeof(ProgressRing), new FrameworkPropertyMetadata(null));

	public double Value
	{
		get => (double)GetValue(ValueProperty);
		set => SetValue(ValueProperty, Boxer.Box(value));
	}

	public static DependencyProperty ValueProperty { get; } =
		DependencyProperty.Register(nameof(Value), typeof(double), typeof(ProgressRing), new FrameworkPropertyMetadata(DoubleBoxes.Zero, (s, e) => ((ProgressRing)s).OnValuePropertyChanged(e)));
}
