#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Private.Infrastructure;
using MUXControlsTestApp.Utilities;
#if __IOS__
using UIKit;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_FrameworkElement
	{
#if __WASM__
		// TODO Android does not handle measure invalidation properly
		[TestMethod]
		public Task When_Measure_Once() =>
			RunOnUIThread.Execute(() =>
			{
				var SUT = new MyControl01();

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[0]);

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
			});
#endif

		[TestMethod]
		public Task When_Measure_And_Invalidate() =>
			RunOnUIThread.Execute(() =>
			{
				var SUT = new MyControl01();

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[0]);

				SUT.InvalidateMeasure();

				SUT.Measure(new Size(10, 10));
				Assert.AreEqual(2, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[1]);
			});

		[TestMethod]
		public Task MeasureWithNan() =>
			RunOnUIThread.Execute(() =>
			{
				var SUT = new MyControl01();

				SUT.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);

				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(double.NaN, double.NaN)));
				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(42.0, double.NaN)));
				Assert.ThrowsException<InvalidOperationException>(() => SUT.Measure(new Size(double.NaN, 42.0)));
			});

		[TestMethod]
		public Task MeasureOverrideWithNan() =>
			RunOnUIThread.Execute(() =>
			{

				var SUT = new MyControl01();

				SUT.BaseAvailableSize = new Size(double.NaN, double.NaN);
				SUT.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);
			});

		[TestMethod]
#if __WASM__
		[Ignore] // Failing on WASM - https://github.com/unoplatform/uno/issues/2314
#endif
		public Task MeasureOverride_With_Nan_In_Grid() =>
			RunOnUIThread.Execute(() =>
			{
				var grid = new Grid();

				var SUT = new MyControl02();
				SUT.Content = new Grid();
				grid.Children.Add(SUT);

				SUT.BaseAvailableSize = new Size(double.NaN, double.NaN);
				grid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				Assert.AreEqual(new Size(double.PositiveInfinity, double.PositiveInfinity), SUT.MeasureOverrides.Last());
				Assert.AreEqual(new Size(0, 0), SUT.DesiredSize);
			});

#if __WASM__
		// TODO Android does not handle measure invalidation properly
		[TestMethod]
		public Task When_Grid_Measure_And_Invalidate() =>
			RunOnUIThread.Execute(() =>
			{
				var grid = new Grid();
				var SUT = new MyControl01();

				grid.Children.Add(SUT);

				grid.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
				Assert.AreEqual(new Size(10, 10), SUT.MeasureOverrides[0]);

				grid.InvalidateMeasure();

				grid.Measure(new Size(10, 10));
				Assert.AreEqual(1, SUT.MeasureOverrides.Count);
			});
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_MinWidth_SmallerThan_AvailableSize()
		{
			Border content = null;
			ContentControl contentCtl = null;
			Grid grid = null;

			content = new Border { Width = 100, Height = 15 };

			contentCtl = new ContentControl { MinWidth = 110, Content = content };

			grid = new Grid() { MinWidth = 120 };

			grid.Children.Add(contentCtl);

			grid.Measure(new Size(50, 50));

			Assert.AreEqual(new Size(50, 15), grid.DesiredSize);
#if NETFX_CORE // Failing on WASM - https://github.com/unoplatform/uno/issues/2314
			Assert.AreEqual(new Size(110, 15), contentCtl.DesiredSize);
			Assert.AreEqual(new Size(100, 15), content.DesiredSize);
#endif

			grid.Arrange(new Rect(default, new Size(50, 50)));

			TestServices.WindowHelper.WindowContent = new Border { Child = grid, Width = 50, Height = 50 };

			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(10);
			await TestServices.WindowHelper.WaitForIdle();

#if NETFX_CORE // Failing on WASM - https://github.com/unoplatform/uno/issues/2314
			var ls1 = LayoutInformation.GetLayoutSlot(grid);
			Assert.AreEqual(new Rect(0, 0, 50, 50), ls1);
			var ls2 = LayoutInformation.GetLayoutSlot(contentCtl);
			Assert.AreEqual(new Rect(0, 0, 120, 50), ls2);
			var ls3 = LayoutInformation.GetLayoutSlot(content);
			Assert.AreEqual(new Rect(0, 0, 100, 15), ls3);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		public void Check_ActualWidth_After_Measure()
		{
			var SUT = new Border { Width = 75, Height = 32 };
			var size = new Size(1000, 1000);
			SUT.Measure(size);
			Assert.AreEqual(75, SUT.DesiredSize.Width);
			Assert.AreEqual(32, SUT.DesiredSize.Height);

			Assert.AreEqual(0, SUT.ActualWidth);
			Assert.AreEqual(0, SUT.ActualHeight);
		}

		// Center: No width & height
		[DataRow("Center", "Center", null, null, null, null, null, null, "88;29;24;42|92;37;16;26|100;50;0;0")]
		// Stretch: No width & height
		[DataRow("Stretch", "Stretch", null, null, null, null, null, null, "0;0;200;100|4;8;192;84|12;21;176;58")]
		// Left/Top: No width & height
		[DataRow("Left", "Top", null, null, null, null, null, null, "0;0;24;42|4;8;16;26|12;21;0;0")]
		// Right/Bottom: No width & height
		[DataRow("Right", "Bottom", null, null, null, null, null, null, "176;58;24;42|180;66;16;26|188;79;0;0")]
		// Center: Only sizes (width & height) defined
		[DataRow("Center", "Center", null, null, 100d, 50d, null, null, "46;17;108;66|50;25;100;50|58;38;84;24")]
		// Stretch: Only sizes (width & height) defined
		[DataRow("Stretch", "Stretch", null, null, 100d, 50d, null, null, "0;0;200;100|50;25;100;50|58;38;84;24")]
		// Right/Top: Only sizes (width & height) defined
		[DataRow("Right", "Top", null, null, 100d, 50d, null, null, "92;0;108;66|96;8;100;50|104;21;84;24")]
		// Left/Bottom: Only sizes (width & height) defined
		[DataRow("Left", "Bottom", null, null, 100d, 50d, null, null, "0;34;108;66|4;42;100;50|12;55;84;24")]
#if NETFX_CORE // Those tests only works on UWP, not Uno yet
		// Center: Only sizes (width & height) defined, but no breath space for margin
		[DataRow("Center", "Center", null, null, 200d, 100d, null, null, "0;0;200;100|4;8;200;100|12;21;184;74")]
		// Center: Only sizes(width & height) defined, but larger than available size
		[DataRow("Center", "Center", null, null, 300d, 200d, null, null, "0;0;200;100|4;8;300;200|12;21;284;174")]
#endif
		// Center: Only min values("min width" & "min height")
		[DataRow("Center", "Center", 100d, 50d, null, null, null, null, "46;17;108;66|50;25;100;50|58;38;84;24")]
		// Center: Only max values("max width" & "max height")
		[DataRow("Center", "Center", null, null, null, null, 100d, 50d, "88;29;24;42|92;37;16;26|100;50;0;0")]
		// Center: Both sizes & max values, sizes < max
		[DataRow("Center", "Center", 100d, 50d, 10d, 5d, null, null, "46;17;108;66|50;25;100;50|58;38;84;24")]
		// Center: Both sizes & max values, sizes > max
		[DataRow("Center", "Center", 25d, 5d, 100d, 50d, null, null, "46;17;108;66|50;25;100;50|58;38;84;24")]
		[TestMethod]
		[RunsOnUIThread]
		public async Task TestVariousArrangedPosition(
			string horizontal,
			string vertical,
			double? minWidth,
			double? minHeight,
			double? width,
			double? height,
			double? maxWidth,
			double? maxHeight,
			string expectedResult)
		{
			// Arrange
			var innerChild = new Border
			{
				Name = "inner",
				Background = new SolidColorBrush(Colors.DarkRed),
			};

			var childBorder = new Border
			{
				Name = "child",
				Background = new SolidColorBrush(Colors.Blue),
				Child = innerChild,
				Margin = new Thickness(4d, 8d, 4d, 8d),
				BorderThickness = new Thickness(3d, 6d, 3d, 6d),
				BorderBrush = new SolidColorBrush(Colors.DeepSkyBlue),
				Padding = new Thickness(5d, 7d, 5d, 7d),
			};

			var childDecorator = new Border
			{
				Name = "decorator",
				Background = new SolidColorBrush(Colors.Green),
				Child = childBorder,
				HorizontalAlignment = (HorizontalAlignment)Enum.Parse(typeof(HorizontalAlignment), horizontal),
				VerticalAlignment = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), vertical),
			};

			void Set(DependencyProperty dp, double? value)
			{
				childBorder.SetValue(dp, value.HasValue ? (object)value.Value : DependencyProperty.UnsetValue);
			}

			Set(FrameworkElement.MinWidthProperty, minWidth);
			Set(FrameworkElement.MinHeightProperty, minHeight);
			Set(FrameworkElement.WidthProperty, width);
			Set(FrameworkElement.HeightProperty, height);
			Set(FrameworkElement.MaxWidthProperty, maxWidth);
			Set(FrameworkElement.MaxHeightProperty, maxHeight);

			var parentBorder = new Border
			{
				Child = childDecorator,
				Background = new SolidColorBrush(Colors.Yellow),
				Name = "Parent",
				Width = 200d,
				Height = 100d,
				VerticalAlignment = VerticalAlignment.Top, // ensure to see something on screen
				HorizontalAlignment = HorizontalAlignment.Left // ensure to see something on screen
			};

			// Act
			TestServices.WindowHelper.WindowContent = parentBorder;
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			string GetStr(FrameworkElement e)
			{
				var positionMatrix = ((MatrixTransform)e.TransformToVisual(parentBorder)).Matrix;
				return $"{positionMatrix.OffsetX};{positionMatrix.OffsetY};{e.ActualWidth};{e.ActualHeight}";
			}

			var resultStr = $"{GetStr(childDecorator)}|{GetStr(childBorder)}|{GetStr(innerChild)}";

#if __IOS__ || __ANDROID__
			var layout = parentBorder.ShowLocalVisualTree();
#else
			var layout = "";
#endif

			Assert.AreEqual(expectedResult, resultStr, layout);
		}

#if __ANDROID__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Native_Parent_And_Measure_Infinite()
		{
			const int InnerBorderHeight = 47;
			var native = new MyLinearLayout();
			var inner = new Border { Width = 200, Height = InnerBorderHeight };
			var outer = new Grid() { VerticalAlignment = VerticalAlignment.Center };
			var panel = new StackPanel();

			native.Child = inner;
			outer.Children.Add(native);
			panel.Children.Add(outer);

			TestServices.WindowHelper.WindowContent = panel;
			await TestServices.WindowHelper.WaitForIdle(); //StretchAffectsMeasure is set when Loaded is called

			panel.Measure(new Size(1000, 1000));

			var measuredHeightLogical = Math.Round(Uno.UI.ViewHelper.PhysicalToLogicalPixels(outer.MeasuredHeight));
			Assert.AreEqual(InnerBorderHeight, measuredHeightLogical);

			outer.Arrange(new Rect(0, 0, 1000, 1000));
			var actualHeight = Math.Round(outer.ActualHeight);
			Assert.AreEqual(InnerBorderHeight, actualHeight);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_AreDimensionsConstrained_And_Margin()
		{
			const double setHeight = 45d;
			var outerPanel = new Grid { Width = 72, Height = setHeight, Margin = new Thickness(8) };
#if !NETFX_CORE
			outerPanel.AreDimensionsConstrained = true;
#endif
			var innerView = new AspectRatioView { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
			outerPanel.Children.Add(innerView);

			TestServices.WindowHelper.WindowContent = outerPanel;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(setHeight, Math.Round(innerView.ActualHeight));

			outerPanel.InvalidateMeasure(); // On Android, AreDimensionsConstrained=true causes view to be measured+arranged through alternate code path

			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(setHeight, Math.Round(innerView.ActualHeight));
		}
	}

	public partial class MyControl01 : FrameworkElement
	{
		public List<Size> MeasureOverrides { get; } = new List<Size>();

		public Size? BaseAvailableSize;

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureOverrides.Add(availableSize);
			return base.MeasureOverride(BaseAvailableSize ?? availableSize);
		}
	}

	public partial class MyControl02 : ContentControl
	{
		public List<Size> MeasureOverrides { get; } = new List<Size>();

		public Size? BaseAvailableSize;

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureOverrides.Add(availableSize);
			return base.MeasureOverride(BaseAvailableSize ?? availableSize);
		}
	}

	public partial class AspectRatioView : FrameworkElement
	{
		public double AspectRatio { get; set; } = 1.5;

		protected override Size MeasureOverride(Size availableSize)
		{
			var height = double.IsPositiveInfinity(availableSize.Height) ? 0 : availableSize.Height;
			var width = height * AspectRatio;
			return new Size(width, height);
		}
	}
}
