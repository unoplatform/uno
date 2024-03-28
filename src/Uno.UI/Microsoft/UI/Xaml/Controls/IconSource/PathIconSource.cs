// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference PathIconSource_Partial.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class PathIconSource : IconSource
{
	public Geometry Data
	{
		get => (Geometry)GetValue(DataProperty);
		set => SetValue(DataProperty, value);
	}

	public static DependencyProperty DataProperty { get; } =
		DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(PathIconSource), new FrameworkPropertyMetadata(null, OnPropertyChanged));

#if !HAS_UNO_WINUI
	private
#endif
	protected override IconElement CreateIconElementCore()
	{
		Geometry data = Data;

		var pathIcon = new PathIcon();

		pathIcon.Data = Data;

		return pathIcon;
	}

#if !HAS_UNO_WINUI
	private
#endif
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
