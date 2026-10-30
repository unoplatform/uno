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
		var panel = new StackPanel();
		await UITestHelper.Load(panel, x => x.IsLoaded);

		try
		{
			var first = new BitmapImage(server.Uri);
			var second = new BitmapImage(server.Uri);
			var firstResult = TrackOpen(first);
			var secondResult = TrackOpen(second);

			// A loaded Image is what opens its source; both join the same cached download.
			panel.Children.Add(new Image { Width = 50, Height = 50, Source = first });
			panel.Children.Add(new Image { Width = 50, Height = 50, Source = second });

			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

			// Cancels the first requester while the shared download is in flight (what a recycled container does).
			first.UriSource = null;

			server.ReleaseResponses();

			Assert.IsTrue(await secondResult.WaitAsync(TimeSpan.FromSeconds(10)), "The second BitmapImage should open");
			Assert.AreEqual(100, second.PixelWidth);
			Assert.AreEqual(1, server.RequestCount, "Both sources should share one download");
			Assert.IsFalse(firstResult.IsCompleted, "The cancelled BitmapImage should raise neither ImageOpened nor ImageFailed");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25096")]
	public async Task When_Only_Requester_Cancelled_Then_Same_Uri_Reopened()
	{
		using var server = await GatedImageServer.StartAsync();
		var panel = new StackPanel();
		await UITestHelper.Load(panel, x => x.IsLoaded);

		try
		{
			var first = new BitmapImage(server.Uri);
			panel.Children.Add(new Image { Width = 50, Height = 50, Source = first });

			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

			first.UriSource = null;
			server.ReleaseResponses();

			var second = new BitmapImage(server.Uri);
			var secondResult = TrackOpen(second);
			panel.Children.Add(new Image { Width = 50, Height = 50, Source = second });

			Assert.IsTrue(await secondResult.WaitAsync(TimeSpan.FromSeconds(10)), "A later BitmapImage for the same Uri should open");
			Assert.AreEqual(100, second.PixelWidth);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25096")]
	public async Task When_Shared_Download_Failed_Then_Same_Uri_Reopened()
	{
		using var server = await GatedImageServer.StartAsync();
		server.FailNextRequest = true;
		server.ReleaseResponses();

		var panel = new StackPanel();
		await UITestHelper.Load(panel, x => x.IsLoaded);

		try
		{
			var first = new BitmapImage(server.Uri);
			var firstResult = TrackOpen(first);
			panel.Children.Add(new Image { Width = 50, Height = 50, Source = first });

			Assert.IsFalse(await firstResult.WaitAsync(TimeSpan.FromSeconds(10)), "The first BitmapImage should fail");

#if HAS_UNO
			// The entry is dropped by a continuation on the load, so poll rather than assume it ran before ImageFailed.
			await TestHelper.RetryAssert(
				async () => Assert.IsNull(await BitmapImage.GetCachedImageDataTaskForTesting(server.Uri, null, null), "The failed load should have been dropped from the cache"),
				count: 200);
#endif

			var second = new BitmapImage(server.Uri);
			var secondResult = TrackOpen(second);
			panel.Children.Add(new Image { Width = 50, Height = 50, Source = second });

			Assert.IsTrue(await secondResult.WaitAsync(TimeSpan.FromSeconds(10)), "A failed download must not be cached: the next BitmapImage should download again and open");
			Assert.AreEqual(100, second.PixelWidth);
			Assert.AreEqual(2, server.RequestCount, "The second BitmapImage should have downloaded again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static Task<bool> TrackOpen(BitmapImage image)
	{
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		image.ImageOpened += (_, _) => tcs.TrySetResult(true);
		image.ImageFailed += (_, _) => tcs.TrySetResult(false);
		return tcs.Task;
	}
}
