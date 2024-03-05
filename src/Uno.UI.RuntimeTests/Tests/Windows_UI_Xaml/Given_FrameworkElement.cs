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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using FluentAssertions;
using FluentAssertions.Execution;
using Private.Infrastructure;
using MUXControlsTestApp.Utilities;
using Microsoft.UI.Xaml.Automation;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation.Metadata;






#if __IOS__
using UIKit;
#endif

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public partial class Given_FrameworkElement
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
#if WINAPPSDK // Those values only works on UWP, not on Uno
		[DataRow("", "\n", double.NaN, double.NaN)]
		[DataRow("abc", "0\n", double.NaN, 0d)]
		[DataRow("∞", "-∞", double.NaN, double.NaN)]
		[DataRow("21-----covfefe", "42 you're \"smart\"", 21d, 42d)]
		[DataRow("		21\n", "\n42-", 21d, 42d)]
#endif
		public void When_Using_Nan_And_Auto_Sizes(string w, string h, double expectedW, double expectedH)
		{
			using var _ = new AssertionScope();

			var sut1 = new ContentControl { Tag = w };

			// Bind sut1.Width to sut1.Tag
			sut1.SetBinding(
				FrameworkElement.WidthProperty,
				new Binding { Source = sut1, Path = new PropertyPath("Tag") });

			sut1.Width.Should().Be(expectedW, "sut1: Width bound after setting value");

			var sut2 = new ContentControl();

			// Bind sut2.Width to sut2.Tag
			sut2.SetBinding(
				FrameworkElement.WidthProperty,
				new Binding { Source = sut2, Path = new PropertyPath("Tag") });

			// Set sut2.Tag AFTER the binding
			sut2.Tag = w;

			sut2.Width.Should().Be(expectedW, "sut2: Width bound before setting value");

			var sut3 = new ContentControl { Tag = h };

			// Bind sut3.Height to sut3.Tag
			sut3.SetBinding(
				FrameworkElement.HeightProperty,
				new Binding { Source = sut3, Path = new PropertyPath("Tag") });

			sut3.Height.Should().Be(expectedH, "sut3: Height bound after setting value");

			var sut4 = new ContentControl();

			// Bind sut4.Height to sut4.Tag
			sut4.SetBinding(
				FrameworkElement.HeightProperty,
				new Binding { Source = sut4, Path = new PropertyPath("Tag") });

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
#if !WINAPPSDK
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

		private sealed partial class MyPanel : Panel
		{
			protected override Size MeasureOverride(Size availableSize)
			{
				var child = this.Children.Single();
				if (child is FrameworkElement { Name: "second" } childAsFE)
				{
					childAsFE.HorizontalAlignment = HorizontalAlignment.Left;
				}

				if (child is FrameworkElement { Name: "third" } childAsFE2)
				{
					childAsFE2.HorizontalAlignment = HorizontalAlignment.Center;
				}

				child.Measure(availableSize);

				return child.DesiredSize;
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				var child = this.Children.Single();
				child.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
				return finalSize;
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Alignment_Changes_During_Measure()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var first = new MyPanel()
			{
				Name = "first",
				Width = 50,
				Background = new SolidColorBrush(Colors.Gray),
				Children =
				{
					new MyPanel()
					{
						Background = new SolidColorBrush(Colors.Pink),
						Name = "second",
						Width = 50,
						Children =
						{
							new Grid()
							{
								Background = new SolidColorBrush(Colors.Green),
								Name = "third",
								Children =
								{
									new Rectangle()
									{
										Fill = new SolidColorBrush(Colors.Red),
										Width = 20,
										Height = 20,
										Name = "rectangle",
									},
								},
							},
						}
					},
				}
			};

			await UITestHelper.Load(first);
			var actualBitmap = await UITestHelper.ScreenShot(first);

			var expected = new Border()
			{
				Width = 50,
				Height = 20,
				Background = new SolidColorBrush(Colors.Pink),
				Child = new Border()
				{
					Width = 20,
					Height = 20,
					Background = new SolidColorBrush(Colors.Red),
				}
			};

			await UITestHelper.Load(expected);
			var expectedBitmap = await UITestHelper.ScreenShot(expected);

			await ImageAssert.AreEqualAsync(actualBitmap, expectedBitmap);
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
		[RunsOnUIThread]
#if __ANDROID__ || __MACOS__ // #9282 for macOS
		[Ignore]
#endif
		public async Task When_InvalidateDuringMeasure_Then_GetReMeasured()
		{
			var sut = new ObservableLayoutingControl();
			var count = 0;
			sut.OnMeasure += (snd, e) =>
			{
				if (count++ == 0)
				{
					snd.InvalidateMeasure();
				}
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();

			// count == 1 on WinUI
#if __SKIA__ || __WASM__
			Assert.AreEqual(1, count);
#else
			Assert.AreEqual(2, count);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
#if __ANDROID__ || __IOS__
		[Ignore]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_InvalidateDuringArrange_Then_GetReArranged()
		{
			var sut = new ObservableLayoutingControl();
			var count = 0;
			sut.OnArrange += (snd, e) =>
			{
				if (count++ == 0)
				{
					snd.InvalidateArrange();
				}
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(2, count);
		}

#if !WINAPPSDK
		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_InvalidateDuringArrange_Then_DoesNotInvalidateParents()
		{
			var sut = new Grid
			{
				Margin = new Thickness(0, 100, 0, 0),
				BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DeepPink),
				BorderThickness = new Thickness(5),
				MinWidth = 100,
				MinHeight = 100,
				RowDefinitions =
				{
					new RowDefinition{Height = 75},
					new RowDefinition(),
					//new RowDefinition{Height = 75}, // Not working on iOS when the line below is commented
				},
				Children =
				{
					new TextBlock{Text = "Hello world"},
					new Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsRepeater
						{
							ItemsSource="0123456789",
							ItemTemplate = new DataTemplate(() => new Border
							{
								BorderBrush= new SolidColorBrush(Microsoft.UI.Colors.Red),
								Margin= new Thickness(5),
								BorderThickness=new Thickness(5),
								Width=300,
								Child = new TextBlock
								{
									TextWrapping= TextWrapping.Wrap,
									Foreground = new SolidColorBrush(Microsoft.UI.Colors.Chartreuse)
								}.Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
							}),
							Layout = new Microsoft/* UWP don't rename */.UI.Xaml.Controls.StackLayout{Orientation = Orientation.Horizontal}
						}
						.Apply(ir => Grid.SetRow(ir, 1))
				}
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();

			sut.IsArrangeDirty.Should().BeFalse();

			TestServices.WindowHelper.WindowContent = null;
		}
#endif

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

		public partial class MyGrid : Grid
		{
			public Size AvailableSizeUsedForMeasure { get; private set; }
			protected override Size MeasureOverride(Size availableSize)
			{
				AvailableSizeUsedForMeasure = availableSize;
				return base.MeasureOverride(availableSize);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
#if __ANDROID__ || __IOS__
		[Ignore("Layouter doesn't work properly")]
#endif
		public async Task When_MinWidth_SmallerThan_AvailableSize()
		{
			Border content = null;
			ContentControl contentCtl = null;
			MyGrid grid = null;

			content = new Border { Width = 100, Height = 15 };

			contentCtl = new ContentControl { MinWidth = 110, Content = content };

			grid = new MyGrid() { MinWidth = 120 };

			grid.Children.Add(contentCtl);

			grid.Measure(new Size(50, 50));

			// This is matching Windows and is important to keep the behavior of this test like this.
			Assert.AreEqual(new Size(120, 50), grid.AvailableSizeUsedForMeasure);

#if __SKIA__
			// https://github.com/unoplatform/uno/issues/7271
			// This assert is NOT correct. The correct size is 50, 15
			// It's happening because ContentControl is measuring incorrectly because GetFirstChild is returning null instead of the Border.
			Assert.AreEqual(new Size(50, 0), grid.DesiredSize);
#else
			Assert.AreEqual(new Size(50, 15), grid.DesiredSize);
#endif

#if WINAPPSDK // Failing on WASM - https://github.com/unoplatform/uno/issues/2314
			Assert.AreEqual(new Size(110, 15), contentCtl.DesiredSize);
			Assert.AreEqual(new Size(100, 15), content.DesiredSize);
#endif

			grid.Arrange(new Rect(default, new Size(50, 50)));

			TestServices.WindowHelper.WindowContent = new Border { Child = grid, Width = 50, Height = 50 };

			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(10);
			await TestServices.WindowHelper.WaitForIdle();

#if WINAPPSDK // Failing on WASM - https://github.com/unoplatform/uno/issues/2314
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
#if WINAPPSDK // Those tests only works on UWP, not Uno yet
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_AreDimensionsConstrained_And_Margin()
		{
			const double setHeight = 45d;
			var outerPanel = new Grid { Width = 72, Height = setHeight, Margin = new Thickness(8) };
#if !WINAPPSDK
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
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Negative_Margin_NonZero_Size()
		{
			var SUT = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -16, 0, 0), Height = 120 };

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
			var SUT = new Grid { VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -16, 0, 0) };

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
		public void When_Add_Native_Child_To_ElementCollection()
		{
			var panel = new Grid();
			var tbNativeTyped = (_View)new TextBlock();
			panel.Children.Add(tbNativeTyped);

			Assert.AreEqual(1, panel.Children.Count);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Set_Name()
		{
			var SUT = new Border();
			SUT.Name = "Test";
			var dpName = SUT.GetValue(FrameworkElement.NameProperty);
			Assert.AreEqual("Test", dpName);
		}

		[TestMethod]
		[RunsOnUIThread]
		public void When_Set_NameProperty()
		{
			var SUT = new Border();
			SUT.SetValue(FrameworkElement.NameProperty, "Test");
			var name = SUT.Name;
			Assert.AreEqual("Test", name);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Add_Element_Then_Unload_Raised()
		{
			var sut = new Border();
			var hostPanel = new Grid { Children = { sut } };

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

		[TestMethod]
		[RunsOnUIThread]
		public void When_BaseUri()
		{
			var sut = new Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.ButtonUserControl();

			Assert.AreEqual(
				new Uri("ms-appx:///Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Controls/ButtonUserControl.xaml"),
				sut.BaseUri);
		}

#if __IOS__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_HasNativeChildren_Should_Measure_And_Arrange()
		{
			var sut = new MyNativeContainer() { Width = 100, Height = 100 };
			var nativeView = new UILabel() { Text = "Hello Uno Platform" };

			sut.AddChild(nativeView);

			var hostPanel = new Grid { Children = { sut } };

			TestServices.WindowHelper.WindowContent = hostPanel;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, sut.Subviews.Length);

			Assert.AreEqual(100, nativeView.Frame.Width);
			Assert.AreEqual(100, nativeView.Frame.Height);

			Assert.AreEqual(0, nativeView.Frame.X);
			Assert.AreEqual(0, nativeView.Frame.Y);
		}
#endif

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

	public partial class MyNativeContainer : FrameworkElement { }

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

	public partial class ObservableLayoutingControl : FrameworkElement
	{
		public
#if __ANDROID__
		new
#endif
		event TypedEventHandler<ObservableLayoutingControl, Size> OnMeasure;

		public event TypedEventHandler<ObservableLayoutingControl, Size> OnArrange;

		protected override Size MeasureOverride(Size availableSize)
		{
			OnMeasure?.Invoke(this, availableSize);
			return new Size(100, 100);
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			OnArrange?.Invoke(this, finalSize);
			return new Size(100, 100);
		}
	}
}
