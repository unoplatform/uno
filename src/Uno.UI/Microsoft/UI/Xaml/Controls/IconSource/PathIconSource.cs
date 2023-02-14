// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PathIconSource.cpp, commit 083796a

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PathIconSource : IconSource
	{
		public Geometry Data
		{
			get => (Geometry)GetValue(DataProperty);
			set => SetValue(DataProperty, value);
		}

		public static DependencyProperty DataProperty { get; } =
			DependencyProperty.Register(nameof(Data), typeof(Geometry), typeof(PathIconSource), new FrameworkPropertyMetadata(null, OnPropertyChanged));

		private protected override IconElement CreateIconElementCore()
		{
			var pathIcon = new PathIcon();

			if (Data != null)
			{
				pathIcon.Data = Data;
			}

			if (Foreground != null)
			{
				pathIcon.Foreground = Foreground;
			}

			return pathIcon;
		}

		private protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty sourceProperty)
		{
			if (sourceProperty == DataProperty)
			{
				return PathIcon.DataProperty;
			}

			return base.GetIconElementPropertyCore(sourceProperty);
		}
	}
}
