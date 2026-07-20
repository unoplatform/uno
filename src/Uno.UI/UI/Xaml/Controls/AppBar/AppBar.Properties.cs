// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar
{
	/// <summary>
	/// Gets or sets a value that indicates whether icon buttons are displayed when the app bar is not completely open.
	/// </summary>
	public AppBarClosedDisplayMode ClosedDisplayMode
	{
		get => (AppBarClosedDisplayMode)GetValue(ClosedDisplayModeProperty);
		set => SetValue(ClosedDisplayModeProperty, value);
	}

	/// <summary>
	/// Identifies the ClosedDisplayMode dependency property.
	/// </summary>
	public static DependencyProperty ClosedDisplayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(ClosedDisplayMode),
			typeof(AppBarClosedDisplayMode),
			typeof(AppBar),
			new FrameworkPropertyMetadata(AppBarClosedDisplayMode.Compact));

	/// <summary>
	/// Gets or sets a value that indicates whether the AppBar is open.
	/// </summary>
	public bool IsOpen
	{
		get => (bool)GetValue(IsOpenProperty);
		set => SetValue(IsOpenProperty, value);
	}

	/// <summary>
	/// Identifies the IsOpen dependency property.
	/// </summary>
	public static DependencyProperty IsOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsOpen),
			typeof(bool),
			typeof(AppBar),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Gets or sets a value that indicates whether the AppBar does not close on light dismiss.
	/// </summary>
	public bool IsSticky
	{
		get => (bool)GetValue(IsStickyProperty);
		set => SetValue(IsStickyProperty, value);
	}

	/// <summary>
	/// Identifies the IsSticky dependency property.
	/// </summary>
	public static DependencyProperty IsStickyProperty { get; } =
		DependencyProperty.Register(
			nameof(IsSticky),
			typeof(bool),
			typeof(AppBar),
			new FrameworkPropertyMetadata(default(bool)));

	/// <summary>
	/// Gets or sets a value that specifies whether the area outside of a light-dismiss UI is darkened.
	/// </summary>
	public LightDismissOverlayMode LightDismissOverlayMode
	{
		get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
		set => SetValue(LightDismissOverlayModeProperty, value);
	}

	/// <summary>
	/// Identifies the LightDismissOverlayMode dependency property.
	/// </summary>
	public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LightDismissOverlayMode),
			typeof(LightDismissOverlayMode),
			typeof(AppBar),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

	/// <summary>
	/// Gets an object that provides calculated values that can be referenced
	/// as {TemplateBinding} markup extension sources when defining templates for an AppBar control.
	/// </summary>
	public AppBarTemplateSettings TemplateSettings
	{
		get => (AppBarTemplateSettings)GetValue(TemplateSettingsProperty);
		private set => SetValue(TemplateSettingsProperty, value);
	}

	private static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(
			nameof(TemplateSettings),
			typeof(AppBarTemplateSettings),
			typeof(AppBar),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Occurs when the AppBar changes from visible to hidden.
	/// </summary>
	public event EventHandler<object>? Closed;

	/// <summary>
	/// Occurs when the AppBar starts to change from visible to hidden.
	/// </summary>
	public event EventHandler<object>? Closing;

	/// <summary>
	/// Occurs when the AppBar changes from hidden to visible.
	/// </summary>
	public event EventHandler<object>? Opened;

	/// <summary>
	/// Occurs when the AppBar starts to change from hidden to visible.
	/// </summary>
	public event EventHandler<object>? Opening;
}
