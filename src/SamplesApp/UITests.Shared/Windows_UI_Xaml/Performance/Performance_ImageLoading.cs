using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SamplesApp.UITests;
using Uno.Disposables;

#if __SKIA__
using SkiaSharp;
using Uno.WinUI.Graphics2DSK;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml.Performance
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Performance", IsManualTest = true, Description = "Make sure the numbers do not regress between different Uno versions. We have image caching enabled, so only test this after closing and reopening the app.")]
	public sealed partial class Performance_ImageLoading : Page
	{
		private static readonly List<string> _images =
		[
			"ms-appx:/Assets/LargeWisteria.jpg",
			"ms-appx:///Assets/BackArrow.png",
			"ms-appx:///Assets/test_image_125_125.png",
			"ms-appx:///Assets/test_image_150_100.png",
			"ms-appx:///Assets/Icons/search.png",
			"ms-appx:///Assets/square100.png",
			"ms-appx:///Assets/Icons/menu.png",
			"ms-appx:///Assets/Icons/star_full.png",
			"ms-appx:///Assets/ingredient1.png",
			"ms-appx:///Assets/ingredient2.png",
			"ms-appx:///Assets/ingredient3.png",
			"ms-appx:///Assets/ingredient4.png",
			"ms-appx:///Assets/ingredient5.png"
		];


		public Performance_ImageLoading()
		{
			var disposable = new CompositeDisposable();
			Unloaded += (s, e) => disposable.Dispose();

			var start = Stopwatch.GetTimestamp();

			int imagesLoaded = 0;

			var grid = new Grid();
			grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0f, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0f, GridUnitType.Star) });
			Content = grid;

			var sp = new StackPanel();
			grid.Children.Add(new Border
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Background = new SolidColorBrush(Colors.White),
				Child = sp,
				Width = 900,
				Height = 400
			});

			foreach (var image in _images)
			{
				var tb = new TextBlock { Foreground = new SolidColorBrush(Colors.Black) };
				sp.Children.Add(tb);
				grid.Children.Insert(0, new Image
				{
					Stretch = Stretch.Fill,
					Source = new BitmapImage { UriSource = new Uri(image) },
				}.Apply(img =>
				{
					RoutedEventHandler callback = (_, _) =>
					{
						tb.Text =
							$"Image {image} of dimensions {((BitmapImage)img.Source).PixelWidth}x{((BitmapImage)img.Source).PixelHeight} loaded in {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms from sample creation";
						imagesLoaded++;
					};
					img.ImageOpened += callback;
					disposable.Add(() => img.ImageOpened -= callback);
				}));
			}

#if __SKIA__
			grid.Children.Add(new InvalidatingSKCanvasElement());
			Loaded += (s, e) =>
			{
				Action onRenderedFrame = default;
				onRenderedFrame = () =>
				{
					if (imagesLoaded == _images.Count)
					{
						sp.Children.Add(new TextBlock
						{
							Foreground = new SolidColorBrush(Colors.Black),
							Text = $"Rendered first frame after all images loaded in {Stopwatch.GetElapsedTime(start).TotalMilliseconds}ms from sample creation"
						});
						XamlRoot.FrameRendered -= onRenderedFrame;
					}
				};
				XamlRoot.FrameRendered += onRenderedFrame;
				Unloaded += (_, _) => XamlRoot.FrameRendered -= onRenderedFrame;
			};
#endif
		}

#if __SKIA__
		private class InvalidatingSKCanvasElement : SKCanvasElement
		{
			protected override void RenderOverride(SKCanvas canvas, Size area) => Invalidate();
		}
#endif
	}
}
