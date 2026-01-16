// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference PathIconSource_Partial.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class PathIconSource : IconSource
{
	public Geometry Data
	{
		get => (Geometry)GetValue(DataProperty);
		set => SetValue(DataProperty, value);
	}

	public static DependencyProperty DataProperty { get; } =
		DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(PathIconSource), new FrameworkPropertyMetadata(null, OnPropertyChanged));

	protected override IconElement CreateIconElementCore()
	{
		Geometry data = Data;

		var pathIcon = new PathIcon();

		pathIcon.Data = Data;

		return pathIcon;
	}

	protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		if (iconSourceProperty == DataProperty)
		{
			return PathIcon.DataProperty;
		}
		else
		{
			return base.GetIconElementPropertyCore(iconSourceProperty);
		}
	}
}
