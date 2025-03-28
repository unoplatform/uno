// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using NonVirtualizingLayoutContext = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NonVirtualizingLayoutContext;
using NonVirtualizingLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NonVirtualizingLayout;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class NonVirtualStackLayout : NonVirtualizingLayout
	{
		protected internal override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
		{
			double extentHeight = 0.0;
			double extentWidth = 0.0;
			foreach (var element in context.Children)
			{
				element.Measure(availableSize);
				extentHeight += element.DesiredSize.Height;
				extentWidth = Math.Max(extentWidth, element.DesiredSize.Width);
			}

			return new Size(extentWidth, extentHeight);
		}

		protected internal override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
		{
			double offset = 0.0;
			foreach (var element in context.Children)
			{
				element.Arrange(new Rect(0, offset, element.DesiredSize.Width, element.DesiredSize.Height));
				offset += element.DesiredSize.Height;
			}

			return finalSize;
		}
	}
}
