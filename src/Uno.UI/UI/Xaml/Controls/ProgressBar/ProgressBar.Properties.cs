// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ProgressBar.properties.cpp, tag winui3/release/1.7-stable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Controls;

public partial class ProgressBar
{
	public static DependencyProperty IsIndeterminateProperty { get; } = DependencyProperty.Register(
		nameof(IsIndeterminate), typeof(bool), typeof(ProgressBar), new FrameworkPropertyMetadata(Boxes.BooleanBoxes.BoxedFalse, OnIsIndeterminateChanged));

	public bool IsIndeterminate
	{
		get => (bool)GetValue(IsIndeterminateProperty);
		set => SetValue(IsIndeterminateProperty, value);
	}

	public static DependencyProperty ShowErrorProperty { get; } = DependencyProperty.Register(
		nameof(ShowError), typeof(bool), typeof(ProgressBar), new FrameworkPropertyMetadata(Boxes.BooleanBoxes.BoxedFalse, OnShowErrorChanged));

	public bool ShowError
	{
		get => (bool)GetValue(ShowErrorProperty);
		set => SetValue(ShowErrorProperty, value);
	}

	public static DependencyProperty ShowPausedProperty { get; } = DependencyProperty.Register(
		nameof(ShowPaused), typeof(bool), typeof(ProgressBar), new FrameworkPropertyMetadata(Boxes.BooleanBoxes.BoxedFalse, OnShowPausedChanged));

	public bool ShowPaused
	{
		get => (bool)GetValue(ShowPausedProperty);
		set => SetValue(ShowPausedProperty, value);
	}

	public static DependencyProperty TemplateSettingsProperty { get; } = DependencyProperty.Register(
		nameof(TemplateSettings), typeof(ProgressBarTemplateSettings), typeof(ProgressBar), new FrameworkPropertyMetadata(default(ProgressBarTemplateSettings)));

	public ProgressBarTemplateSettings TemplateSettings
	{
		get => (ProgressBarTemplateSettings)GetValue(TemplateSettingsProperty);
		set => SetValue(TemplateSettingsProperty, value);
	}
}
