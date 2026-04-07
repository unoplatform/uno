using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using SamplesApp.UITests;
using Path = Microsoft.UI.Xaml.Shapes.Path;
using Uno.UI.RuntimeTests.Helpers;
using System.Threading;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Path
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/6846")]
		public void Should_not_throw_if_Path_Data_is_set_to_null()
		{
			// Set initial Data
			var SUT = new Path { Data = new RectangleGeometry() };

			// Switch back to null.  Should not throw an exception.
			SUT.Data = null;
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void Should_Not_Include_Control_Points_Bounds()
		{
			var SUT = new Path { Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), "M 0 0 C 0 0 25 25 0 50") };

			SUT.Measure(new Size(300, 300));

#if WINAPPSDK
			Assert.AreEqual(new Size(11, 50), SUT.DesiredSize);
#else
			Assert.IsLessThanOrEqualTo(1, Math.Abs(11 - SUT.DesiredSize.Width), $"Actual size: {SUT.DesiredSize}");
			Assert.IsLessThanOrEqualTo(1, Math.Abs(50 - SUT.DesiredSize.Height), $"Actual size: {SUT.DesiredSize}");
#endif
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18694")]
#if !__SKIA__
		[Ignore("PathFigure.IsFilled's interaction with Path is only implemented on Skia.")]
#endif
		public async Task When_PathGeometry_Figures_Not_Filled_ColorBrush()
		{
			var SUT = new Path
			{
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Data = new PathGeometry
				{
					Figures = new PathFigureCollection
					{
						new PathFigure
						{
							StartPoint = new Point(0, 0),
							IsFilled = true,
							Segments = new PathSegmentCollection
							{
								new LineSegment { Point = new Point(100, 0) },
								new LineSegment { Point = new Point(100, 100) },
								new LineSegment { Point = new Point(0, 100) },
							}
						},
						new PathFigure // this is an unfilled rectangle without a stroke, should be useless
						{
							StartPoint = new Point(0, 0),
							IsFilled = false,
							Segments = new PathSegmentCollection
							{
								new LineSegment { Point = new Point(50, 0) },
								new LineSegment { Point = new Point(50, 50) },
								new LineSegment { Point = new Point(0, 50) },
							}
						},
						new PathFigure
						{
							StartPoint = new Point(0, 100),
							IsFilled = false,
							Segments = new PathSegmentCollection
							{
								new LineSegment { Point = new Point(100, 100) },
								new LineSegment { Point = new Point(100, 200) },
								new LineSegment { Point = new Point(0, 200) },
							}
						},
						new PathFigure
						{
							StartPoint = new Point(0, 200),
							IsFilled = true,
							Segments = new PathSegmentCollection
							{
								new LineSegment { Point = new Point(100, 200) },
								new LineSegment { Point = new Point(100, 300) },
								new LineSegment { Point = new Point(0, 300) },
							}
						}
					}
				}
			};

			await UITestHelper.Load(SUT);

			var screenShot = await UITestHelper.ScreenShot(SUT);
			ImageAssert.HasColorAt(screenShot, new Point(25, 25), Microsoft.UI.Colors.Red);
			ImageAssert.HasColorAt(screenShot, new Point(50, 50), Microsoft.UI.Colors.Red);
			ImageAssert.DoesNotHaveColorAt(screenShot, new Point(50, 150), Microsoft.UI.Colors.Red);
			ImageAssert.HasColorAt(screenShot, new Point(50, 250), Microsoft.UI.Colors.Red);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/18694")]
#if !__SKIA__
		[Ignore("PathFigure.IsFilled's interaction with Path is only implemented on Skia.")]
#endif
		public async Task When_PathGeometry_Figures_Not_Filled_ImageBrush()
		{
			var brush = new ImageBrush() { ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/rect.png")) };
#if !WINAPPSDK
			var brushOpenTask = WindowHelper.WaitForOpened(brush);
#endif

			var SUT = new Path
			{
				Fill = brush,
				Data = new PathGeometry
				{
					Figures = new PathFigureCollection
					{
						new PathFigure
						{
							StartPoint = new Point(0, 0),
							IsFilled = true,
							Segments = new PathSegmentCollection
							{
								new LineSegment { Point = new Point(100, 0) },
								new LineSegment { Point = new Point(100, 100) },
								new LineSegment { Point = new Point(0, 100) },
							}
						},
						new PathFigure // this is an unfilled rectangle, so the ImageBrush should fill the PathFigure above with the entire image (i.e. as if this PathFigure doesn't exist)
						{
							StartPoint = new Point(100, 0),
							IsFilled = false,
							Segments = new PathSegmentCollection
							{
								new LineSegment { Point = new Point(200, 0) },
								new LineSegment { Point = new Point(200, 100) },
								new LineSegment { Point = new Point(100, 100) },
							}
						}
					}
				}
			};

			await UITestHelper.Load(SUT);

#if !WINAPPSDK
			await brushOpenTask;
#endif

			var screenShot = await UITestHelper.ScreenShot(SUT);
			ImageAssert.HasColorAt(screenShot, new Point(90, 50), "#FF38FF52");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3238")]
#if !__SKIA__ && !WINAPPSDK
		[Ignore("Geometry.Transform is only implemented on Skia and WinUI.")]
#endif
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Geometry_Transform_Translates_Rendering()
		{
			var path = new Path
			{
				Data = new RectangleGeometry
				{
					Rect = new Rect(0, 0, 50, 50),
					Transform = new TranslateTransform { X = 60, Y = 60 }
				},
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Width = 150,
				Height = 150
			};

			await UITestHelper.Load(path);

			var screenshot = await UITestHelper.ScreenShot(path);

			// The rectangle is at (0,0)-(50,50) but translated by (60,60),
			// so it should render at (60,60)-(110,110).
			// Origin area should NOT be red
			ImageAssert.DoesNotHaveColorAt(screenshot, new Point(10, 10), Microsoft.UI.Colors.Red);
			// Translated position should be red
			ImageAssert.HasColorAt(screenshot, new Point(85, 85), Microsoft.UI.Colors.Red);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3238")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if !__SKIA__ && !WINAPPSDK
		[Ignore("Geometry.Transform is only implemented on Skia and WinUI.")]
#endif
		public async Task When_Geometry_Transform_Changed_At_Runtime()
		{
			var translate = new TranslateTransform { X = 0, Y = 0 };
			var path = new Path
			{
				Data = new RectangleGeometry
				{
					Rect = new Rect(0, 0, 50, 50),
					Transform = translate
				},
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Width = 150,
				Height = 150
			};

			await UITestHelper.Load(path);

			// Initially, the rect is at (0,0)
			var screenshotBefore = await UITestHelper.ScreenShot(path);
			ImageAssert.HasColorAt(screenshotBefore, new Point(25, 25), Microsoft.UI.Colors.Red);
			ImageAssert.DoesNotHaveColorAt(screenshotBefore, new Point(125, 125), Microsoft.UI.Colors.Red);

			// Change the transform at runtime (simulates what an animation would do)
			translate.X = 80;
			translate.Y = 80;

			await WindowHelper.WaitForIdle();

			// After transform change, the rect should have moved
			var screenshotAfter = await UITestHelper.ScreenShot(path);
			ImageAssert.HasColorAt(screenshotAfter, new Point(105, 105), Microsoft.UI.Colors.Red);
			// Origin should no longer be red
			ImageAssert.DoesNotHaveColorAt(screenshotAfter, new Point(10, 10), Microsoft.UI.Colors.Red);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3238")]
#if !__SKIA__ && !WINAPPSDK
		[Ignore("Geometry.Transform is only implemented on Skia and WinUI.")]
#endif
		public async Task When_GeometryGroup_Children_Have_Transforms()
		{
			var path = new Path
			{
				Data = new GeometryGroup
				{
					Children =
					{
						new RectangleGeometry
						{
							Rect = new Rect(0, 0, 40, 40),
						},
						new RectangleGeometry
						{
							Rect = new Rect(0, 0, 40, 40),
							Transform = new TranslateTransform { X = 80, Y = 80 }
						}
					}
				},
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Width = 150,
				Height = 150
			};

			await UITestHelper.Load(path);

			var screenshot = await UITestHelper.ScreenShot(path);

			// First rect at origin should be red
			ImageAssert.HasColorAt(screenshot, new Point(20, 20), Microsoft.UI.Colors.Red);
			// Second rect translated to (80,80) should be red
			ImageAssert.HasColorAt(screenshot, new Point(100, 100), Microsoft.UI.Colors.Red);
			// Gap between the two rects should NOT be red
			ImageAssert.DoesNotHaveColorAt(screenshot, new Point(60, 60), Microsoft.UI.Colors.Red);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19957")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if !__SKIA__ && !WINAPPSDK
		[Ignore("PolyLineSegment stroke rendering is validated on Skia and WinUI.")]
#endif
		public async Task When_PolyLineSegment_Is_Stroked_Renders_All_Points()
		{
			// A PathGeometry containing a LineSegment followed by a PolyLineSegment,
			// both stroke-only (no fill).  Before the fix, the PolyLineSegment portion
			// was invisible on Skia because the canvas Save() was missing before ClipPath().
			var SUT = new Path
			{
				Width = 200,
				Height = 100,
				Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black),
				StrokeThickness = 4,
				Data = new PathGeometry
				{
					Figures = new PathFigureCollection
					{
						new PathFigure
						{
							StartPoint = new Point(0, 50),
							IsClosed = false,
							Segments = new PathSegmentCollection
							{
								// Single LineSegment — always rendered correctly
								new LineSegment { Point = new Point(60, 50) },
								// PolyLineSegment — was silently dropped before the fix
								new PolyLineSegment
								{
									Points = new PointCollection
									{
										new Point(120, 50),
										new Point(200, 50),
									}
								},
							}
						}
					}
				}
			};

			await UITestHelper.Load(SUT);

			var screenshot = await UITestHelper.ScreenShot(SUT);

			// Mid-point of the LineSegment portion — should be black
			ImageAssert.HasColorAt(screenshot, new Point(30, 50), Microsoft.UI.Colors.Black);

			// Mid-point of the PolyLineSegment portion — must also be black
			ImageAssert.HasColorAt(screenshot, new Point(160, 50), Microsoft.UI.Colors.Black);

			// Area well above the line — must NOT be black
			ImageAssert.DoesNotHaveColorAt(screenshot, new Point(30, 10), Microsoft.UI.Colors.Black);
			ImageAssert.DoesNotHaveColorAt(screenshot, new Point(160, 10), Microsoft.UI.Colors.Black);
		}

		private void Brush_ImageOpened(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => throw new NotImplementedException();

		// Repro tests for https://github.com/unoplatform/uno/issues/4563
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4563")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_Paths_In_Canvas_With_MatrixTransform_Render_Without_Exception()
		{
			// Issue: Path objects inside nested Canvases with MatrixTransform and TranslateTransform
			// don't render correctly on WASM (visual mismatch with UWP).
			// This test verifies the layout doesn't throw and produces non-zero bounds.

			Exception caught = null;
			Microsoft.UI.Xaml.Controls.Canvas canvas = null;

			try
			{
				var root = (Microsoft.UI.Xaml.FrameworkElement)XamlReader.Load(
					@"<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
						   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
						   Width='200' Height='200'>
						<Viewbox>
							<Canvas x:Name='layer1' Width='53' Height='53'>
								<Canvas x:Name='g958'>
									<Canvas.RenderTransform>
										<MatrixTransform Matrix='1.1048446, 0, 0, 1.1959561, -67.035883, -96.098854'/>
									</Canvas.RenderTransform>
									<Canvas x:Name='g920'>
										<Canvas.RenderTransform>
											<TranslateTransform X='3.6170566' Y='26.121871'/>
										</Canvas.RenderTransform>
										<Path x:Name='path882' Fill='#FFFFFFFF' Data='m 89.844925 86.908932 -5.484915 -9.50015 -5.484913 -9.500149 10.969828 -10e-7 10.969825 0 -5.484911 9.50015 z'>
											<Path.RenderTransform>
												<MatrixTransform Matrix='0.99974084, 0, 0, 1.0590767, 0.02096173, -5.1400261'/>
											</Path.RenderTransform>
										</Path>
									</Canvas>
									<Rectangle Canvas.Left='68.170586' Canvas.Top='85.901428' Width='38.119644' Height='2.9399648' x:Name='rect9016' Fill='#FFFFFFFF'/>
								</Canvas>
							</Canvas>
						</Viewbox>
					</Grid>");

				canvas = root.FindName("layer1") as Microsoft.UI.Xaml.Controls.Canvas;

				WindowHelper.WindowContent = root;
				await WindowHelper.WaitForLoaded(root);
				await WindowHelper.WaitForIdle();
			}
			catch (Exception ex)
			{
				caught = ex;
			}

			Assert.IsNull(caught,
				$"Expected paths in nested Canvases with MatrixTransform to load without exception, but got: {caught?.Message}");

			Assert.IsNotNull(canvas, "Canvas layer1 should be found in visual tree.");
			Assert.IsTrue(canvas.ActualWidth > 0,
				$"Expected Canvas to have non-zero width after rendering.");
		}
	}
}
