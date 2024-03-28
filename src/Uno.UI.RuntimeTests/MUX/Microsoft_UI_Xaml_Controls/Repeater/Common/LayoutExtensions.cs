// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.UI.Xaml.Controls;

using VirtualizingLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.VirtualizingLayout;
using StackLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.StackLayout;
//using UniformGridLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.UniformGridLayout;
using FlowLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.FlowLayout;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public static class LayoutExtension
	{
		public static void SetOrientation(this VirtualizingLayout layout, ScrollOrientation scrollOrientation)
		{
			// Note:
			// The public properties of UniformGridLayout and FlowLayout interpret orientation the opposite to
			// how FlowLayoutAlgorithm interprets it. 
			// For simplicity, all of our test validation code is written in terms that match the implementation.
			// For this reason, we need to switch the orientation whenever we set UniformGridLayout.Orientation 
			// or StackLayout.Orientation.
			if (layout is StackLayout)
			{
				((StackLayout)layout).Orientation = scrollOrientation.ToLayoutOrientation();
			}
#if false
			else if (layout is UniformGridLayout)
			{
				((UniformGridLayout)layout).Orientation = scrollOrientation.ToOrthogonalLayoutOrientation();
			}
#endif
			else if (layout is FlowLayout)
			{
				((FlowLayout)layout).Orientation = scrollOrientation.ToOrthogonalLayoutOrientation();
			}
			else
			{
				throw new InvalidOperationException("layout unknown");
			}
		}
	}
}
