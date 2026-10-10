#if __UNO__
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Xaml.Media;
using Windows.Storage;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
public class Given_Image_Release
{
	private const string AssetUri = "ms-appx:///Assets/my500x200.jpg";
	private const string SquareAssetUri = "ms-appx:///Assets/square100.png";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Unloaded_Then_Decoded_Image_Released()
	{
		// The source owns its decoded data only when the cache does not share it between sources.
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		FeatureConfiguration.Image.EnableBitmapImageCache = false;

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };

			await LoadAndWaitForOpened(image);
			Assert.IsTrue(bitmap.IsOpened, "Pre-condition: the bitmap is decoded while an Image shows it");

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(bitmap.IsOpened, "Nothing displays the bitmap once its only Image is unloaded, so the decoded data must be released");

			// Showing it again decodes it again.
			await LoadAndWaitForOpened(image);
			Assert.IsTrue(bitmap.IsOpened, "An Image that is loaded again must decode its source again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Replaced_Then_Previous_Decoded_Image_Released()
	{
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		FeatureConfiguration.Image.EnableBitmapImageCache = false;

		try
		{
			var first = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = first };

			await LoadAndWaitForOpened(image);
			Assert.IsTrue(first.IsOpened, "Pre-condition: the first bitmap is decoded while the Image shows it");

			var opened = TrackOpened(image);
			image.Source = new BitmapImage(new Uri(AssetUri));
			await opened.WaitAsync(TimeSpan.FromSeconds(10));

			Assert.IsFalse(first.IsOpened, "The replaced bitmap is shown by nothing, so its decoded data must be released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Requester_Cancelled_After_Decode_Then_Abandoned_Surface_Released()
	{
		// Without the cache the load belongs to this source alone. The decode runs off the UI thread and its result is applied
		// by a dispatched continuation; a source change that lands first (a user clicking through images quickly) must release
		// the surface that continuation would otherwise drop.
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		FeatureConfiguration.Image.EnableBitmapImageCache = false;

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var bitmap = new BitmapImage(server.Uri);
			var result = TrackOpen(bitmap);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

			var createdBefore = ImageData.CompositionSurfacesCreatedForTesting;
			var releasedBefore = ImageSource.ReleasedSurfacesForTesting;
			server.ReleaseResponses();

			// Holding the UI thread keeps the dispatched continuation from applying the result while the decode completes.
			var decoded = SpinWait.SpinUntil(() => ImageData.CompositionSurfacesCreatedForTesting > createdBefore, 5000);
			Assert.IsTrue(decoded, $"Pre-condition: the decode must complete (requests {server.RequestCount}, bodies sent {server.BodiesSent}, surfaces {ImageData.CompositionSurfacesCreatedForTesting - createdBefore}, opened {result.IsCompleted})");

			bitmap.UriSource = null;
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(ImageSource.ReleasedSurfacesForTesting > releasedBefore, "The decoded surface nobody will show must be released");
			Assert.IsFalse(result.IsCompleted, "The cancelled BitmapImage should raise neither ImageOpened nor ImageFailed");
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_UriSource_Changed_Then_Previous_Decoded_Image_Released()
	{
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		FeatureConfiguration.Image.EnableBitmapImageCache = false;

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };

			await LoadAndWaitForOpened(image);
			Assert.IsTrue(bitmap.IsOpened, "Pre-condition: the bitmap is decoded while the Image shows it");

			var releasedBefore = ImageSource.ReleasedSurfacesForTesting;
			var opened = TrackOpened(image);
			bitmap.UriSource = new Uri(SquareAssetUri);
			await opened.WaitAsync(TimeSpan.FromSeconds(10));

			Assert.IsTrue(ImageSource.ReleasedSurfacesForTesting > releasedBefore, "The surface decoded for the previous UriSource is shown by nothing, so it must be released");
			Assert.IsTrue(bitmap.IsOpened, "The bitmap must open its new UriSource");
			Assert.AreEqual(100, bitmap.PixelWidth);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Changed_While_Unloaded_Then_Decoded_On_Load()
	{
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		FeatureConfiguration.Image.EnableBitmapImageCache = false;

		try
		{
			var image = new Image { Width = 100, Height = 40, Source = new BitmapImage(new Uri(AssetUri)) };
			await LoadAndWaitForOpened(image);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();

			// A recycled container gets its new source while it is off the tree: nothing decodes until it is loaded again.
			var bitmap = new BitmapImage(new Uri(SquareAssetUri));
			var createdBefore = ImageData.CompositionSurfacesCreatedForTesting;
			image.Source = bitmap;
			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(bitmap.IsOpened, "An unloaded Image must not decode its source");
			Assert.AreEqual(createdBefore, ImageData.CompositionSurfacesCreatedForTesting, "An unloaded Image must not decode its source");

			await LoadAndWaitForOpened(image);
			Assert.IsTrue(bitmap.IsOpened, "The Image decodes its source once it is loaded");
			Assert.IsTrue(ImageData.CompositionSurfacesCreatedForTesting > createdBefore, "The Image decodes its source once it is loaded");
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Stream_Load_Superseded_After_Decode_Then_Abandoned_Surface_Released()
	{
		// A stream load is never cached, so its result is the source's alone. The decode runs off the UI thread and its
		// result is applied by a dispatched continuation; a second SetSourceAsync that lands first must release the
		// surface of the first, which must not raise ImageOpened for a stream the source no longer has.
		var square = await ReadAssetAsync(SquareAssetUri);
		var tall = await ReadAssetAsync("ms-appx:///Assets/test_image_100_150.png");

		try
		{
			var bitmap = new BitmapImage();
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			var openedCount = 0;
			var failedCount = 0;
			bitmap.ImageOpened += (_, _) => openedCount++;
			bitmap.ImageFailed += (_, _) => failedCount++;

			var createdBefore = ImageData.CompositionSurfacesCreatedForTesting;
			var releasedBefore = ImageSource.ReleasedSurfacesForTesting;
			var first = bitmap.SetSourceAsync(new MemoryStream(square));

			// Holding the UI thread keeps the dispatched continuation from applying the result while the decode completes.
			var decoded = SpinWait.SpinUntil(() => ImageData.CompositionSurfacesCreatedForTesting > createdBefore, 5000);
			Assert.IsTrue(decoded, "Pre-condition: the first decode must complete");

			var second = bitmap.SetSourceAsync(new MemoryStream(tall));
			await second.WaitAsync(TimeSpan.FromSeconds(10));
			await first.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(ImageSource.ReleasedSurfacesForTesting > releasedBefore, "The surface decoded for the superseded stream is shown by nothing, so it must be released");
			Assert.AreEqual(1, openedCount, "Only the stream the source still has may raise ImageOpened");
			Assert.AreEqual(0, failedCount, "The superseded load must not raise ImageFailed");
			Assert.AreEqual(150, bitmap.PixelHeight);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Cache_Disabled_Then_Cached_Surface_Not_Released()
	{
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		var uri = new Uri("ms-appx:///Assets/test_image_100_150.png");

		try
		{
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			// A cache-on requester fills the cache and a second one shares the entry: unloading both must not release
			// the shared surface, which is the cache's (an entry is only evicted when its load fails).
			FeatureConfiguration.Image.EnableBitmapImageCache = true;
			var first = new Image { Width = 100, Height = 40, Source = new BitmapImage(uri) };
			await AddAndWaitForOpened(panel, first);

			var createdBefore = ImageData.CompositionSurfacesCreatedForTesting;
			var releasedBefore = ImageSource.ReleasedSurfacesForTesting;
			var sharing = new Image { Width = 100, Height = 40, Source = new BitmapImage(uri) };
			await AddAndWaitForOpened(panel, sharing);
			Assert.AreEqual(createdBefore, ImageData.CompositionSurfacesCreatedForTesting, "A cache-on requester is served from the cache");

			panel.Children.Remove(sharing);
			panel.Children.Remove(first);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(releasedBefore, ImageSource.ReleasedSurfacesForTesting, "A shared surface is the cache's to release, not a requester's");

			// With the cache off a source decodes for itself even when an entry exists, and releases only its own surface.
			FeatureConfiguration.Image.EnableBitmapImageCache = false;
			var own = new Image { Width = 100, Height = 40, Source = new BitmapImage(uri) };
			await AddAndWaitForOpened(panel, own);
			Assert.IsTrue(ImageData.CompositionSurfacesCreatedForTesting > createdBefore, "A cache-off source must not be served from the cache");

			panel.Children.Remove(own);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(ImageSource.ReleasedSurfacesForTesting > releasedBefore, "A cache-off source owns its surface and releases it when unloaded");

			// The cached surface is intact: a later cache-on requester opens without decoding again.
			FeatureConfiguration.Image.EnableBitmapImageCache = true;
			createdBefore = ImageData.CompositionSurfacesCreatedForTesting;
			var cached = new BitmapImage(uri);
			await AddAndWaitForOpened(panel, new Image { Width = 100, Height = 40, Source = cached });
			Assert.AreEqual(createdBefore, ImageData.CompositionSurfacesCreatedForTesting, "The cached surface must still serve a later requester");
			Assert.AreEqual(150, cached.PixelHeight);
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	private static async Task LoadAndWaitForOpened(Image image)
	{
		var opened = TrackOpened(image);
		await UITestHelper.Load(image);
		await opened.WaitAsync(TimeSpan.FromSeconds(10));
	}

	private static async Task AddAndWaitForOpened(Panel panel, Image image)
	{
		var opened = TrackOpened(image);
		panel.Children.Add(image);
		await opened.WaitAsync(TimeSpan.FromSeconds(10));
	}

	private static async Task<byte[]> ReadAssetAsync(string uri)
	{
		var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));
		using var stream = await file.OpenStreamForReadAsync();
		using var memory = new MemoryStream();
		await stream.CopyToAsync(memory);
		return memory.ToArray();
	}

	private static Task TrackOpened(Image image)
	{
		var tcs = new TaskCompletionSource();
		image.ImageOpened += (_, _) => tcs.TrySetResult();
		image.ImageFailed += (_, e) => tcs.TrySetException(new Exception($"Failed to load the image: {e.ErrorMessage}"));
		return tcs.Task;
	}

	private static Task<bool> TrackOpen(BitmapImage image)
	{
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		image.ImageOpened += (_, _) => tcs.TrySetResult(true);
		image.ImageFailed += (_, _) => tcs.TrySetResult(false);
		return tcs.Task;
	}

	/// <summary>
	/// Minimal loopback HTTP server that sends the response headers immediately but holds the image bytes
	/// until <see cref="ReleaseResponses"/>, so a test can act while a download is deterministically in flight.
	/// </summary>
	private sealed class GatedImageServer : IDisposable
	{
		private readonly TcpListener _listener;
		private readonly byte[] _body;
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _requestCount;
		private int _bodiesSent;

		private GatedImageServer(TcpListener listener, byte[] body)
		{
			_listener = listener;
			_body = body;
			Uri = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/square100.png");
			// Off the UI thread: the test may hold the UI thread while a response is pending, and awaits started from it
			// would otherwise resume through its synchronization context.
			_ = Task.Run(AcceptLoopAsync);
		}

		public static async Task<GatedImageServer> StartAsync()
		{
			var body = await ReadAssetAsync(SquareAssetUri);
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			return new GatedImageServer(listener, body);
		}

		public Uri Uri { get; }

		public int RequestCount => Volatile.Read(ref _requestCount);

		public int BodiesSent => Volatile.Read(ref _bodiesSent);

		public void ReleaseResponses() => _release.TrySetResult();

		public void Dispose()
		{
			_release.TrySetResult();
			_listener.Stop();
		}

		private async Task AcceptLoopAsync()
		{
			try
			{
				while (true)
				{
					var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
					_ = HandleAsync(client);
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException)
			{
			}
		}

		private async Task HandleAsync(TcpClient client)
		{
			try
			{
				using var _ = client;
				using var stream = client.GetStream();

				var request = new byte[8192];
				var read = 0;
				while (!Encoding.ASCII.GetString(request, 0, read).Contains("\r\n\r\n"))
				{
					var count = await stream.ReadAsync(request, read, request.Length - read).ConfigureAwait(false);
					if (count == 0)
					{
						return;
					}

					read += count;
				}

				Interlocked.Increment(ref _requestCount);

				var headers = $"HTTP/1.1 200 OK\r\nContent-Type: image/png\r\nContent-Length: {_body.Length}\r\nConnection: close\r\n\r\n";
				await stream.WriteAsync(Encoding.ASCII.GetBytes(headers)).ConfigureAwait(false);
				await stream.FlushAsync().ConfigureAwait(false);

				await _release.Task.ConfigureAwait(false);

				await stream.WriteAsync(_body).ConfigureAwait(false);
				await stream.FlushAsync().ConfigureAwait(false);
				Interlocked.Increment(ref _bodiesSent);
			}
			catch (IOException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException)
			{
			}
		}
	}
}
#endif
