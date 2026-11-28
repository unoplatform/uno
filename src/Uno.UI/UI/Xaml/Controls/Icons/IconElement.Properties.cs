// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class IconElement
{
	/// <summary>
	/// Gets or sets a brush that describes the foreground color.
	/// </summary>
	public Brush Foreground
	{
		get => (Brush)GetValue(ForegroundProperty);
		set => SetValue(ForegroundProperty, value);
	}

	/// <summary>
	/// Identifies the Foreground dependency property.
	/// </summary>
	public static DependencyProperty ForegroundProperty { get; } =
		DependencyProperty.Register(
			nameof(Foreground),
			typeof(Brush),
			typeof(IconElement),
			new FrameworkPropertyMetadata(
				// TODO Uno specific: WinUI defaults to DefaultTextForegroundThemeBrush through text formatting,
				// a static DP default can't follow the theme. Inherited values resolve the same once in the tree.
				SolidColorBrushHelper.White,
				FrameworkPropertyMetadataOptions.Inherits,
				propertyChangedCallback: (s, e) => ((IconElement)s).OnForegroundChanged(e)));
}
