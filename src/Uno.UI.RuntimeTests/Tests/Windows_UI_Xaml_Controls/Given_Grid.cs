using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.GridPages;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Uno.Extensions;
using Uno.UI.Toolkit.DevTools.Input;

#if HAS_UNO
using DirectUI;
using Uno.UI.Helpers;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Grid
	{
		[TestMethod]
		[DataRow(20, 20, 70, 60, 0, 0, 90, 20, 110, 0, 30, 20, 160, 0, 90, 20, 0, 40, 90, 260, 110, 40, 30, 120, 160, 40, 90, 120, 110, 180, 140, 120)]
		[DataRow(20, 80, 70, 180, 0, 0, 90, 20, 110, 0, 30, 20, 160, 0, 90, 20, 0, 100, 90, 200, 110, 100, 30, 60, 160, 100, 90, 60, 110, 240, 140, 60)]
		[DataRow(80, 80, 190, 180, 0, 0, 30, 20, 110, 0, 30, 20, 220, 0, 30, 20, 0, 100, 30, 200, 110, 100, 30, 60, 220, 100, 30, 60, 110, 240, 140, 60)]
		[DataRow(-20, 20, 0, 60, 0, 0, 130, 20, 110, 0, 30, 20, 120, 0, 130, 20, 0, 40, 130, 260, 110, 40, 30, 120, 120, 40, 130, 120, 110, 180, 140, 120)]
		[DataRow(20, -20, 70, 0, 0, 0, 90, 20, 110, 0, 30, 20, 160, 0, 90, 20, 0, 0, 90, 300, 110, 0, 30, 160, 160, 0, 90, 160, 110, 140, 140, 160)]
		[DataRow(-20, -20, 0, 0, 0, 0, 130, 20, 110, 0, 30, 20, 120, 0, 130, 20, 0, 0, 130, 300, 110, 0, 30, 160, 120, 0, 130, 160, 110, 140, 140, 160)]
		public async Task When_Has_ColumnSpacing(double columnSpacing,
			double rowSpacing,
			double gridDesiredWidthExpected, double gridDesiredHeightExpected,
			double child0LeftExpected, double child0TopExpected, double child0WidthExpected, double child0HeightExpected,
			double child1LeftExpected, double child1TopExpected, double child1WidthExpected, double child1HeightExpected,
			double child2LeftExpected, double child2TopExpected, double child2WidthExpected, double child2HeightExpected,
			double child3LeftExpected, double child3TopExpected, double child3WidthExpected, double child3HeightExpected,
			double child4LeftExpected, double child4TopExpected, double child4WidthExpected, double child4HeightExpected,
			double child5LeftExpected, double child5TopExpected, double child5WidthExpected, double child5HeightExpected,
			double child6LeftExpected, double child6TopExpected, double child6WidthExpected, double child6HeightExpected
			)
		{
			await RunOnUIThread.ExecuteAsync(() =>
			{
				TestServices.WindowHelper.WindowContent = null;
			});

			Grid SUT = null;
			await RunOnUIThread.ExecuteAsync(() =>
			{
				SUT = new Grid
				{
					ColumnSpacing = columnSpacing,
					RowSpacing = rowSpacing,
					ColumnDefinitions =
					{
						new ColumnDefinition {Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
						new ColumnDefinition {Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Auto)},
						new ColumnDefinition {Width = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
					},
					RowDefinitions =
					{
						new RowDefinition {Height = GridLengthHelper2.FromValueAndType(1, GridUnitType.Auto)},
						new RowDefinition {Height = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
						new RowDefinition {Height = GridLengthHelper2.FromValueAndType(1, GridUnitType.Star)},
					},
					Children =
					{
						GetChild(0, 0, height: 20),
						GetChild(1, 0, width:30, height:20),
						GetChild(2, 0, height:20),

						GetChild(0, 1, rowSpan:2),
						GetChild(1, 1, width:30),
						GetChild(2, 1),

						GetChild(1,2, colSpan: 2)
					}
				};

				var outerBorder = new Border { Width = 250, Height = 300 };
				outerBorder.Child = SUT;

				TestServices.WindowHelper.WindowContent = outerBorder;

				FrameworkElement GetChild(int gridCol, int gridRow, int? colSpan = null, int? rowSpan = null, double? width = null, double? height = null)
				{
					var child = new Border();

					Grid.SetColumn(child, gridCol);
					Grid.SetRow(child, gridRow);
					if (colSpan.HasValue)
					{
						Grid.SetColumnSpan(child, colSpan.Value);
					}
					if (rowSpan.HasValue)
					{
						Grid.SetRowSpan(child, rowSpan.Value);
					}
					if (width.HasValue)
					{
						child.Width = width.Value;
					}
					else
					{
						child.HorizontalAlignment = HorizontalAlignment.Stretch;
					}
					if (height.HasValue)
					{
						child.Height = height.Value;
					}
					else
					{
						child.VerticalAlignment = VerticalAlignment.Stretch;
					}

					return child;
				}
			});

			await WaitForMeasure(SUT);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var desiredSize = SUT.DesiredSize;
				var data = $"({columnSpacing}, {rowSpacing}, {desiredSize.Width}, {desiredSize.Height}";
				foreach (var child in SUT.Children)
				{
					var layoutRect = LayoutInformation.GetLayoutSlot(child as FrameworkElement);
					data += $", {layoutRect.Left}, {layoutRect.Top}, {layoutRect.Width}, {layoutRect.Height}";
				}
				data += ")";
				Debug.WriteLine(data);

				Assert.AreEqual(new Size(gridDesiredWidthExpected, gridDesiredHeightExpected), desiredSize);

#if !__ANDROID__ && !__APPLE_UIKIT__ // These assertions fail on Android/iOS because layout slots aren't set the same way as UWP
				var layoutRect0Actual = LayoutInformation.GetLayoutSlot(SUT.Children[0] as FrameworkElement);
				var layoutRect0Expected = new Rect(child0LeftExpected, child0TopExpected, child0WidthExpected, child0HeightExpected);
				Assert.AreEqual(layoutRect0Expected, layoutRect0Actual);

				var layoutRect1Actual = LayoutInformation.GetLayoutSlot(SUT.Children[1] as FrameworkElement);
				var layoutRect1Expected = new Rect(child1LeftExpected, child1TopExpected, child1WidthExpected, child1HeightExpected);
				Assert.AreEqual(layoutRect1Expected, layoutRect1Actual);

				var layoutRect2Actual = LayoutInformation.GetLayoutSlot(SUT.Children[2] as FrameworkElement);
				var layoutRect2Expected = new Rect(child2LeftExpected, child2TopExpected, child2WidthExpected, child2HeightExpected);
				Assert.AreEqual(layoutRect2Expected, layoutRect2Actual);

				var layoutRect3Actual = LayoutInformation.GetLayoutSlot(SUT.Children[3] as FrameworkElement);
				var layoutRect3Expected = new Rect(child3LeftExpected, child3TopExpected, child3WidthExpected, child3HeightExpected);
				Assert.AreEqual(layoutRect3Expected, layoutRect3Actual);

				var layoutRect4Actual = LayoutInformation.GetLayoutSlot(SUT.Children[4] as FrameworkElement);
				var layoutRect4Expected = new Rect(child4LeftExpected, child4TopExpected, child4WidthExpected, child4HeightExpected);
				Assert.AreEqual(layoutRect4Expected, layoutRect4Actual);

				var layoutRect5Actual = LayoutInformation.GetLayoutSlot(SUT.Children[5] as FrameworkElement);
				var layoutRect5Expected = new Rect(child5LeftExpected, child5TopExpected, child5WidthExpected, child5HeightExpected);
				Assert.AreEqual(layoutRect5Expected, layoutRect5Actual);

				var layoutRect6Actual = LayoutInformation.GetLayoutSlot(SUT.Children[6] as FrameworkElement);
				var layoutRect6Expected = new Rect(child6LeftExpected, child6TopExpected, child6WidthExpected, child6HeightExpected);
				Assert.AreEqual(layoutRect6Expected, layoutRect6Actual);
#endif

				TestServices.WindowHelper.WindowContent = null;
			});
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#elif __WASM__
		[Ignore("Failing on WASM: https://github.com/unoplatform/uno/issues/17742")]
#endif
		public async Task When_Grid_Child_Canvas_ZIndex()
		{
			var btn = new Button();
			var border = new Border
			{
				Background = new SolidColorBrush(Microsoft.UI.Colors.Blue),
				Width = 200,
				Height = 200
			};
			var SUT = new Grid
			{
				Children =
				{
					btn,
					border
				}
			};

			btn.SetValue(Canvas.ZIndexProperty, 1);

			var buttonClickCount = 0;
			btn.Click += (_, _) => buttonClickCount++;

			await UITestHelper.Load(SUT);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			mouse.Press(btn.GetAbsoluteBoundsRect().GetCenter());
			mouse.Release();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(1, buttonClickCount);
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ColumnDefinition_Width_Changed()
		{
			var outerShell = new Grid { Width = 290, Height = 220 };

			var colDef0 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
			var colDef1 = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
			var SUT = new Grid
			{
				ColumnDefinitions =
				{
					colDef0,
					colDef1,
				}
			};
			outerShell.Children.Add(SUT);
			AddChild(SUT, new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }, 0, 0);
			AddChild(SUT, new Border { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch }, 0, 1);

			TestServices.WindowHelper.WindowContent = outerShell;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			const double expectedColWidth = 290 / 2;
			Assert.AreEqual(expectedColWidth, colDef0.ActualWidth);
			Assert.AreEqual(expectedColWidth, colDef1.ActualWidth);

			colDef0.Width = new GridLength(80, GridUnitType.Pixel);
			Assert.AreEqual(expectedColWidth, colDef0.ActualWidth);
			Assert.AreEqual(expectedColWidth, colDef1.ActualWidth);

			await TestServices.WindowHelper.WaitForRelayouted(SUT);

			Assert.AreEqual(80, colDef0.ActualWidth);
			Assert.AreEqual(210, colDef1.ActualWidth);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Definitions_Cleared_And_Empty()
		{
			var SUT = new Grid();
			var tb = new TextBlock { Text = "Text" };
			AddChild(SUT, new Border { Width = 50, Height = 20 }, 0, 0);
			AddChild(SUT, tb, 0, 0);

			TestServices.WindowHelper.WindowContent = SUT;
			SUT.ColumnDefinitions.Clear();

			await TestServices.WindowHelper.WaitForLoaded(SUT);

			await TestServices.WindowHelper.WaitForLoaded(tb); // Needed for iOS where measurement is async

			NumberAssert.Greater(tb.ActualWidth, 0);
			NumberAssert.Greater(tb.ActualHeight, 0);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Padding_Set_In_SizeChanged()
		{
			var SUT = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition()
					{
						Height = new GridLength(200)
					}
				},
				ColumnDefinitions =
				{
					new ColumnDefinition()
					{
						Width = new GridLength(200)
					}
				},
				Children =
				{
					new Border()
					{
						Child = new Ellipse()
						{
							Fill = new SolidColorBrush(Colors.DarkOrange)
						}
					}
				}
			};

			SUT.SizeChanged += (sender, args) => SUT.Padding = new Thickness(0, 200, 0, 0);

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);
			await TestServices.WindowHelper.WaitForIdle();

			// We have a problem on IOS and Android where SUT isn't relayouted after the padding
			// change even though IsMeasureDirty is true. This is a workaround to explicity relayout.
#if __APPLE_UIKIT__ || __ANDROID__
			SUT.InvalidateMeasure();
			SUT.UpdateLayout();
#endif

			Assert.AreEqual(200, ((UIElement)VisualTreeHelper.GetChild(SUT, 0)).ActualOffset.Y);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task Change_Grid_Row_After_Load()
		{
			Border firstBorder;
			Border secondBorder;
			var container = new Grid();
			var SUT = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				RowDefinitions =
				{
					new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
					new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
					new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
				},
				Children =
				{
					(firstBorder = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) }),
					(secondBorder = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Green) }),
				}
			};

			Grid.SetRow(firstBorder, 0);
			Grid.SetRow(secondBorder, 1);
			container.Children.Add(SUT);
			TestServices.WindowHelper.WindowContent = container;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			Point GetRelativePosition(FrameworkElement element)
			{
				var position = element.TransformToVisual(SUT).TransformPoint(new Point(0, 0));
				return position;
			}

			var firstPosition = GetRelativePosition(firstBorder);
			var secondPosition = GetRelativePosition(secondBorder);
			Assert.AreEqual(0, firstPosition.Y);
			Assert.AreEqual(100, secondPosition.Y);

			Grid.SetRow(firstBorder, 2);

			await TestServices.WindowHelper.WaitForIdle();

			firstPosition = GetRelativePosition(firstBorder);
			secondPosition = GetRelativePosition(secondBorder);

			Assert.AreEqual(100, firstPosition.Y);
			Assert.AreEqual(0, secondPosition.Y);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task Change_Grid_Column_After_Load()
		{
			Border firstBorder;
			Border secondBorder;
			var container = new Grid();
			var SUT = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
				},
				Children =
				{
					(firstBorder = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) }),
					(secondBorder = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Green) }),
				}
			};

			Grid.SetColumn(firstBorder, 0);
			Grid.SetColumn(secondBorder, 1);
			container.Children.Add(SUT);
			TestServices.WindowHelper.WindowContent = container;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			Point GetRelativePosition(FrameworkElement element)
			{
				var position = element.TransformToVisual(SUT).TransformPoint(new Point(0, 0));
				return position;
			}

			var firstPosition = GetRelativePosition(firstBorder);
			var secondPosition = GetRelativePosition(secondBorder);
			Assert.AreEqual(0, firstPosition.X);
			Assert.AreEqual(100, secondPosition.X);

			Grid.SetColumn(firstBorder, 2);

			await TestServices.WindowHelper.WaitForIdle();

			firstPosition = GetRelativePosition(firstBorder);
			secondPosition = GetRelativePosition(secondBorder);

			Assert.AreEqual(100, firstPosition.X);
			Assert.AreEqual(0, secondPosition.X);

			await Task.Delay(1000);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task Change_Grid_RowSpan_After_Load()
		{
			Border firstBorder;
			var container = new Grid();
			var SUT = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Background = new SolidColorBrush(Colors.Blue),
				RowSpacing = 10,
				RowDefinitions =
				{
					new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
					new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }
				},
				Children =
				{
					(firstBorder = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) }),
				}
			};

			Grid.SetRowSpan(firstBorder, 2);
			container.Children.Add(SUT);
			TestServices.WindowHelper.WindowContent = container;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(100, SUT.ActualHeight);

			Grid.SetRowSpan(firstBorder, 1);

			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(110, SUT.ActualHeight);

			await Task.Delay(1000);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task Change_Grid_ColumnSpan_After_Load()
		{
			Border firstBorder;
			var container = new Grid();
			var SUT = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Background = new SolidColorBrush(Colors.Blue),
				ColumnSpacing = 10,
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
					new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }
				},
				Children =
				{
					(firstBorder = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) }),
				}
			};

			Grid.SetColumnSpan(firstBorder, 2);
			container.Children.Add(SUT);
			TestServices.WindowHelper.WindowContent = container;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			Assert.AreEqual(100, SUT.ActualWidth);

			Grid.SetColumnSpan(firstBorder, 1);

			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(110, SUT.ActualWidth);

			await Task.Delay(1000);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Child_Added_Measure_And_Visible_Arrange()
		{
			// This test emulates the layout sequence associated with DataGridColumnHeadersPresenter in the WCT
			var gridAdder = new GridAdder() { MinWidth = 5, MinHeight = 5 };
			TestServices.WindowHelper.WindowContent = gridAdder;
			await TestServices.WindowHelper.WaitForLoaded(gridAdder);

			await TestServices.WindowHelper.WaitFor(() => gridAdder.WasArranged); // Needed for iOS where measurement is async

			if (gridAdder.Exception != null)
			{
				throw new AssertFailedException("Exception occurred", gridAdder.Exception);
			}

			var SUT = gridAdder.AddedGrid;
			Assert.IsNotNull(SUT);
			Assert.AreEqual(Visibility.Visible, SUT.Visibility);

#if !__ANDROID__ && !__APPLE_UIKIT__ // The Grid contents doesn't seem to actually display properly when added this way, but at least it should not throw an exception.
			Assert.AreEqual(27, SUT.ActualHeight);
			NumberAssert.Greater(SUT.ActualWidth, 0);
#endif
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresScaling(1f)]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("Fails on Android and iOS.")]
#endif
		public async Task When_Negative_Margin_Should_Not_Clip()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // "System.NotImplementedException: RenderTargetBitmap is not supported on this platform.";
			}

			var yellowGrid = new Grid()
			{
				Margin = new Thickness(0, -4, -4, 0),
				Background = new SolidColorBrush(Colors.Yellow),
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = HorizontalAlignment.Right,
				Children =
						{
							new Ellipse
							{
								Height = 30,
								Width = 30,
								Fill = new SolidColorBrush(Colors.Red),
							},
						},
			};

			var parentGrid = new Grid()
			{
				Padding = new Thickness(10),
				Children =
				{
					new Grid()
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Colors.Purple),
						Children =
						{
							yellowGrid,
						}
					}
				},
			};

			TestServices.WindowHelper.WindowContent = parentGrid;
			await TestServices.WindowHelper.WaitForLoaded(parentGrid);

			var renderer = new RenderTargetBitmap();
			await renderer.RenderAsync(parentGrid);
			var bitmap = await RawBitmap.From(renderer, parentGrid);

			var purpleBounds = ImageAssert.GetColorBounds(bitmap, Colors.Purple, tolerance: 5);
			Assert.AreEqual(new Rect(new Point(10, 10), new Size(99, 99)), purpleBounds);

			var yellowBounds = ImageAssert.GetColorBounds(bitmap, Colors.Yellow, tolerance: 5);
			Assert.AreEqual(new Rect(new Point(84, 6), new Size(29, 29)), yellowBounds);

			var redBounds = ImageAssert.GetColorBounds(bitmap, Colors.Red, tolerance: 5);
			Assert.AreEqual(new Rect(new Point(84, 6), new Size(29, 29)), redBounds);
		}

		[TestMethod]
		[RunsOnUIThread]
		[RequiresScaling(1f)]
#if __ANDROID__ || __APPLE_UIKIT__
		[Ignore("Fails on Android and iOS.")]
#endif
		public async Task When_RenderTransform_Ensure_Correct_Clipping()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var grid = new Grid
			{
				Padding = new Thickness(100),
				Children =
				{
					new Grid
					{
						Width = 100,
						Height = 100,
						Background = new SolidColorBrush(Colors.Blue),
						Children =
						{
							new Ellipse
							{
								Fill = new SolidColorBrush(Colors.Red),
								Width = 120,
								Height = 120,
								RenderTransform = new RotateTransform
								{
									Angle = 45,
								},
							},
						},
					}
				}
			};

			TestServices.WindowHelper.WindowContent = grid;
			await TestServices.WindowHelper.WaitForLoaded(grid);

			var renderer = new RenderTargetBitmap();
			await renderer.RenderAsync(grid);
			var bitmap = await RawBitmap.From(renderer, grid);

			var blueBounds = ImageAssert.GetColorBounds(bitmap, Colors.Blue, tolerance: 5);
			Assert.AreEqual(new Rect(new Point(100, 100), new Size(99, 99)), blueBounds);
			var redBounds = ImageAssert.GetColorBounds(bitmap, Colors.Red, tolerance: 5);
			Assert.AreEqual(new Rect(new Point(41, 125), new Size(117, 114)), redBounds);
		}

#if __ANDROID__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Grid_Remeasure()
		{
			// uno#12315: rows/columns of Grid can sometimes fail to re-measure due to android measure caching
			// causing incorrect measure/arrange for views belonging to that rows/columns.

			// ensure the flag is active, and will be restored after test
			var flag = FeatureConfiguration.FrameworkElement.InvalidateNativeCacheOnRemeasure;
			using var restore = Disposable.Create(() => FeatureConfiguration.FrameworkElement.InvalidateNativeCacheOnRemeasure = flag);
			FeatureConfiguration.FrameworkElement.InvalidateNativeCacheOnRemeasure = true;

			var setup = XamlHelper.LoadXaml<Border>("""
				<Border BorderThickness="5" BorderBrush="Red">
					<Grid x:Name="SutGrid" Height="400" Width="600">
						<Grid.RowDefinitions>
							<RowDefinition Height="Auto" />
							<RowDefinition Height="*" />
						</Grid.RowDefinitions>

						<StackPanel>
							<!--<Button x:Name="ToggleVisibility" Content="ON|OFF" Style="{StaticResource BasicButtonStyle}" />-->
							<Border x:Name="TogglableBorder" Height="50" Background="SkyBlue" />
						</StackPanel>

						<ListView x:Name="SutLV" Grid.Row="1" ItemsSource="{Binding}" />
						<TextBlock Grid.Row="1" Text="TEXT MUST BE HERE" HorizontalAlignment="Center" VerticalAlignment="Bottom" />
					</Grid>
				</Border>
				""");
			setup.DataContext = Enumerable.Range(0, 50);

			var grid = setup.FindFirstDescendant<Grid>(x => x.Name == "SutGrid");
			var togglableBorder = setup.FindFirstDescendant<Border>(x => x.Name == "TogglableBorder");
			var lv = setup.FindFirstDescendant<ListView>(x => x.Name == "SutLV");

			TestServices.WindowHelper.WindowContent = setup;
			await TestServices.WindowHelper.WaitForLoaded(setup);
			await TestServices.WindowHelper.WaitForIdle();

			var initial = lv.ActualHeight;

			// On first cycle, everything will be normal.
			togglableBorder.Visibility = Visibility.Collapsed; // C = 1
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsGreaterThan(initial, lv.ActualHeight, $"C1: SutLV should've expanded with the border collapsed: Lv.ActualHeight>initial --> ({lv.ActualHeight}>{initial})");
			togglableBorder.Visibility = Visibility.Visible; // C = 2
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(DoubleUtil.AreWithinTolerance(lv.ActualHeight, initial, 0.1), $"C2: SutLV should've returned to initial size with the border visible: Lv.ActualHeight==initial --> ({lv.ActualHeight}=={initial})");

			// On the following cycles, everything should still be normal.
			// [Android:] However, in context of uno12315, it would fail on C4,C6,C8...
			// due to measure caching of Android.Views.View.Measure inside the measure pass of Grid.
			togglableBorder.Visibility = Visibility.Collapsed; // C = 3
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsGreaterThan(initial, lv.ActualHeight, $"C3: SutLV should've expanded with the border collapsed: Lv.ActualHeight>initial --> ({lv.ActualHeight}>{initial})");
			togglableBorder.Visibility = Visibility.Visible; // C = 4
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(DoubleUtil.AreWithinTolerance(lv.ActualHeight, initial, 0.1), $"C4: SutLV should've returned to initial size with the border visible: Lv.ActualHeight==initial --> ({lv.ActualHeight}=={initial})");
		}
#endif

		private static void AddChild(Grid parent, FrameworkElement child, int row, int col, int? rowSpan = null, int? colSpan = null)
		{
			Grid.SetRow(child, row);
			Grid.SetColumn(child, col);

			if (rowSpan is { } rs)
			{
				Grid.SetRowSpan(child, rs);
			}
			if (colSpan is { } cs)
			{
				Grid.SetColumnSpan(child, cs);
			}

			parent.Children.Add(child);
		}

		private async Task WaitForMeasure(FrameworkElement view, int timeOutMs = 1000)
		{
			var isMeasured = false;
			var stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedMilliseconds < timeOutMs)
			{
				await RunOnUIThread.ExecuteAsync(() =>
				{
					isMeasured = view.DesiredSize != default(Size);
				});
				if (isMeasured)
				{
					return;
				}

				await Task.Delay(50);
			}
		}
	}
}
