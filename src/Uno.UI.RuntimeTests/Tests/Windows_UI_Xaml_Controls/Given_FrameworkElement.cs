#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Private.Infrastructure;
using MUXControlsTestApp.Utilities;
#if __IOS__
using UIKit;
#endif

#if XAMARIN_ANDROID
using _View = Android.Views.View;
#elif XAMARIN_IOS
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Windows.UI.Xaml.UIElement;
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
			RunOnUIThread.ExecuteAsync(() =>
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
		[RunsOnUIThread]
		[DataRow("Auto", "Auto", double.NaN, double.NaN)]
		[DataRow("auto", "auto", double.NaN, double.NaN)]
		[DataRow("   AUTO", "AUTO   ", double.NaN, double.NaN)]
		[DataRow("auTo", "auTo", double.NaN, double.NaN)]
		[DataRow("NaN", "	NaN			", double.NaN, double.NaN)]
		[DataRow("NAN", "nAn", double.NaN, double.NaN)]
		[DataRow("0", "-0", 0d, 0d)]
		[DataRow("21", "42", 21d, 42d)]
		[DataRow("+21", "+42", 21d, 42d)]
#if NETFX_CORE // Those values only works on UWP, not on Uno
		[DataRow("", "\n", double.NaN, double.NaN)]
		[DataRow("abc", "0\n", double.NaN, 0d)]
		[DataRow("∞", "-∞", double.NaN, double.NaN)]
		[DataRow("21-----covfefe", "42 you're \"smart\"", 21d, 42d)]
		[DataRow("		21\n", "\n42-", 21d, 42d)]
#endif
		public void When_Using_Nan_And_Auto_Sizes(string w, string h, double expectedW, double expectedH)
		{
			using var _ = new AssertionScope();

			var sut1 = new ContentControl {Tag = w};

			// Bind sut1.Width to sut1.Tag
			sut1.SetBinding(
				FrameworkElement.WidthProperty,
				new Binding {Source = sut1, Path = new PropertyPath("Tag")});

			sut1.Width.Should().Be(expectedW, "sut1: Width bound after setting value");

			var sut2 = new ContentControl();

			// Bind sut2.Width to sut2.Tag
			sut2.SetBinding(
				FrameworkElement.WidthProperty,
				new Binding {Source = sut2, Path = new PropertyPath("Tag")});

			// Set sut2.Tag AFTER the binding
			sut2.Tag = w;

			sut2.Width.Should().Be(expectedW, "sut2: Width bound before setting value");

			var sut3 = new ContentControl {Tag = h};

			// Bind sut3.Height to sut3.Tag
			sut3.SetBinding(
				FrameworkElement.HeightProperty,
				new Binding {Source = sut3, Path = new PropertyPath("Tag")});

			sut3.Height.Should().Be(expectedH, "sut3: Height bound after setting value");

			var sut4 = new ContentControl();

			// Bind sut4.Height to sut4.Tag
			sut4.SetBinding(
				FrameworkElement.HeightProperty,
				new Binding {Source = sut4, Path = new PropertyPath("Tag")});

			// Set sut4.Tag AFTER the binding
			sut4.Tag = h;

			sut4.Height.Should().Be(expectedH, "sut4: Height bound before setting value");
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow("-42")]
		[DataRow("Infinity")]
		[DataRow("+Infinity")]
		[DataRow("	Infinity")]
		[DataRow("-Infinity ")]
		[DataRow("	Infinity")]
		[ExpectedException(typeof(ArgumentException))]
#if !NETFX_CORE
		[Ignore]
#endif
		public void When_Setting_Sizes_To_Invalid_Values_Then_Should_Throw(string variant)
		{
			using var _ = new AssertionScope();

			var sut = new ContentControl { Tag = variant };

			sut.SetBinding(
				FrameworkElement.WidthProperty,
				new Binding { Source = sut, Path = new PropertyPath("Tag") });
		}

		[TestMethod]
		public Task When_Measure_And_Invalidate() =>
			RunOnUIThread.ExecuteAsync(() =>
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
			RunOnUIThread.ExecuteAsync(() =>
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
			RunOnUIThread.ExecuteAsync(() =>
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
			RunOnUIThread.ExecuteAsync(() =>
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
			RunOnUIThread.ExecuteAsync(() =>
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
				Margin = ThicknessHelper.FromLengths(4d, 8d, 4d, 8d),
				BorderThickness = ThicknessHelper.FromLengths(3d, 6d, 3d, 6d),
				BorderBrush = new SolidColorBrush(Colors.DeepSkyBlue),
				Padding = ThicknessHelper.FromLengths(5d, 7d, 5d, 7d),
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
			var outerPanel = new Grid { Width = 72, Height = setHeight, Margin = ThicknessHelper.FromUniformLength(8) };
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

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Negative_Margin_NonZero_Size()
		{
			var SUT = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = ThicknessHelper.FromLengths(0, -16, 0, 0), Height = 120 };

			var hostPanel = new Grid();
			hostPanel.Children.Add(SUT);

			TestServices.WindowHelper.WindowContent = hostPanel;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(104d, Math.Round(SUT.DesiredSize.Height));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Negative_Margin_Zero_Size()
		{
			var SUT = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = ThicknessHelper.FromLengths(0, -16, 0, 0) };

			var hostPanel = new Grid();
			hostPanel.Children.Add(SUT);

			TestServices.WindowHelper.WindowContent = hostPanel;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(0d, Math.Round(SUT.DesiredSize.Height));
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Add_Element_Then_Load_Raised()
		{
			var sut = new Border();
			var hostPanel = new Grid();

			TestServices.WindowHelper.WindowContent = hostPanel;
			await TestServices.WindowHelper.WaitForIdle();

			int loadingCount = 0, loadedCount = 0;
			sut.Loading += (snd, e) => loadingCount++;
			sut.Loaded += (snd, e) => loadedCount++;

			hostPanel.Children.Add(sut);

			Assert.AreEqual(1, loadingCount, "loading");
			Assert.AreEqual(1, loadedCount, "loaded");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Add_Native_Child_To_ElementCollection()
		{
			var panel = new Grid();
			var tbNativeTyped = (_View)new TextBlock();
			panel.Children.Add(tbNativeTyped);

			Assert.AreEqual(1, panel.Children.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Add_Element_Then_Unload_Raised()
		{
			var sut = new Border();
			var hostPanel = new Grid {Children = {sut}};

			TestServices.WindowHelper.WindowContent = hostPanel;
			await TestServices.WindowHelper.WaitForIdle();

			var unloadCount = 0;
			sut.Unloaded += (snd, e) => unloadCount++;

			hostPanel.Children.Remove(sut);

			Assert.AreEqual(1, unloadCount);
#if UNO_REFERENCE_API
			Assert.IsFalse(hostPanel._children.Contains(sut));
#endif
		}

#if UNO_REFERENCE_API
		// Those tests only validate the current behavior which should be reviewed by https://github.com/unoplatform/uno/issues/2895
		// (cf. notes in the tests)

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Add_Element_While_Parent_Loading_Then_Load_Raised()
		{
			var sut = new Border();
			var hostPanel = new Grid();

			int loadingCount = 0, loadedCount = 0;
			var success = false;
			sut.Loading += (snd, e) => loadingCount++;
			sut.Loaded += (snd, e) => loadedCount++;

			hostPanel.Loading += (snd, e) =>
			{
				hostPanel.Children.Add(sut);

				// Note: This is NOT the case on UWP. Loading and Loaded event are raised on the child (aka. 'sut'')
				//		 only after the completion of the current handler.
				Assert.AreEqual(1, loadingCount, "loading");
				Assert.AreEqual(0, loadedCount, "loaded");
				success = true;
			};

			TestServices.WindowHelper.WindowContent = hostPanel;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(success);
			Assert.AreEqual(1, loadingCount, "loading");
			Assert.AreEqual(1, loadedCount, "loaded");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Add_Element_While_Parent_Loaded_Then_Load_Raised()
		{
			var sut = new Border();
			var hostPanel = new Grid();

			int loadingCount = 0, loadedCount = 0;
			var success = false;
			sut.Loading += (snd, e) => loadingCount++;
			sut.Loaded += (snd, e) => loadedCount++;

			hostPanel.Loaded += (snd, e) =>
			{
				hostPanel.Children.Add(sut);

				// Note: This is NOT the case on UWP. Loading and Loaded event are raised on the child (aka. 'sut'')
				//		 only after the completion of the current handler.
				// Note 2: On UWP, when adding a child to the parent while in the parent's loading event handler (i.e. this case)
				//		   the child will receive the Loaded ** BEFORE ** the Loading event.
				Assert.AreEqual(1, loadingCount, "loading");
				Assert.AreEqual(1, loadedCount, "loaded");

				success = true;
			};

			TestServices.WindowHelper.WindowContent = hostPanel;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(success);
			Assert.AreEqual(1, loadingCount, "loading");
			Assert.AreEqual(1, loadedCount, "loaded");
		}
#endif
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
