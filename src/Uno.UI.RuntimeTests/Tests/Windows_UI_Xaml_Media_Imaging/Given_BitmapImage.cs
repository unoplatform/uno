#nullable enable

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Storage;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging;

[TestClass]
[RunsOnUIThread]
public class Given_BitmapImage
{
#if HAS_UNO
	private const string AssetUri = "ms-appx:///Uno.UI.RuntimeTests/Assets/Transitive-ingredient01.png";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25098")]
	public async Task When_IgnoreImageCache_Then_Not_Added_To_Cache()
	{
		// Distinct decode widths give each load its own cache key, independent of other tests.
		const int ignoredWidth = 41;
		const int cachedWidth = 42;

		var uri = new Uri(AssetUri);
		var panel = new StackPanel();
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;

		try
		{
			FeatureConfiguration.Image.EnableBitmapImageCache = true;

			await UITestHelper.Load(panel, x => x.IsLoaded);

			await LoadAsync(panel, uri, ignoredWidth, BitmapCreateOptions.IgnoreImageCache);
			Assert.IsNull(await BitmapImage.GetCachedImageDataTaskForTesting(uri, ignoredWidth, null));

			await LoadAsync(panel, uri, cachedWidth, BitmapCreateOptions.None);
			var cachedTask = await BitmapImage.GetCachedImageDataTaskForTesting(uri, cachedWidth, null);
			Assert.IsNotNull(cachedTask);

			// Ignoring the cache must not replace an entry other consumers already share.
			await LoadAsync(panel, uri, cachedWidth, BitmapCreateOptions.IgnoreImageCache);
			Assert.AreSame(cachedTask, await BitmapImage.GetCachedImageDataTaskForTesting(uri, cachedWidth, null));
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	private static async Task LoadAsync(Panel panel, Uri uri, int decodePixelWidth, BitmapCreateOptions options)
	{
		var tcs = new TaskCompletionSource<bool>();
		var bitmapImage = new BitmapImage { CreateOptions = options, DecodePixelWidth = decodePixelWidth };
		bitmapImage.ImageOpened += (_, _) => tcs.TrySetResult(true);
		bitmapImage.ImageFailed += (_, e) => tcs.TrySetException(new Exception($"Failed to load {uri}: {e.ErrorMessage}"));
		bitmapImage.UriSource = uri;

		panel.Children.Add(new Image { Width = 50, Height = 50, Source = bitmapImage });

		await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
	}
#endif

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25096")]
	public async Task When_Shared_Download_And_First_Requester_Cancelled()
	{
		using var server = await GatedImageServer.StartAsync();

		var first = new BitmapImage(server.Uri);
		var second = new BitmapImage(server.Uri);
		var firstResult = TrackOpen(first);
		var secondResult = TrackOpen(second);

		// Subscribing an Image is what opens the source; both join the same cached download.
		var firstImage = new Image { Source = first };
		var secondImage = new Image { Source = second };

		await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

		// Cancels the first requester while the shared download is in flight (what a recycled container does).
		first.UriSource = null;

		server.ReleaseResponses();

		Assert.IsTrue(await secondResult.WaitAsync(TimeSpan.FromSeconds(10)), "The second BitmapImage should open");
		Assert.AreEqual(100, second.PixelWidth);
		Assert.AreEqual(1, server.RequestCount, "Both sources should share one download");
		Assert.IsFalse(firstResult.IsCompleted, "The cancelled BitmapImage should raise neither ImageOpened nor ImageFailed");

		GC.KeepAlive(firstImage);
		GC.KeepAlive(secondImage);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25096")]
	public async Task When_Only_Requester_Cancelled_Then_Same_Uri_Reopened()
	{
		using var server = await GatedImageServer.StartAsync();

		var first = new BitmapImage(server.Uri);
		var firstImage = new Image { Source = first };

		await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

		first.UriSource = null;
		server.ReleaseResponses();

		var second = new BitmapImage(server.Uri);
		var secondResult = TrackOpen(second);
		var secondImage = new Image { Source = second };

		Assert.IsTrue(await secondResult.WaitAsync(TimeSpan.FromSeconds(10)), "A later BitmapImage for the same Uri should open");
		Assert.AreEqual(100, second.PixelWidth);

		GC.KeepAlive(firstImage);
		GC.KeepAlive(secondImage);
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

		private GatedImageServer(TcpListener listener, byte[] body)
		{
			_listener = listener;
			_body = body;
			Uri = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/square100.png");
			_ = AcceptLoopAsync();
		}

		public static async Task<GatedImageServer> StartAsync()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/square100.png"));
			using var stream = await file.OpenStreamForReadAsync();
			using var memory = new MemoryStream();
			await stream.CopyToAsync(memory);

			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			return new GatedImageServer(listener, memory.ToArray());
		}

		public Uri Uri { get; }

		public int RequestCount => Volatile.Read(ref _requestCount);

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
					var client = await _listener.AcceptTcpClientAsync();
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
					var count = await stream.ReadAsync(request, read, request.Length - read);
					if (count == 0)
					{
						return;
					}

					read += count;
				}

				Interlocked.Increment(ref _requestCount);

				var headers = $"HTTP/1.1 200 OK\r\nContent-Type: image/png\r\nContent-Length: {_body.Length}\r\nConnection: close\r\n\r\n";
				await stream.WriteAsync(Encoding.ASCII.GetBytes(headers));
				await stream.FlushAsync();

				await _release.Task;

				await stream.WriteAsync(_body);
				await stream.FlushAsync();
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
