// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressBar.properties.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

namespace Microsoft.UI.Xaml.Controls;

partial class ProgressBar
{
	/// <summary>
	/// Identifies the IsIndeterminate dependency property.
	/// </summary>
	public static DependencyProperty IsIndeterminateProperty { get; } = DependencyProperty.Register(
		nameof(IsIndeterminate),
		typeof(bool),
		typeof(ProgressBar),
		new FrameworkPropertyMetadata(default(bool), (s, e) => ((ProgressBar)s).OnIsIndeterminatePropertyChanged(e)));

	/// <summary>
	/// Gets or sets a value that indicates whether the progress bar reports generic progress
	/// with a repeating pattern or reports progress based on the Value property.
	/// </summary>
	public bool IsIndeterminate
	{
		get => (bool)GetValue(IsIndeterminateProperty);
		set => SetValue(IsIndeterminateProperty, value);
	}

	/// <summary>
	/// Identifies the ShowError dependency property.
	/// </summary>
	public static DependencyProperty ShowErrorProperty { get; } = DependencyProperty.Register(
		nameof(ShowError),
		typeof(bool),
		typeof(ProgressBar),
		new FrameworkPropertyMetadata(default(bool), (s, e) => ((ProgressBar)s).OnShowErrorPropertyChanged(e)));

	/// <summary>
	/// Gets or sets a value that indicates whether the progress bar should use visual states
	/// that communicate an Error state to the user.
	/// </summary>
	public bool ShowError
	{
		get => (bool)GetValue(ShowErrorProperty);
		set => SetValue(ShowErrorProperty, value);
	}

	/// <summary>
	/// Identifies the ShowPaused dependency property.
	/// </summary>
	public static DependencyProperty ShowPausedProperty { get; } = DependencyProperty.Register(
		nameof(ShowPaused),
		typeof(bool),
		typeof(ProgressBar),
		new FrameworkPropertyMetadata(default(bool), (s, e) => ((ProgressBar)s).OnShowPausedPropertyChanged(e)));

	/// <summary>
	/// Gets or sets a value that indicates whether the progress bar should use visual states
	/// that communicate a Paused state to the user.
	/// </summary>
	public bool ShowPaused
	{
		get => (bool)GetValue(ShowPausedProperty);
		set => SetValue(ShowPausedProperty, value);
	}

	/// <summary>
	/// Identifies the TemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TemplateSettingsProperty { get; } = DependencyProperty.Register(
		nameof(TemplateSettings),
		typeof(ProgressBarTemplateSettings),
		typeof(ProgressBar),
		new FrameworkPropertyMetadata(default(ProgressBarTemplateSettings)));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced as TemplateBinding
	/// sources when defining templates for a ProgressBar control.
	/// </summary>
	public ProgressBarTemplateSettings TemplateSettings
	{
		get => (ProgressBarTemplateSettings)GetValue(TemplateSettingsProperty);
		set => SetValue(TemplateSettingsProperty, value);
	}
}
