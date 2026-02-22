using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using SamplesApp.UITests;
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

		private void Brush_ImageOpened(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => throw new NotImplementedException();
	}
}
