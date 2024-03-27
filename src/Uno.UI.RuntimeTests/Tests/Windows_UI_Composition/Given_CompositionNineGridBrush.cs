using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

#if HAS_UNO
using Uno.UI.Dispatching;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_CompositionNineGridBrush
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Source_Changes()
	{
#if HAS_UNO
		var expectedThreadId = -1;
		NativeDispatcher.Main.Enqueue(() => expectedThreadId = Environment.CurrentManagedThreadId, NativeDispatcherPriority.High);
		await TestServices.WindowHelper.WaitFor(() => expectedThreadId != -1);
#endif

		var compositor = ElementCompositionPreview.GetElementVisual(TestServices.WindowHelper.RootElement).Compositor;

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

		var surface = Microsoft.UI.Xaml.Media.LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/test_image_100_100.png"));

		bool loadCompleted = false;
		surface.LoadCompleted += async (s, o) =>
		{
#if HAS_UNO
			if (Environment.CurrentManagedThreadId != expectedThreadId)
			{
				Assert.Fail("LoadCompleted event is run on thread pool incorrectly");
			}
#endif

			if (o.Status == Microsoft.UI.Xaml.Media.LoadedImageSourceLoadStatus.Success)
			{
				var offlineBrush = compositor.CreateSurfaceBrush(surface);

				var offlineNineGridBrush = compositor.CreateNineGridBrush();
				offlineNineGridBrush.Source = offlineBrush;
				offlineNineGridBrush.SetInsets(35);

				offline.Background = new TestBrush(offlineNineGridBrush);

				var result = await Render(onlineSource, online, offline);
				await ImageAssert.AreEqualAsync(result.actual, result.expected);

				loadCompleted = true;
			}
			else
			{
				Assert.Fail();
			}
		};

		await TestServices.WindowHelper.WaitFor(() => loadCompleted);
	}

	private class TestBrush : Microsoft.UI.Xaml.Media.XamlCompositionBrushBase
	{
		private CompositionBrush Brush;

		public TestBrush(CompositionBrush brush) => Brush = brush;

		protected override void OnConnected() => CompositionBrush = Brush;
	}

	private async Task<(RawBitmap expected, RawBitmap actual)> Render(FrameworkElement source, FrameworkElement online, FrameworkElement offline)
	{
		await UITestHelper.Load(new StackPanel
		{
			Children =
			{
				source,
				// we need to put the children in borders to work around our limited implementation of RenderTargetBitmap
				// not taking the offsets of the Visuals into account. This way, the Child will not have any offset
				// relative to its parent (the border)
				new Border { Child = online },
				new Border { Child = offline }
			}
		});

		var onlineImg = await UITestHelper.ScreenShot(online);
		var offlineImg = await UITestHelper.ScreenShot(offline);

		return (onlineImg, offlineImg);
	}
}
