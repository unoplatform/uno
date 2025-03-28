// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using MUXControlsTestApp.Utilities;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
using System.Threading;
using System;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.Foundation;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using StackLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.StackLayout;
//using LayoutPanel = Microsoft/* UWP don't rename */.UI.Xaml.Controls.LayoutPanel;
//using UniformGridLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.UniformGridLayout;
using NonVirtualizingLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NonVirtualizingLayout;
using NonVirtualizingLayoutContext = Microsoft/* UWP don't rename */.UI.Xaml.Controls.NonVirtualizingLayoutContext;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests
{
#if false
	[TestClass]
	public partial class LayoutPanelTests : MUXApiTestBase
	{
		[ClassInitialize]
		[TestProperty("Classification", "Integration")]
		public static void ClassInitialize(TestContext context) { }

		[TestMethod]
		public void VerifyPaddingAndBorderThicknessLayoutOffset()
		{
			RunOnUIThread.Execute(() =>
			{
				double width = 400;
				double height = 400;
				Thickness borderThickness = new Thickness(5, 10, 15, 20);
				Thickness padding = new Thickness(2, 4, 6, 8);

				LayoutPanel panel = new LayoutPanel();
				panel.Width = width;
				panel.Height = height;
				panel.BorderBrush = new SolidColorBrush(Colors.Red);
				panel.BorderThickness = borderThickness;
				panel.Padding = padding;

				var button = new Button { Content = "Button", VerticalAlignment = VerticalAlignment.Stretch, HorizontalAlignment = HorizontalAlignment.Stretch };
				var expectedButtonLayoutSlot = new Rect
				{
					Width = width - borderThickness.Left - borderThickness.Right - padding.Left - padding.Right,
					Height = height - borderThickness.Top - borderThickness.Bottom - padding.Top - padding.Bottom,
					X = borderThickness.Left + padding.Left,
					Y = borderThickness.Top + padding.Top,
				};
				panel.Children.Add(button);

				Content = panel;
				Content.UpdateLayout();

				Verify.AreEqual(expectedButtonLayoutSlot, LayoutInformation.GetLayoutSlot(button), "Verify LayoutSlot of child Button");
			});
		}

		[TestMethod]
		public void VerifyPaddingAndBorderThicknessLayoutOffset_StackLayout()
		{
			RunOnUIThread.Execute(() =>
			{
				double width = 400;
				double height = 400;
				Thickness borderThickness = new Thickness(5, 10, 15, 20);
				Thickness padding = new Thickness(2, 4, 6, 8);

				LayoutPanel panel = new LayoutPanel();
				panel.Layout = new StackLayout();
				panel.Width = width;
				panel.Height = height;
				panel.BorderBrush = new SolidColorBrush(Colors.Red);
				panel.BorderThickness = borderThickness;
				panel.Padding = padding;

				double unpaddedWidth = width - borderThickness.Left - borderThickness.Right - padding.Left - padding.Right;
				double itemHeight = 50;
				double unpaddedX = borderThickness.Left + padding.Left;
				double unpaddedY = borderThickness.Top + padding.Top;

				var button1 = new Button { Content = "Button", Height = itemHeight, HorizontalAlignment = HorizontalAlignment.Stretch };
				var button2 = new Button { Content = "Button", Height = itemHeight, HorizontalAlignment = HorizontalAlignment.Stretch };
				var expectedButton1LayoutSlot = new Rect
				{
					Width = unpaddedWidth,
					Height = itemHeight,
					X = unpaddedX,
					Y = unpaddedY,
				};
				var expectedButton2LayoutSlot = new Rect
				{
					Width = unpaddedWidth,
					Height = itemHeight,
					X = unpaddedX,
					Y = unpaddedY + itemHeight,
				};
				panel.Children.Add(button1);
				panel.Children.Add(button2);

				Content = panel;
				Content.UpdateLayout();

				Verify.AreEqual(expectedButton1LayoutSlot, LayoutInformation.GetLayoutSlot(button1), "Verify LayoutSlot of child 1");
				Verify.AreEqual(expectedButton2LayoutSlot, LayoutInformation.GetLayoutSlot(button2), "Verify LayoutSlot of child 2");
			});
		}

		[TestMethod]
		public void VerifySwitchingLayoutDynamically()
		{
			LayoutPanel panel = null;
			Button button1 = null;
			Button button2 = null;

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Create LayoutPanel with StackLayout");

				panel = new LayoutPanel() { Width = 400, Height = 400 };

				var stackLayout = new StackLayout
				{
					Orientation = Orientation.Vertical
				};
				panel.Layout = stackLayout;

				button1 = new Button { Height = 100, Content = "1" };
				button2 = new Button { Height = 100, Content = "2" };
				panel.Children.Add(button1);
				panel.Children.Add(button2);

				Content = panel;
				Content.UpdateLayout();

				Log.Comment("Verify layout for StackLayout:");
				Verify.AreEqual(new Rect(0, 0, 400, 100), LayoutInformation.GetLayoutSlot(button1), "Verify LayoutSlot of child 1");
				Verify.AreEqual(new Rect(0, 100, 400, 100), LayoutInformation.GetLayoutSlot(button2), "Verify LayoutSlot of child 2");
			});

			IdleSynchronizer.Wait();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Switch LayoutPanel to UniformGridLayout");
				UniformGridLayout gridLayout = new UniformGridLayout();
				gridLayout.MinItemWidth = 100;
				gridLayout.MinItemHeight = 100;
				panel.Layout = gridLayout;

				Content.UpdateLayout();

				Log.Comment("Verify layout for UniformGridLayout:");
				Verify.AreEqual(new Rect(0, 0, 100, 100), LayoutInformation.GetLayoutSlot(button1), "Verify LayoutSlot of child 1");
				Verify.AreEqual(new Rect(100, 0, 100, 100), LayoutInformation.GetLayoutSlot(button2), "Verify LayoutSlot of child 2");
			});
		}

		[TestMethod]
		public void VerifyCustomNonVirtualizingLayout()
		{
			LayoutPanel panel = null;
			Button button1 = null;
			Button button2 = null;

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Create LayoutPanel with MyCustomNonVirtualizingStackLayout");

				panel = new LayoutPanel() { Width = 400, Height = 400 };

				var customStackLayout = new MyCustomNonVirtualizingStackLayout();
				panel.Layout = customStackLayout;

				button1 = new Button { Height = 100, Width = 400, Content = "1" };
				button2 = new Button { Height = 100, Width = 400, Content = "2" };
				panel.Children.Add(button1);
				panel.Children.Add(button2);

				Content = panel;
				Content.UpdateLayout();

				Log.Comment("Verify layout for StackLayout:");
				Verify.AreEqual(new Rect(0, 0, 400, 100), LayoutInformation.GetLayoutSlot(button1), "Verify LayoutSlot of child 1");
				Verify.AreEqual(new Rect(0, 100, 400, 100), LayoutInformation.GetLayoutSlot(button2), "Verify LayoutSlot of child 2");
			});

			IdleSynchronizer.Wait();
		}
	}
#endif

	public class MyCustomNonVirtualizingStackLayout : NonVirtualizingLayout
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
