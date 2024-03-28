using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

#if WINAPPSDK
public static class Ext
{
	public static Color WithOpacity(this Color color, double opacity)
	{
		return new Color()
		{
			A = (byte)(color.A * opacity),
			R = color.R,
			G = color.G,
			B = color.B,
		};
	}
}
#endif


[TestClass]
internal class Given_CompositionClip
{
#if __SKIA__ || WINAPPSDK
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TransformElementClippedByParent_Then_ClippingAppliedPostRendering()
	{
		var sut = new Grid
		{
			Width = 300,
			Height = 300,
			Background = new SolidColorBrush(Colors.Chartreuse),
			Children =
			{
				new Grid
				{
					Width = 200,
					Height = 200,
					Background = new SolidColorBrush(Colors.DeepPink),
					Children =
					{
						new Grid
						{
							Width = 300,
							Height = 300,
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center,
							Background = new SolidColorBrush(Colors.DeepSkyBlue),
							RenderTransform = new TranslateTransform { Y = 200 },
						}
					}
				}
			}
		};

		var expected = new Canvas
		{
			Width = 300,
			Height = 300,
			Children =
			{
				new Rectangle
					{
						Width = 300,
						Height = 300,
						Fill = new SolidColorBrush(Colors.Chartreuse),
					},
				new Rectangle
					{
						Width = 200,
						Height = 200,
						Fill = new SolidColorBrush(Colors.DeepPink),
					}
					.Apply(r => Canvas.SetLeft(r, 50))
					.Apply(r => Canvas.SetTop(r, 50)),
				new Rectangle
					{
						Width = 200,
						Height = 50,
						Fill = new SolidColorBrush(Colors.DeepSkyBlue),
					}
					.Apply(r => Canvas.SetLeft(r, 50))
					.Apply(r => Canvas.SetTop(r, 200)),
			}
		};

		var (actual, expectedImg) = await Render(sut, expected);

		// First we check some well known important points
		ImageAssert.HasColorAt(actual, 150, 1, Colors.Chartreuse);
		ImageAssert.HasColorAt(actual, 150, 49, Colors.Chartreuse);
		ImageAssert.HasColorAt(actual, 150, 151, Colors.DeepPink);
		ImageAssert.HasColorAt(actual, 150, 199, Colors.DeepPink);
		ImageAssert.HasColorAt(actual, 150, 201, Colors.DeepSkyBlue);
		ImageAssert.HasColorAt(actual, 150, 249, Colors.DeepSkyBlue);
		ImageAssert.HasColorAt(actual, 150, 299, Colors.Chartreuse);

		// Second we do a full comparison of something that should be identical but drawn using another (simpler ?) mechanism
		await ImageAssert.AreEqual(actual, expectedImg);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TransformElementClippedByParentWithBorder_Then_ClippingAppliedPostRendering()
	{
		var sut = new Grid
		{
			Width = 300,
			Height = 300,
			Name = "Chartreuse",
			BorderBrush = new SolidColorBrush(Colors.Chartreuse),
			BorderThickness = new Thickness(5),
			Background = new SolidColorBrush(Colors.Chartreuse.WithOpacity(.5)),
			Children =
			{
				new Grid
				{
					Width = 200,
					Height = 200,
					Name = "Pink",
					BorderBrush = new SolidColorBrush(Colors.DeepPink),
					BorderThickness = new Thickness(5),
					Background = new SolidColorBrush(Colors.DeepPink.WithOpacity(.5)),
					Children =
					{
						new Grid
						{
							Width = 300,
							Height = 300,
							Name = "Blue",
							HorizontalAlignment = HorizontalAlignment.Center,
							VerticalAlignment = VerticalAlignment.Center,
							BorderBrush = new SolidColorBrush(Colors.DeepSkyBlue),
							BorderThickness = new Thickness(5),
							Background = new SolidColorBrush(Colors.DeepSkyBlue.WithOpacity(.5)),
							RenderTransform = new TranslateTransform { Y = 200 },
						}
					}
				}
			}
		};

		var expected = new Canvas
		{
			Width = 300,
			Height = 300,
			Children =
			{
				new Rectangle
					{
						Width = 300,
						Height = 300,
						Fill = new SolidColorBrush(Colors.Chartreuse.WithOpacity(.5)),
						Stroke = new SolidColorBrush(Colors.Chartreuse),
						StrokeThickness = 5,
					},
				new Rectangle
					{
						Width = 200,
						Height = 200,
						Fill = new SolidColorBrush(Colors.DeepPink.WithOpacity(.5)),
						Stroke = new SolidColorBrush(Colors.DeepPink),
						StrokeThickness = 5,
					}
					.Apply(r => Canvas.SetLeft(r, 50))
					.Apply(r => Canvas.SetTop(r, 50)),
				new Rectangle
					{
						Width = 190,
						Height = 45,
						Fill = new SolidColorBrush(Colors.DeepSkyBlue.WithOpacity(.5)),
					}
					.Apply(r => Canvas.SetLeft(r, 55))
					.Apply(r => Canvas.SetTop(r, 200)),
				new Line
					{
						X1 = 55,
						Y1 = 200,
						X2 = 55 + 190,
						Y2 = 200,
						Stroke = new SolidColorBrush(Colors.DeepSkyBlue),
						StrokeThickness = 5,
					}
			}
		};

		var (actual, expectedImg) = await Render(sut, expected);

		// First we check some well known important points
		ImageAssert.HasColorAt(actual, 1, 201, Colors.Chartreuse);
		//ImageAssert.HasColorAt(actual, 51, 201, Colors.DeepPink); // Known limitation, the blue is overflowing over the pink border
		ImageAssert.HasColorAt(actual, 56, 201, Colors.DeepSkyBlue);
		ImageAssert.HasColorAt(actual, 243, 201, Colors.DeepSkyBlue);
		ImageAssert.HasColorAt(actual, 246, 201, Colors.DeepPink);
		ImageAssert.HasColorAt(actual, 299, 201, Colors.Chartreuse);

		// Second we do a full comparison of something that should be identical but drawn using another (simpler ?) mechanism
		//await ImageAssert.AreEqualAsync(actual, expectedImg);  // Known limitation, the blue is overflowing over the pink border
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_MixTransformCliPropertyAndClippedByParentWithBorders_Then_RenderingIsValid()
	{
		var sut = new Grid
		{
			Width = 230,
			Height = 120,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			Children =
			{
				new Grid
				{
					Name = "red",
					Width = 100,
					Height = 100,
					Background = new SolidColorBrush(Colors.Red.WithOpacity(.5)),
					BorderBrush = new SolidColorBrush(Colors.Red),
					BorderThickness = new Thickness(6),
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left,
					RenderTransform = new CompositeTransform { ScaleX = 2 },
					Clip = new RectangleGeometry { Rect = new Rect(0, 0, 110, 110) },
					Children =
					{
						new Rectangle
						{
							Name = "blue",
							Width = 150,
							Height = 100,
							Fill = new SolidColorBrush(Colors.Blue.WithOpacity(.5)),
							Stroke = new SolidColorBrush(Colors.Blue),
							StrokeThickness = 5,
							VerticalAlignment = VerticalAlignment.Top,
							HorizontalAlignment = HorizontalAlignment.Left,
							RenderTransform = new TranslateTransform { X = 50, Y = 50 }
						},
						new Border
						{
							Name = "green",
							Width = 150,
							Height = 100,
							BorderBrush = new SolidColorBrush(Colors.Green),
							BorderThickness = new Thickness(6),
							Background = new SolidColorBrush(Colors.Green.WithOpacity(.5)),
							VerticalAlignment = VerticalAlignment.Top,
							HorizontalAlignment = HorizontalAlignment.Left,
							RenderTransform = new TranslateTransform { X = 50, Y = -50 },
							Child = new Rectangle
							{
								Name = "orange",
								Width = 50,
								Height = 50,
								Fill = new SolidColorBrush(Colors.Orange),
								VerticalAlignment = VerticalAlignment.Top,
								HorizontalAlignment = HorizontalAlignment.Left,
								RenderTransform = new TranslateTransform { X = 25, Y = 0 }
							}
						}
					}
				}
			}
		};

		var expected = new Canvas
		{
			Width = 230,
			Height = 120,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Children =
			{
				new Rectangle { Width = 200, Height = 100, Fill = new SolidColorBrush(Colors.Red.WithOpacity(.5)) },
				new Line { X1 = 6, X2 = 6, Y1 = 0, Y2 = 100, Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 12 },
				new Line { X1 = 0, X2 = 200, Y1 = 3, Y2 = 3, Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 6 },
				new Line { X1 = 194, X2 = 194, Y1 = 0, Y2 = 100, Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 12 },
				new Line { X1 = 200, X2 = 0, Y1 = 97, Y2 = 97, Stroke = new SolidColorBrush(Colors.Red), StrokeThickness = 6 },

				new Rectangle { Width = 108, Height = 44, Fill = new SolidColorBrush(Colors.Green.WithOpacity(.5)) }.Apply(e => Canvas.SetLeft(e, 112)),
				new Line { X1 = 118, X2 = 118, Y1 = 0, Y2 = 44, Stroke = new SolidColorBrush(Colors.Green), StrokeThickness = 12 },

				new Rectangle { Width = 46, Height = 12, Fill = new SolidColorBrush(Colors.Orange) }.Apply(e => Canvas.SetLeft(e, 174)),

				new Rectangle { Width = 108, Height = 54, Fill = new SolidColorBrush(Colors.Blue.WithOpacity(.5)) }.Apply(e => Canvas.SetLeft(e, 112)).Apply(e => Canvas.SetTop(e, 56)),
				new Line { X1 = 118, X2 = 118, Y1 = 56, Y2 = 110, Stroke = new SolidColorBrush(Colors.Blue), StrokeThickness = 12 },
				new Line { X1 = 112, X2 = 220, Y1 = 59, Y2 = 59, Stroke = new SolidColorBrush(Colors.Blue), StrokeThickness = 6 }
			}
		};

		var (actual, expectedImg) = await Render(sut, expected);

		// First we check some well known important points
		ImageAssert.HasColorAt(actual, 106, 1, Colors.Red);
		ImageAssert.HasColorAt(actual, 118, 1, Colors.Green);
		ImageAssert.DoesNotHaveColorAt(actual, 172, 1, Colors.Orange);
		ImageAssert.HasColorAt(actual, 176, 1, Colors.Orange);

		ImageAssert.HasColorAt(actual, 118, 42, Colors.Green);
		ImageAssert.DoesNotHaveColorAt(actual, 118, 46, Colors.Green);

		ImageAssert.DoesNotHaveColorAt(actual, 118, 54, Colors.Blue);
		ImageAssert.HasColorAt(actual, 118, 58, Colors.Blue);
		ImageAssert.HasColorAt(actual, 118, 100, Colors.Blue);
		ImageAssert.HasColorAt(actual, 118, 108, Colors.Blue);
		ImageAssert.DoesNotHaveColorAt(actual, 118, 112, Colors.Blue); // clipped by Clip property
		ImageAssert.HasColorAt(actual, 200, 58, Colors.Blue);
		ImageAssert.HasColorAt(actual, 218, 58, Colors.Blue);
		ImageAssert.DoesNotHaveColorAt(actual, 222, 58, Colors.Blue); // clipped by Clip property

		// Second we do a full comparison of something that should be identical but drawn using another (simpler ?) mechanism
		// Note: We give a pixel tolerance because of the antialiasing and scaling
		var expectedPixels = ExpectedPixels
			.At(0, 0)
			.Pixels(expectedImg)
			.WithPixelTolerance(2, 2, LocationToleranceKind.PerPixel)
			.WithColorTolerance(32);
		ImageAssert.HasPixels(actual, expectedPixels);
	}

	private async Task<(RawBitmap actual, RawBitmap expected)> Render(FrameworkElement sut, FrameworkElement expected)
	{
#if DEBUG
		await UITestHelper.Load(new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(),
				new ColumnDefinition()
			},
			Children =
			{
				sut.Apply(e => Grid.SetColumn(e, 0)),
				expected.Apply(e => Grid.SetColumn(e, 1))
			}
		});

		return (await UITestHelper.ScreenShot(sut), await UITestHelper.ScreenShot(expected));
#else
		await UITestHelper.Load(expected);
		var expectedImg = await UITestHelper.ScreenShot(expected);

		// We render the sut in second so the final screenshot is the one of the sut
		await UITestHelper.Load(sut);
		var actual = await UITestHelper.ScreenShot(sut);

		return (actual, expectedImg);
#endif
	}
#endif
}
