// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.h

#nullable enable

using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

partial class PathIcon
{
	/// <summary>
	/// Gets or sets a Geometry that specifies the shape to be drawn. In XAML. this can also be set using a string that describes Move and draw commands syntax.
	/// </summary>
	public Geometry Data
	{
		get => (Geometry)GetValue(DataProperty);
		set => SetValue(DataProperty, value);
	}

	/// <summary>
	/// Identifies the Data dependency property.
	/// </summary>
	public static DependencyProperty DataProperty { get; } =
		DependencyProperty.Register(
			nameof(Data),
			typeof(Geometry),
			typeof(PathIcon),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: (s, e) => ((PathIcon)s).UpdateLinkedChildProperty(e.Property, Path.DataProperty)));
}
