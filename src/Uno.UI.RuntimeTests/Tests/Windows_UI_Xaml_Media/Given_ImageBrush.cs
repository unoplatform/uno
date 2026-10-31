using System;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
using ImageBrush = Microsoft.UI.Xaml.Media.ImageBrush;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ImageBrush
	{
		[DataRow(Stretch.Fill, false)]
		[DataRow(Stretch.Fill, true)]
#if !__APPLE_UIKIT__
		// See https://github.com/unoplatform/uno/issues/9080
		[DataRow(Stretch.UniformToFill, false)]
#endif
		[DataRow(Stretch.UniformToFill, true)]
#if !__ANDROID__
		// Stretch.None is broken on Android.
		// See https://github.com/unoplatform/uno/pull/7238#issuecomment-937667565
		[DataRow(Stretch.None, false)]
		[DataRow(Stretch.None, true)]
#endif
#if !__APPLE_UIKIT__
		// See https://github.com/unoplatform/uno/issues/9080
		[DataRow(Stretch.Uniform, false)]
#endif
		[DataRow(Stretch.Uniform, true)]
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeUIKit)] // Flaky on UIKit - #9080
		public async Task When_Stretch(Stretch stretch, bool useRectangle)
		{
			const string Redish = "#FFEB1C24";
			const string Yellowish = "#FFFEF200";
			const string Greenish = "#FF0ED145";
			const string Transparent = "#00000000";

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var brush = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/colored-ellipse.jpg")),
				Stretch = stretch,
			};

			FrameworkElement SUT;
			if (useRectangle)
			{
				SUT = new Rectangle
				{
					Width = 100,
					Height = 100,
					StrokeThickness = 2,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
					Fill = brush,
				};
			}
			else
			{
				SUT = new Border
				{
					Width = 100,
					Height = 100,
					BorderThickness = new Thickness(2),
					BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
					Background = brush,
				};
			}
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			// Retry is required because the brush even is not always raised
			await TestHelper.RetryAssert(async () =>
			{
				float BorderOffset =
#if __APPLE_UIKIT__ || __SKIA__
					6;
#else
					3;
#endif
				float width = (float)SUT.Width, height = (float)SUT.Height;
				float centerX = width / 2, centerY = height / 2;
				var expectations = stretch switch
				{
					// All edges are red-ish
					Stretch.Fill => (Top: Redish, Bottom: Redish, Left: Redish, Right: Redish),
					// Top and bottom are red-ish. Left and right are yellow-ish
					Stretch.UniformToFill => (Top: Redish, Bottom: Redish, Left: Yellowish, Right: Yellowish),
					// Top and bottom are same as background. Left and right are red-ish
					Stretch.Uniform => (Top: Transparent, Bottom: Transparent, Left: Redish, Right: Redish),
					// Everything is green-ish
					Stretch.None => (Top: Greenish, Bottom: Greenish, Left: Greenish, Right: Greenish),

					_ => throw new ArgumentOutOfRangeException($"unexpected stretch: {stretch}"),
				};

				var bitmap = await UITestHelper.ScreenShot(SUT);

				ImageAssert.HasColorAt(bitmap, centerX, BorderOffset, expectations.Top, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, centerX, height - BorderOffset, expectations.Bottom, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, BorderOffset, centerY, expectations.Left, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, width - BorderOffset, centerY, expectations.Right, tolerance: 28);
			}, 10);
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaMobile)] // No sockets in the browser
		public async Task When_SvgImageSource_Assigned_While_Opening()
		{
			// Setting UriSource opens the source, so the brush subscribes while that open is still in flight. A slow
			// server keeps it in flight deterministically.
			const string svg = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect width='100' height='100' fill='red'/></svg>";
			using var server = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
			server.Start();
			var port = ((System.Net.IPEndPoint)server.LocalEndpoint).Port;
			var serving = ServeOnceAsync(server, svg);

			var svgImageSource = new SvgImageSource(new Uri($"http://127.0.0.1:{port}/red.svg"));
			var opened = false;
			svgImageSource.Opened += (_, _) => opened = true;

			var border = new Border { Width = 100, Height = 100, Background = new ImageBrush { ImageSource = svgImageSource } };
			await UITestHelper.Load(border);
			await serving;
			await WindowHelper.WaitFor(() => opened, message: "SvgImageSource.Opened was not raised.");
			await WindowHelper.WaitForIdle();

			var screenshot = await UITestHelper.ScreenShot(border);
			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2, screenshot.Height / 2, Microsoft.UI.Colors.Red, tolerance: 10);

			static async Task ServeOnceAsync(System.Net.Sockets.TcpListener server, string body)
			{
				using var client = await server.AcceptTcpClientAsync();
				using var stream = client.GetStream();
				var buffer = new byte[4096];
				_ = await stream.ReadAsync(buffer);
				await Task.Delay(200);

				var payload = System.Text.Encoding.UTF8.GetBytes(body);
				var header = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: image/svg+xml\r\nContent-Length: {payload.Length}\r\nConnection: close\r\n\r\n");
				try
				{
					await stream.WriteAsync(header);
					await stream.WriteAsync(payload);
				}
				catch (System.IO.IOException)
				{
					// The client gave up on the request; the assertions report the outcome.
				}
			}
		}

#if __SKIA__
		[TestMethod]
		[Ignore("Flaky. https://github.com/unoplatform/uno/issues/9080")]
		public async Task When_DownSampling()
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = new ImageBrush
				{
					ImageSource = new Uri("ms-appx:/Assets/LargeWisteria.jpg")
				}
			};

			var image = new Image
			{
				Width = 100,
				Height = 100,
				Source = new Uri("ms-appx:/Assets/ResizedLargeWisteria.png")
			};

			await UITestHelper.Load(new StackPanel
			{
				Children =
				{
					border,
					image
				}
			});
			await Task.Delay(1000); // wait for idle might not be enough here

			var bitmap = await UITestHelper.ScreenShot(border);
			var bitmap2 = await UITestHelper.ScreenShot(image);
			await ImageAssert.AreEqualAsync(bitmap, bitmap2);
		}
#endif
	}
}
