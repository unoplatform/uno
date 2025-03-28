// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

[Sample("Scrolling")]
public sealed partial class ScrollViewWithRTLFlowDirectionPage : TestPage
{
	public ScrollViewWithRTLFlowDirectionPage()
	{
		this.InitializeComponent();
	}

	private void ChkScrollViewFlowDirection_Checked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null && wuxScrollViewer != null)
		{
			muxScrollView.FlowDirection = FlowDirection.RightToLeft;
			wuxScrollViewer.FlowDirection = FlowDirection.RightToLeft;
		}
	}

	private void ChkScrollViewFlowDirection_Unchecked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null && wuxScrollViewer != null)
		{
			muxScrollView.FlowDirection = FlowDirection.LeftToRight;
			wuxScrollViewer.FlowDirection = FlowDirection.LeftToRight;
		}
	}

	private void ChkScrollViewContentFlowDirection_Checked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null && wuxScrollViewer != null)
		{
			FrameworkElement content = muxScrollView.Content as FrameworkElement;

			if (content != null)
			{
				content.FlowDirection = FlowDirection.RightToLeft;
			}

			content = wuxScrollViewer.Content as FrameworkElement;

			if (content != null)
			{
				content.FlowDirection = FlowDirection.RightToLeft;
			}
		}
	}

	private void ChkScrollViewContentFlowDirection_Unchecked(object sender, RoutedEventArgs e)
	{
		if (muxScrollView != null && wuxScrollViewer != null)
		{
			FrameworkElement content = muxScrollView.Content as FrameworkElement;

			if (content != null)
			{
				content.FlowDirection = FlowDirection.LeftToRight;
			}

			content = wuxScrollViewer.Content as FrameworkElement;

			if (content != null)
			{
				content.FlowDirection = FlowDirection.LeftToRight;
			}
		}
	}
}
