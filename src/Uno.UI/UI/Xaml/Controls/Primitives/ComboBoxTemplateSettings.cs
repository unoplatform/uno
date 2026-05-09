// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\winrtgeneratedclasses\ComboBoxTemplateSettings.g.cpp, commit 978ab6363

#nullable enable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class ComboBoxTemplateSettings : DependencyObject
{
	public double DropDownOpenedHeight
	{
		get => (double)GetValue(DropDownOpenedHeightProperty);
		internal set => SetValue(DropDownOpenedHeightProperty, value);
	}

	internal static DependencyProperty DropDownOpenedHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(DropDownOpenedHeight),
			typeof(double),
			typeof(ComboBoxTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	public double DropDownClosedHeight
	{
		get => (double)GetValue(DropDownClosedHeightProperty);
		internal set => SetValue(DropDownClosedHeightProperty, value);
	}

	internal static DependencyProperty DropDownClosedHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(DropDownClosedHeight),
			typeof(double),
			typeof(ComboBoxTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	public double DropDownOffset
	{
		get => (double)GetValue(DropDownOffsetProperty);
		internal set => SetValue(DropDownOffsetProperty, value);
	}

	internal static DependencyProperty DropDownOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(DropDownOffset),
			typeof(double),
			typeof(ComboBoxTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	public AnimationDirection SelectedItemDirection
	{
		get => (AnimationDirection)GetValue(SelectedItemDirectionProperty);
		internal set => SetValue(SelectedItemDirectionProperty, value);
	}

	internal static DependencyProperty SelectedItemDirectionProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedItemDirection),
			typeof(AnimationDirection),
			typeof(ComboBoxTemplateSettings),
			new FrameworkPropertyMetadata(AnimationDirection.Top));

	public double DropDownContentMinWidth
	{
		get => (double)GetValue(DropDownContentMinWidthProperty);
		internal set => SetValue(DropDownContentMinWidthProperty, value);
	}

	internal static DependencyProperty DropDownContentMinWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(DropDownContentMinWidth),
			typeof(double),
			typeof(ComboBoxTemplateSettings),
			new FrameworkPropertyMetadata(0.0));
}
