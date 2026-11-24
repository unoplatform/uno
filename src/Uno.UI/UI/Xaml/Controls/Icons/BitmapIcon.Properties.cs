// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

using System;
using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls;

partial class BitmapIcon
{
	/// <summary>
	/// Gets or sets a value that indicates whether the bitmap is shown in a single color.
	/// </summary>
	public bool ShowAsMonochrome
	{
		get => (bool)GetValue(ShowAsMonochromeProperty);
		set => SetValue(ShowAsMonochromeProperty, value);
	}

	/// <summary>
	/// Identifies the ShowAsMonochrome dependency property.
	/// </summary>
	public static DependencyProperty ShowAsMonochromeProperty { get; } =
		DependencyProperty.Register(
			nameof(ShowAsMonochrome), typeof(bool),
			typeof(BitmapIcon),
			new FrameworkPropertyMetadata(BoolBoxes.True, propertyChangedCallback: (s, e) => ((BitmapIcon)s).OnShowAsMonochromeChanged()));

	/// <summary>
	/// Gets or sets the Uniform Resource Identifier (URI) of the bitmap to use as the icon content.
	/// </summary>
	public Uri UriSource
	{
		get => (Uri)GetValue(UriSourceProperty);
		set => SetValue(UriSourceProperty, value);
	}

	/// <summary>
	/// Identifies the UriSource dependency property.
	/// </summary>
	public static DependencyProperty UriSourceProperty { get; } =
		DependencyProperty.Register(
			nameof(UriSource),
			typeof(Uri),
			typeof(BitmapIcon),
			new FrameworkPropertyMetadata(default(Uri), propertyChangedCallback: (s, e) => ((BitmapIcon)s).ReloadSourceImage()));
}
