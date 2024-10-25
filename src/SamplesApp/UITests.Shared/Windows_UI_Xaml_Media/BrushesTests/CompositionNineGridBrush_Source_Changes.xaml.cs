using System;
using Windows.UI.Composition;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace UITests.Windows_UI_Xaml_Media.BrushesTests
{
	[Sample("Brushes")]
	public sealed partial class CompositionNineGridBrush_Source_Changes : Page
	{
		public CompositionNineGridBrush_Source_Changes()
		{
			var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

			var onlineSource = new Image
			{
				Width = 100,
				Height = 100,
				Stretch = Stretch.UniformToFill,
				Source = new BitmapImage(new Uri("ms-appx:///Assets/test_image_100_100.png"))
			};

			var online = new Border
			{
				Width = 200,
				Height = 200
			};

			var offline = new Grid
			{
				Width = 200,
				Height = 200
			};

			var visualSurface = compositor.CreateVisualSurface();
			visualSurface.SourceVisual = ElementCompositionPreview.GetElementVisual(onlineSource);
			visualSurface.SourceSize = new(200, 200);

			var onlineBrush = compositor.CreateSurfaceBrush(visualSurface);
			var onlineNineGridBrush = compositor.CreateNineGridBrush();
			onlineNineGridBrush.Source = onlineBrush;
			onlineNineGridBrush.SetInsets(35);

			online.Background = new TestBrush(onlineNineGridBrush);

			var surface = Windows.UI.Xaml.Media.LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/test_image_100_100.png"));

			surface.LoadCompleted += new Windows.Foundation.TypedEventHandler<Windows.UI.Xaml.Media.LoadedImageSurface, LoadedImageSourceLoadCompletedEventArgs>((s, o) =>
			{
				if (o.Status == Windows.UI.Xaml.Media.LoadedImageSourceLoadStatus.Success)
				{
					var offlineBrush = compositor.CreateSurfaceBrush(surface);

					var offlineNineGridBrush = compositor.CreateNineGridBrush();
					offlineNineGridBrush.Source = offlineBrush;
					offlineNineGridBrush.SetInsets(35);

					offline.Background = new TestBrush(offlineNineGridBrush);

					this.Content = new StackPanel
					{
						Children =
						{
							onlineSource,
							// we need to put the children in borders to work around our limited implementation of RenderTargetBitmap
							// not taking the offsets of the Visuals into account. This way, the Child will not have any offset
							// relative to its parent (the border)
							new Border
							{
								Child = online
							},
							new Border
							{
								Child = offline
							}
						}
					};
				}
			});
		}

		private class TestBrush : Windows.UI.Xaml.Media.XamlCompositionBrushBase
		{
			private CompositionBrush Brush;

			public TestBrush(CompositionBrush brush) => Brush = brush;

			protected override void OnConnected() => CompositionBrush = Brush;
		}
	}
}
