#if __UNO__
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Graphics.Imaging;
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
	private const string TallAssetUri = "ms-appx:///Assets/test_image_100_150.png";

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Unloaded_Then_Decoded_Image_Released()
	{
		// The source owns its decoded data only when the cache does not share it between sources.
		using var _ = DisableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };

			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);
			Assert.IsNotNull(surface?.Image, "Pre-condition: the bitmap is decoded while an Image shows it");

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(surface.Image, "Nothing displays the bitmap once its only Image is unloaded, so its decoded pixels must be released");
			Assert.IsFalse(bitmap.IsOpened, "The source must not keep data it released");

			// Showing it again decodes it again.
			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "An Image that is loaded again must decode its source again");
			Assert.IsNotNull(GetSurface(bitmap)?.Image);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Failed_Image_Reloaded_Then_Source_Not_Retried()
	{
		// A source that failed stays failed until it changes, as on WinUI: its only Image, unloaded and loaded again, is not
		// given a retry, so nothing is requested or raised again.
		using var _ = DisableBitmapImageCache();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			server.FailNextRequest = true;
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = new BitmapImage(server.Uri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			var bitmapFailed = 0;
			var imageFailed = 0;
			bitmap.ImageFailed += (_, _) => bitmapFailed++;
			image.ImageFailed += (_, _) => imageFailed++;

			panel.Children.Add(image);
			await WindowHelper.WaitFor(() => imageFailed == 1, 10000, "Pre-condition: the source fails");
			await WindowHelper.WaitForIdle();

			panel.Children.Remove(image);
			await WindowHelper.WaitForIdle();
			server.FailNextRequest = true;
			panel.Children.Add(image);
			await UITestHelper.WaitForLoaded(image, x => x.IsLoaded);
			await Task.Delay(1000);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, server.RequestCount, "The failed source must not be requested again");
			Assert.AreEqual(1, bitmapFailed, "BitmapImage.ImageFailed must not be raised again");
			Assert.AreEqual(1, imageFailed, "Image.ImageFailed must not be raised again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Decoded_Again_After_Release_Then_ImageOpened_Raised_Once()
	{
		// WinUI raises ImageOpened once per source; decoding the same source again after its release is not a new open.
		using var _ = DisableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			var bitmapOpened = 0;
			var imageOpened = 0;
			bitmap.ImageOpened += (_, _) => bitmapOpened++;
			image.ImageOpened += (_, _) => imageOpened++;

			await LoadAndWaitForOpened(image);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(bitmap.IsOpened, "Pre-condition: the decoded data is released while nothing displays it");

			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "Pre-condition: the bitmap is decoded again");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(1, bitmapOpened, "BitmapImage.ImageOpened is raised once per source");
			Assert.AreEqual(1, imageOpened, "Image.ImageOpened is raised once per source");

			// A new source is a new open.
			var opened = TrackOpened(image);
			bitmap.UriSource = new Uri(SquareAssetUri);
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.AreEqual(2, bitmapOpened, "A new UriSource raises BitmapImage.ImageOpened again");
			Assert.AreEqual(2, imageOpened, "A new UriSource raises Image.ImageOpened again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Moved_Before_Next_Layout_Pass_Then_Decoded_Image_Kept(bool useCache)
	{
		// Moving an Image to another parent unloads it at once and loads it again in the next layout pass: even with a limit
		// of 0, which releases what nothing displays at the end of that pass, it must keep its pixels rather than release
		// them and decode them again, whether its source owns its decode or shares it through the cache.
		using var _ = SetBitmapImageCache(useCache);
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var first = new StackPanel();
			var second = new StackPanel();
			await UITestHelper.Load(new StackPanel { Children = { first, second } }, x => x.IsLoaded);

			// With the cache on, a decode size of its own gives the image its own cache key.
			var (width, height) = useCache ? (89, 35) : (500, 200);
			var bitmap = useCache ? NewBitmap(AssetUri, width, height) : new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			var bitmapOpened = 0;
			var imageOpened = 0;
			bitmap.ImageOpened += (_, _) => bitmapOpened++;
			image.ImageOpened += (_, _) => imageOpened++;

			await AddAndWaitForOpened(first, image);
			await WindowHelper.WaitForIdle();
			var surface = GetSurface(bitmap);
			var decodes = decoder.Count(width, height);

			first.Children.Remove(image);
			second.Children.Add(image);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(image.IsLoaded);
			Assert.IsNotNull(surface.Image, "An Image that re-enters the tree within the pass keeps its decoded pixels");
			Assert.AreSame(surface, GetSurface(bitmap), "The source must deliver the data it kept");
			Assert.AreEqual(decodes, decoder.Count(width, height), "Nothing must be decoded again");
			Assert.AreEqual(1, bitmapOpened);
			Assert.AreEqual(1, imageOpened);

			second.Children.Remove(image);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(surface.Image, "Once the Image stays away, its pixels are released at the end of the pass");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Cached_Image_Unloaded_Then_Shown_Again_Without_Reopen()
	{
		// A source keeps the decode it shares through the bitmap cache while nothing displays it, and the decode waits like
		// one the source owns: an Image that comes back within the delay is shown again at once, without a new open.
		using var _ = EnableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));
		using var decoder = TrackingDecoder.Install();

		try
		{
			var bitmap = new BitmapImage(new Uri(TallAssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			var bitmapOpened = 0;
			var imageOpened = 0;
			bitmap.ImageOpened += (_, _) => bitmapOpened++;
			image.ImageOpened += (_, _) => imageOpened++;

			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);
			var decodes = decoder.Count(100, 150);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(bitmap.IsOpened, "A source keeps the data it shares through the cache");
			Assert.IsNotNull(surface.Image, "A shared decode keeps its pixels within the release delay");

			await UITestHelper.Load(image);
			Assert.AreSame(surface, GetSurface(bitmap));
			Assert.AreEqual(decodes, decoder.Count(100, 150), "Nothing must be decoded again");
			Assert.AreEqual(1, bitmapOpened, "The source must not be opened again");
			Assert.AreEqual(1, imageOpened);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Replaced_Then_Previous_Decoded_Image_Released()
	{
		using var _ = DisableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			var first = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = first };

			await LoadAndWaitForOpened(image);
			var surface = GetSurface(first);

			var opened = TrackOpened(image);
			var second = new BitmapImage(new Uri(SquareAssetUri));
			image.Source = second;
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsNull(surface.Image, "The replaced bitmap is shown by nothing, so its decoded pixels must be released");
			Assert.IsNotNull(GetSurface(second)?.Image);
		}
		finally
		{
			WindowHelper.WindowContent = null;
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
		using var _ = DisableBitmapImageCache();
		using var decoder = TrackingDecoder.Install();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var bitmap = new BitmapImage(server.Uri);
			var result = TrackOpen(bitmap);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

			server.ReleaseResponses();

			// Holding the UI thread keeps the dispatched continuation from applying the result while the decode completes.
			var decoded = SpinWait.SpinUntil(() => decoder.Count(100, 100) > 0, 5000);
			Assert.IsTrue(decoded, $"Pre-condition: the decode must complete (requests {server.RequestCount}, bodies sent {server.BodiesSent}, opened {result.IsCompleted})");

			bitmap.UriSource = null;
			await WindowHelper.WaitForIdle();

			var abandoned = decoder.Decoded(100, 100).Single();
			await TestHelper.RetryAssert(() => Assert.IsTrue(abandoned.IsReleased, "The decoded surface nobody will show must be released"));
			Assert.IsFalse(result.IsCompleted, "The cancelled BitmapImage should raise neither ImageOpened nor ImageFailed");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_UriSource_Changed_Then_Previous_Decoded_Image_Released()
	{
		using var _ = DisableBitmapImageCache();

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };

			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);

			var opened = TrackOpened(image);
			bitmap.UriSource = new Uri(SquareAssetUri);
			Assert.IsNotNull(surface.Image, "The Image keeps showing the previous image until the new one is decoded");

			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsNull(surface.Image, "The surface decoded for the previous UriSource is shown by nothing once the new one is, so it must be released");
			Assert.IsTrue(bitmap.IsOpened, "The bitmap must open its new UriSource");
			Assert.AreEqual(100, bitmap.PixelWidth);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Changed_While_Unloaded_Then_Decoded_On_Load()
	{
		using var _ = DisableBitmapImageCache();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var image = new Image { Width = 100, Height = 40, Source = new BitmapImage(new Uri(AssetUri)) };
			await LoadAndWaitForOpened(image);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();

			// A recycled container gets its new source while it is off the tree: nothing decodes until it is loaded again.
			var bitmap = new BitmapImage(new Uri(SquareAssetUri));
			image.Source = bitmap;
			await Task.Delay(200);
			await WindowHelper.WaitForIdle();

			Assert.IsFalse(bitmap.IsOpened, "An unloaded Image must not decode its source");
			Assert.AreEqual(0, decoder.Count(100, 100), "An unloaded Image must not decode its source");

			await LoadAndWaitForOpened(image);
			Assert.IsTrue(bitmap.IsOpened, "The Image decodes its source once it is loaded");
			Assert.AreEqual(1, decoder.Count(100, 100), "The Image decodes its source once it is loaded");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	public enum SharedSource
	{
		Bitmap,
		Svg,
		CachedUri,
	}

	[TestMethod]
	[DataRow(SharedSource.Bitmap, DisplayName = "Bitmap")]
	[DataRow(SharedSource.Svg, DisplayName = "Svg")]
	[DataRow(SharedSource.CachedUri, DisplayName = "CachedUri")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25596")]
	public async Task When_Shared_Source_Outlives_Unloaded_Images_Then_Images_Collected(SharedSource kind)
	{
		// An icon kept in a static field or an app resource outlives the Images that show it: it must not keep them alive,
		// with everything they reference, once they left the tree. Two share it, as icons are: a source with a single owner
		// keeps that owner as its inheritance context parent (DependencyObject._associatedParent).
		using var cache = kind == SharedSource.CachedUri ? EnableBitmapImageCache() : null;
		var source = await CreateSharedSourceAsync(kind);

		try
		{
			var images = await ShowThenUnloadAsync(source);

			foreach (var image in images)
			{
				Assert.IsTrue(await TestHelper.TryWaitUntilCollected(image), "An unloaded Image must not be kept alive by the source it showed");
			}

			GC.KeepAlive(source);
		}
		finally
		{
			WindowHelper.WindowContent = null;
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
		var tall = await ReadAssetAsync(TallAssetUri);
		using var decoder = TrackingDecoder.Install();

		try
		{
			var bitmap = new BitmapImage();
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			var openedCount = 0;
			var failedCount = 0;
			bitmap.ImageOpened += (_, _) => openedCount++;
			bitmap.ImageFailed += (_, _) => failedCount++;

			// SetSourceAsync reads the pixel size with a decode of its own, on the calling thread, then decodes the stream
			// for the source off the UI thread.
			var first = bitmap.SetSourceAsync(new MemoryStream(square));

			// Holding the UI thread keeps the dispatched continuation from applying the result while the decode completes.
			var decoded = SpinWait.SpinUntil(() => decoder.Count(100, 100) >= 2, 5000);
			Assert.IsTrue(decoded, "Pre-condition: the first decode must complete");

			var second = bitmap.SetSourceAsync(new MemoryStream(tall));
			await second.WaitAsync(TimeSpan.FromSeconds(10));
			await first.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(decoder.Decoded(100, 100).All(d => d.IsReleased), "The surface decoded for the superseded stream is shown by nothing, so it must be released");
			Assert.IsTrue(decoder.Decoded(100, 150).Any(d => !d.IsReleased), "The surface of the stream the source has must be kept");
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
	public async Task When_Stream_Disposed_After_SetSourceAsync_Then_Decoded_Again_On_Reload()
	{
		// The source clones the stream it is given, so decoding it again after a release does not need the app's stream.
		var square = await ReadAssetAsync(SquareAssetUri);
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			var bitmap = new BitmapImage();
			var stream = new MemoryStream(square);
			await bitmap.SetSourceAsync(stream);
			stream.Dispose();

			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(surface.Image, "Pre-condition: the decoded pixels are released while nothing displays them");

			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The stream must be decoded again once an Image shows it");
			Assert.IsNotNull(GetSurface(bitmap)?.Image);
			Assert.AreEqual(100, bitmap.PixelWidth);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Cache_Disabled_Then_Cached_Surface_Not_Released()
	{
		var cacheWasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			// A cache-on requester fills the cache and a second one shares the entry: unloading the second must not release
			// the shared surface, which the first still displays.
			FeatureConfiguration.Image.EnableBitmapImageCache = true;
			// A decode size of its own gives this test its own cache key: an ImageBrush another test left keeps its decode.
			var firstBitmap = NewBitmap(TallAssetUri, 51, 77);
			var first = new Image { Width = 100, Height = 40, Source = firstBitmap };
			await AddAndWaitForOpened(panel, first);
			var cachedSurface = GetSurface(firstBitmap);

			var decodes = decoder.Count(51, 77);
			var sharingBitmap = NewBitmap(TallAssetUri, 51, 77);
			var sharing = new Image { Width = 100, Height = 40, Source = sharingBitmap };
			await AddAndWaitForOpened(panel, sharing);
			Assert.AreEqual(decodes, decoder.Count(51, 77), "A cache-on requester is served from the cache");
			Assert.AreSame(cachedSurface, GetSurface(sharingBitmap));

			panel.Children.Remove(sharing);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(cachedSurface.Image, "A shared surface another Image displays must not be released");

			// With the cache off a source decodes for itself even when an entry exists, and releases only its own surface.
			FeatureConfiguration.Image.EnableBitmapImageCache = false;
			var ownBitmap = NewBitmap(TallAssetUri, 51, 77);
			var own = new Image { Width = 100, Height = 40, Source = ownBitmap };
			await AddAndWaitForOpened(panel, own);
			var ownSurface = GetSurface(ownBitmap);
			Assert.AreNotSame(cachedSurface, ownSurface, "A cache-off source must not be served from the cache");
			Assert.AreEqual(decodes + 1, decoder.Count(51, 77), "A cache-off source decodes for itself");

			panel.Children.Remove(own);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(ownSurface.Image, "A cache-off source owns its surface and releases it when unloaded");
			Assert.IsNotNull(cachedSurface.Image, "Releasing an owned surface must leave the shared one alone");

			// The shared surface is intact: a later cache-on requester opens without decoding again.
			FeatureConfiguration.Image.EnableBitmapImageCache = true;
			var cached = NewBitmap(TallAssetUri, 51, 77);
			var later = new Image { Width = 100, Height = 40, Source = cached };
			await AddAndWaitForOpened(panel, later);
			Assert.AreEqual(decodes + 1, decoder.Count(51, 77), "The cached surface must still serve a later requester");
			Assert.AreSame(cachedSurface, GetSurface(cached));
			Assert.AreEqual(77, cached.PixelHeight);

			panel.Children.Remove(later);
			panel.Children.Remove(first);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(cachedSurface.Image, "A shared surface nothing displays is released like an owned one");
		}
		finally
		{
			WindowHelper.WindowContent = null;
			FeatureConfiguration.Image.EnableBitmapImageCache = cacheWasEnabled;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Shared_Decode_Undisplayed_Then_Released()
	{
		// Two sources share one decode: it is kept while either displays it and released once neither does, and both then
		// open again from a single new decode, without raising ImageOpened again.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			// A decode size of its own gives this test its own cache key.
			var firstBitmap = NewBitmap(TallAssetUri, 43, 61);
			var secondBitmap = NewBitmap(TallAssetUri, 43, 61);
			var first = new Image { Width = 100, Height = 40, Source = firstBitmap };
			var second = new Image { Width = 100, Height = 40, Source = secondBitmap };
			var opened = 0;
			firstBitmap.ImageOpened += (_, _) => opened++;
			secondBitmap.ImageOpened += (_, _) => opened++;

			await AddAndWaitForOpened(panel, first);
			await AddAndWaitForOpened(panel, second);
			var surface = GetSurface(firstBitmap);
			Assert.AreSame(surface, GetSurface(secondBitmap), "Pre-condition: both sources share one decode");
			Assert.AreEqual(1, decoder.Count(43, 61));

			panel.Children.Remove(first);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(surface.Image, "A shared decode another Image displays must be kept");

			panel.Children.Remove(second);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(surface.Image, "A shared decode nothing displays must be released like an owned one");
			Assert.IsFalse(firstBitmap.IsOpened, "A source must not keep a shared decode that was released");
			Assert.IsFalse(secondBitmap.IsOpened, "A source must not keep a shared decode that was released");

			panel.Children.Add(first);
			await WindowHelper.WaitFor(() => firstBitmap.IsOpened, 10000, "A source whose shared decode was released opens again");
			panel.Children.Add(second);
			await WindowHelper.WaitFor(() => secondBitmap.IsOpened, 10000, "A source whose shared decode was released opens again");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(2, decoder.Count(43, 61), "Both sources share the new decode");
			Assert.AreSame(GetSurface(firstBitmap), GetSurface(secondBitmap));
			Assert.IsNotNull(GetSurface(firstBitmap).Image);
			Assert.AreEqual(2, opened, "Decoding the same sources again raises no ImageOpened");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Shared_Decode_Undisplayed_Then_Counted_With_Owned_Ones()
	{
		// A shared decode nothing displays waits in the same list as the decodes sources own: let go first, it is the one
		// released once the waiting images exceed the limit.
		using var _ = EnableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var sharedBitmap = NewBitmap(TallAssetUri, 47, 67);
			var ownedBitmaps = new[]
			{
				new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache, UriSource = new Uri(AssetUri) },
				new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache, UriSource = new Uri(AssetUri) },
			};
			var bitmaps = new[] { sharedBitmap, ownedBitmaps[0], ownedBitmaps[1] };
			var images = bitmaps.Select(b => new Image { Width = 100, Height = 40, Source = b }).ToArray();
			foreach (var image in images)
			{
				await AddAndWaitForOpened(panel, image);
			}

			var surfaces = bitmaps.Select(GetSurface).ToArray();

			// Measured in the same turn as the removals, so no texture is drawn in between and the sizes are those that wait.
			FeatureConfiguration.Image.MaxUndisplayedImageBytes = surfaces.Sum(s => s.DecodedByteCount) - 1;
			foreach (var image in images)
			{
				panel.Children.Remove(image);
			}

			await WindowHelper.WaitForIdle();

			Assert.IsNull(surfaces[0].Image, "The shared decode waits the longest, so it is released first");
			Assert.IsNotNull(surfaces[1].Image, "Images within the limit keep their pixels");
			Assert.IsNotNull(surfaces[2].Image, "Images within the limit keep their pixels");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Shared_Load_Requesters_All_Cancelled_Then_Result_Released()
	{
		// The shared download keeps running when its only requester is cancelled, in case another one comes; the decode it
		// produces then waits like any image nothing displays, and is released.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var bitmap = new BitmapImage(server.Uri);
			var result = TrackOpen(bitmap);
			await UITestHelper.Load(new Image { Width = 100, Height = 40, Source = bitmap });
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

			bitmap.UriSource = null;
			server.ReleaseResponses();

			await WindowHelper.WaitFor(() => decoder.Count(100, 100) == 1, 5000, "Pre-condition: the shared load must still decode");
			var abandoned = decoder.Decoded(100, 100).Single();
			await TestHelper.RetryAssert(() => Assert.IsTrue(abandoned.IsReleased, "The decode nothing uses must be released"));
			Assert.IsFalse(result.IsCompleted, "The cancelled BitmapImage should raise neither ImageOpened nor ImageFailed");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Source_Collected_While_Subscribed_Then_Shared_Decode_Not_Kept()
	{
		// An ImageBrush keeps its subscription for its lifetime, so a source can be collected while it still counts as
		// showing its shared decode: the cache must not keep that decode for good, as WinUI's store does not own its
		// ImageCache.
		using var _ = EnableBitmapImageCache();
		var uri = new Uri(TallAssetUri);

		var collected = await OpenThroughUnusedBrush(uri, 53, 71);
		Assert.IsTrue(await IsCached(uri, 53, 71), "Pre-condition: the brush shares the decode");

		Assert.IsTrue(await TestHelper.TryWaitUntilCollected(collected), "Pre-condition: the brush's source is collected");
		Assert.IsFalse(await IsCached(uri, 53, 71), "The cache must not keep the decode of a collected source");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Source_Collected_While_Sharing_With_Live_One_Then_Decode_Still_Released()
	{
		// A source collected while still subscribed never ends its use of a decode it shared with a live source: that use
		// must not keep the decode once the live source lets it go, nor hide the live source's use before then.
		using var cache = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();
		var uri = new Uri(TallAssetUri);

		try
		{
			var collected = await OpenThroughUnusedBrush(uri, 57, 73);
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);
			await AddAndWaitForOpened(panel, new Image { Width = 100, Height = 40, Source = NewBitmap(TallAssetUri, 57, 73) });
			Assert.AreEqual(1, decoder.Count(57, 73), "Pre-condition: both sources share one decode");

			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(collected), "Pre-condition: the brush's source is collected");

			// Whenever the collected source's use ends, the live one still keeps the decode.
			await AddAndWaitForOpened(panel, new Image { Width = 100, Height = 40, Source = NewBitmap(TallAssetUri, 57, 73) });
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(decoder.Decoded(57, 73).Single().IsReleased, "The decode must be kept while a live source shows it");

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			await TestHelper.RetryAssert(() => Assert.IsTrue(decoder.Decoded(57, 73).Single().IsReleased, "The decode must be released once the live source lets it go"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Last_Sharing_Source_Collected_While_Other_Idles_Then_Decode_Released()
	{
		// A source kept alive after its Image left still has the shared decode, but nothing displays it. Once the brush's
		// source that still used it is collected, nothing else changes the decode's uses, and it must still be released after
		// the delay.
		var releaseDelay = TimeSpan.FromMilliseconds(300);
		using var cache = EnableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(releaseDelay);
		using var decoder = TrackingDecoder.Install();

		try
		{
			var collected = await OpenThroughUnusedBrush(new Uri(TallAssetUri), 61, 67);
			var idle = NewBitmap(TallAssetUri, 61, 67);
			await LoadAndWaitForOpened(new Image { Width = 100, Height = 40, Source = idle });
			Assert.AreEqual(1, decoder.Count(61, 67), "Pre-condition: both sources share one decode");

			WindowHelper.WindowContent = null;
			await Task.Delay(releaseDelay * 2);
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(decoder.Decoded(61, 67).Single().IsReleased, "Pre-condition: the brush's source still uses the decode");

			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(collected), "Pre-condition: the brush's source is collected");
			await WindowHelper.WaitFor(() => decoder.Decoded(61, 67).Single().IsReleased, 10000, "The decode nothing displays must be released once its last user is collected");
			GC.KeepAlive(idle);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Shared_Decode_Released_Then_Decoded_Again_Without_Download()
	{
		// The cache keeps the encoded bytes of a downloaded image, so decoding it again after its release downloads nothing.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var bytes = SetMaxBitmapImageCacheBytes(long.MaxValue);
		using var decoder = TrackingDecoder.Install();

		try
		{
			using var served = await ServedImage.StartAsync();
			var bitmap = new BitmapImage(served.Uri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsNull(surface.Image, "Pre-condition: the shared decode is released once nothing displays it");

			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The bitmap must be decoded again once an Image shows it");
			Assert.AreEqual(2, decoder.Count(100, 100), "Pre-condition: the bitmap is decoded again");
			Assert.AreEqual(1, served.RequestCount, "Decoding again must use the encoded bytes the cache kept");

			// A source that ignores the cache downloads for itself.
			var ignoring = new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache, UriSource = served.Uri };
			await LoadAndWaitForOpened(new Image { Width = 100, Height = 40, Source = ignoring });
			Assert.AreEqual(2, served.RequestCount, "IgnoreImageCache must not use the encoded bytes the cache kept");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	[DataRow("Cache-Control: no-store", "200 OK", 2)]
	[DataRow("Cache-Control: no-cache", "200 OK", 2)]
	[DataRow("Cache-Control: max-age=0", "200 OK", 2)]
	[DataRow("Pragma: no-cache", "200 OK", 2)]
	// Uno opens an image body whatever the status (WinUI's download likely fails the bind): the row checks only that it is not kept.
	[DataRow("", "404 Not Found", 2)]
	[DataRow("", "200 OK", 1)]
	public async Task When_Response_Not_Reusable_Then_Downloaded_Again(string header, string status, int expectedRequests)
	{
		// The cache keeps only the bytes of a successful response that may be reused without asking the server again, so
		// decoding another one again after its release downloads it again. Otherwise the kept bytes are used.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var bytes = SetMaxBitmapImageCacheBytes(long.MaxValue);
		using var served = await ServedImage.StartAsync(header, status);

		try
		{
			var bitmap = new BitmapImage(served.Uri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(bitmap.IsOpened, "Pre-condition: the shared decode is released once nothing displays it");

			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The bitmap must be decoded again once an Image shows it");
			Assert.AreEqual(expectedRequests, served.RequestCount, $"Requests for a '{status}' response with '{header}'");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Response_Redirected_Then_Downloaded_Again()
	{
		// Reuse is judged from the final response, while the bytes would be kept under the requested uri: the bytes of a
		// redirected response are not kept.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var bytes = SetMaxBitmapImageCacheBytes(long.MaxValue);
		using var server = await GatedImageServer.StartAsync();
		server.RedirectRequests = true;
		server.ReleaseResponses();

		try
		{
			var bitmap = new BitmapImage(server.Uri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			Assert.AreEqual(2, server.RequestCount, "Pre-condition: the image is redirected");

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(bitmap.IsOpened, "Pre-condition: the shared decode is released once nothing displays it");

			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The bitmap must be decoded again once an Image shows it");
			Assert.AreEqual(4, server.RequestCount, "A redirected response must be downloaded again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Encoded_Cache_Lowered_Or_Disabled_Then_Kept_Bytes_Dropped(bool disable)
	{
		// Lowering the bound, or turning the cache off, drops the bytes kept so far, so the next decode downloads again.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var bytes = SetMaxBitmapImageCacheBytes(long.MaxValue);
		using var served = await ServedImage.StartAsync();

		try
		{
			var bitmap = new BitmapImage(served.Uri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(bitmap.IsOpened, "Pre-condition: the shared decode is released once nothing displays it");

			if (disable)
			{
				FeatureConfiguration.Image.EnableBitmapImageCache = false;
				FeatureConfiguration.Image.EnableBitmapImageCache = true;
			}
			else
			{
				FeatureConfiguration.Image.MaxBitmapImageCacheBytes = 0;
				FeatureConfiguration.Image.MaxBitmapImageCacheBytes = long.MaxValue;
			}

			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The bitmap must be decoded again once an Image shows it");
			Assert.AreEqual(2, served.RequestCount, "The bytes kept before must have been dropped");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Encoded_Bytes_Unused_Past_Expiry_Then_Downloaded_Again()
	{
		// Bytes nothing used for a while are dropped, as the bitmap cache this replaces dropped entries unused for 5 minutes.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var bytes = SetMaxBitmapImageCacheBytes(long.MaxValue);
		using var expiry = SetEncodedIdleExpiry(TimeSpan.FromMilliseconds(200));
		using var served = await ServedImage.StartAsync();

		try
		{
			var bitmap = new BitmapImage(served.Uri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(bitmap.IsOpened, "Pre-condition: the shared decode is released once nothing displays it");
			await Task.Delay(500);

			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The bitmap must be decoded again once an Image shows it");
			Assert.AreEqual(2, served.RequestCount, "Bytes unused past the expiry must have been dropped");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Encoded_Cache_Full_Then_Least_Recently_Used_Dropped()
	{
		// The cache has room for the encoded bytes of one of the two images: the one loaded first is dropped, so decoding it
		// again downloads it again, while the other is decoded again from its bytes.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var first = await ServedImage.StartAsync();
		using var second = await ServedImage.StartAsync();
		using var bytes = SetMaxBitmapImageCacheBytes(first.Length * 3 / 2);

		try
		{
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);
			var firstBitmap = new BitmapImage(first.Uri);
			var secondBitmap = new BitmapImage(second.Uri);
			var firstImage = new Image { Width = 100, Height = 40, Source = firstBitmap };
			var secondImage = new Image { Width = 100, Height = 40, Source = secondBitmap };
			await AddAndWaitForOpened(panel, firstImage);
			await AddAndWaitForOpened(panel, secondImage);

			panel.Children.Clear();
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(firstBitmap.IsOpened, "Pre-condition: the shared decodes are released once nothing displays them");
			Assert.IsFalse(secondBitmap.IsOpened, "Pre-condition: the shared decodes are released once nothing displays them");

			// The second first, so its bytes are looked up before the download of the first adds them again.
			panel.Children.Add(secondImage);
			await WindowHelper.WaitFor(() => secondBitmap.IsOpened, 10000, "The second bitmap must be decoded again");
			panel.Children.Add(firstImage);
			await WindowHelper.WaitFor(() => firstBitmap.IsOpened, 10000, "The first bitmap must be decoded again");

			Assert.AreEqual(2, first.RequestCount, "The least recently used bytes must be dropped once the cache is full");
			Assert.AreEqual(1, second.RequestCount, "The bytes within the bound must be kept");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Disposed_While_Displayed_Then_Opened_Again()
	{
		// Dispose drops what the source decoded, but an Image still showing it must not be left without an image: it keeps
		// the previous pixels until the source is opened again for it.
		using var _ = DisableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);

			bitmap.Dispose();
			Assert.IsNotNull(surface.Image, "The Image keeps showing the previous pixels until the source is opened again");

			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The source must be opened again for the Image that still shows it");
			await WindowHelper.WaitForIdle();

			var reopened = GetSurface(bitmap);
			Assert.AreNotSame(surface, reopened);
			Assert.IsNotNull(reopened?.Image);
			Assert.IsNull(surface.Image, "The previous pixels are released once the Image shows the new ones");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Disposed_While_Downloading_Then_Subscriber_Still_Opened()
	{
		// Dispose cancels the open in flight; an Image still showing the source must get it from a new one.
		using var _ = DisableBitmapImageCache();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var bitmap = new BitmapImage(server.Uri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "the download never started");

			bitmap.Dispose();
			server.ReleaseResponses();

			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "The Image that still shows the source must get its image");
			Assert.AreEqual(100, bitmap.PixelWidth);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Reentered_Within_Release_Delay_Then_Decoded_Image_Kept()
	{
		// Within the limit, an Image that comes back before the release delay ends keeps its pixels.
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));
		using var decoder = TrackingDecoder.Install();

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			var bitmapOpened = 0;
			var imageOpened = 0;
			bitmap.ImageOpened += (_, _) => bitmapOpened++;
			image.ImageOpened += (_, _) => imageOpened++;

			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);
			var decodes = decoder.Count(500, 200);

			WindowHelper.WindowContent = null;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(bitmap.IsOpened, "The source keeps its data within the release delay");
			Assert.IsNotNull(surface.Image, "The source keeps its pixels within the release delay");

			await UITestHelper.Load(image);
			Assert.AreSame(surface, GetSurface(bitmap));
			Assert.AreEqual(decodes, decoder.Count(500, 200), "Nothing must be decoded again");
			Assert.AreEqual(1, bitmapOpened);
			Assert.AreEqual(1, imageOpened);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Unused_Past_Release_Delay_Then_Released()
	{
		var releaseDelay = TimeSpan.FromMilliseconds(200);
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(releaseDelay);

		try
		{
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			var surface = GetSurface(bitmap);

			var unloaded = Stopwatch.StartNew();
			WindowHelper.WindowContent = null;
			Assert.IsNotNull(surface.Image, "Nothing is released before the delay");

			await WindowHelper.WaitFor(() => surface.Image is null, 5000, "The decoded pixels must be released once nothing has displayed them for the delay");
			Assert.IsTrue(unloaded.Elapsed >= releaseDelay - TimeSpan.FromMilliseconds(10), $"Released after {unloaded.Elapsed}, before the delay");
			Assert.IsFalse(bitmap.IsOpened);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Undisplayed_Images_Exceed_Limit_Then_Oldest_Released_First()
	{
		// The limit is set just under what the three images hold, so that releasing the oldest is enough.
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmaps = new BitmapImage[3];
			var images = new Image[3];
			for (var i = 0; i < 3; i++)
			{
				bitmaps[i] = new BitmapImage(new Uri(AssetUri));
				images[i] = new Image { Width = 100, Height = 40, Source = bitmaps[i] };
				await AddAndWaitForOpened(panel, images[i]);
			}

			var surfaces = bitmaps.Select(GetSurface).ToArray();
			Assert.IsTrue(surfaces.All(s => s.DecodedByteCount >= 500 * 200 * 4), "Pre-condition: a decode counts its pixels");

			// Measured in the same turn as the removals, so no texture is drawn in between and the sizes are those that wait.
			FeatureConfiguration.Image.MaxUndisplayedImageBytes = surfaces.Sum(s => s.DecodedByteCount) - 1;
			foreach (var image in images)
			{
				panel.Children.Remove(image);
			}

			Assert.IsNotNull(surfaces[0].Image, "Nothing is released before the layout pass");
			await WindowHelper.WaitForIdle();

			Assert.IsNull(surfaces[0].Image, "The image that waits the longest is released first once the waiting images exceed the limit");
			Assert.IsNotNull(surfaces[1].Image, "Images within the limit keep their pixels");
			Assert.IsNotNull(surfaces[2].Image, "Images within the limit keep their pixels");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public void When_Many_Images_Wait_Then_Each_Starts_Its_Wait_At_Once()
	{
		// Starting a wait must not walk the waits already under way: unloading thousands of small images at once would
		// otherwise take quadratic time on the UI thread (40,000 waits is 800 million visits).
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));
		var entries = new List<PendingImageReleases.Entry>(40_000);

		try
		{
			var stopwatch = Stopwatch.StartNew();
			for (var i = 0; i < 40_000; i++)
			{
				entries.Add(PendingImageReleases.Add(1, static () => { }));
			}

			stopwatch.Stop();
			Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromMilliseconds(500), $"Starting 40,000 waits took {stopwatch.Elapsed}");
		}
		finally
		{
			entries.ForEach(PendingImageReleases.Remove);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Over_Limit_Behind_A_Download_Then_Undisplayed_Image_Still_Released()
	{
		// Over the limit, a download in flight that nothing waits for (no bytes yet) is passed over, not released: the
		// undisplayed image behind it goes, and the download goes on.
		using var _ = DisableBitmapImageCache();
		using var limit = ReleaseAfterLayoutPass();
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var downloading = new BitmapImage(server.Uri);
			var downloaded = TrackOpen(downloading);
			var first = new Image { Width = 100, Height = 40, Source = downloading };
			panel.Children.Add(first);
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the first image is downloading");
			panel.Children.Remove(first);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var second = new Image { Width = 100, Height = 40, Source = bitmap };
			await AddAndWaitForOpened(panel, second);
			var surface = GetSurface(bitmap);
			panel.Children.Remove(second);
			await WindowHelper.WaitForIdle();

			Assert.IsNull(surface.Image, "The undisplayed image behind the download is released over the limit");

			server.ReleaseResponses();
			Assert.IsTrue(await downloaded.WaitAsync(TimeSpan.FromSeconds(10)), "The download goes on");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Images_Leave_With_The_Same_Pixels_Then_Each_Can_Come_Back_With_Them()
	{
		// Two Images leave drawing the first image while a third draws its replacement. The first one back takes the image
		// and moves on to the replacement; the other, back while the next download is held, must still find the image.
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var staying = new Image { Source = bitmap };
			var firstBack = new Image { Source = bitmap };
			var secondBack = new Image { Source = bitmap };
			foreach (var image in new[] { staying, firstBack, secondBack })
			{
				panel.Children.Add(image);
			}

			await WindowHelper.WaitFor(() => secondBack.ActualWidth > 99, 10000, "Pre-condition: the Images draw the first image");
			await WindowHelper.WaitForIdle();
			var previous = GetSurface(bitmap);

			panel.Children.Remove(firstBack);
			panel.Children.Remove(secondBack);
			bitmap.UriSource = new Uri(TallAssetUri);
			await WindowHelper.WaitFor(() => staying.ActualWidth is > 0 and < 99, 10000, "Pre-condition: the staying Image draws the second image");

			panel.Children.Add(firstBack);
			await WindowHelper.WaitFor(() => firstBack.ActualWidth is > 0 and < 99, 10000, "Pre-condition: the first Image back draws the second image");
			await WindowHelper.WaitForIdle();

			bitmap.UriSource = server.Uri;
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");
			panel.Children.Add(secondBack);
			await UITestHelper.WaitForLoaded(secondBack, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(previous.Image, "The other Image, still away, keeps the first image");
			Assert.AreEqual(100, secondBack.ActualWidth, 1, "Back while the download is held, it draws the first image");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Replaced_Image_Released_After_Pass_Then_Older_Waiting_Image_Kept()
	{
		// An image waits within the limit when another source's displayed image is replaced. The replaced image goes after
		// the pass that draws its replacement, and must not count toward the limit meanwhile, which would release the
		// waiting image with it.
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var waitingBitmap = new BitmapImage(new Uri(AssetUri));
			var shownBitmap = new BitmapImage(new Uri(AssetUri));
			var waitingImage = new Image { Width = 100, Height = 40, Source = waitingBitmap };
			var shownImage = new Image { Width = 100, Height = 40, Source = shownBitmap };
			await AddAndWaitForOpened(panel, waitingImage);
			await AddAndWaitForOpened(panel, shownImage);
			await WindowHelper.WaitForIdle();
			var waiting = GetSurface(waitingBitmap);
			var replaced = GetSurface(shownBitmap);

			// Set in the same turn as the removal, so the waiting image alone fits the limit exactly.
			FeatureConfiguration.Image.MaxUndisplayedImageBytes = waiting.DecodedByteCount;
			panel.Children.Remove(waitingImage);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(waiting.Image, "An Image that leaves with the current image has it wait once, so it fits a limit of its own size");

			var opened = TrackOpened(shownImage);
			shownBitmap.UriSource = new Uri(SquareAssetUri);
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsNull(replaced.Image, "The replaced image goes after the pass that draws its replacement");
			Assert.IsNotNull(waiting.Image, "The waiting image must not be released for the replaced one");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Moved_During_Layout_After_Loaded_Then_Decoded_Image_Kept()
	{
		// An Image moved by the layout that follows Loaded within a pass is only loaded again by the next pass, so even with
		// a limit of 0 its pixels must wait for that pass. A Rendering handler adds an element, so the pass raises Loaded a
		// second time; that element's Loaded invalidates a control whose measure, in the layout run after it, moves the Image.
		using var _ = DisableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var first = new StackPanel();
			var second = new StackPanel();
			var hook = new MeasureHook();
			var root = new StackPanel { Children = { first, second, hook } };
			await UITestHelper.Load(root, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await AddAndWaitForOpened(first, image);
			await WindowHelper.WaitForIdle();
			var surface = GetSurface(bitmap);
			var decodes = decoder.Count(500, 200);

			var armed = false;
			var moved = false;
			hook.Measuring = () =>
			{
				if (armed && !moved)
				{
					moved = true;
					first.Children.Remove(image);
					second.Children.Add(image);
				}
			};

			var marker = new Border { Width = 1, Height = 1 };
			marker.Loaded += (_, _) =>
			{
				armed = true;
				hook.InvalidateMeasure();
			};

			EventHandler<object> rendering = null;
			rendering = (_, _) =>
			{
				Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= rendering;
				root.Children.Add(marker);
			};
			Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += rendering;

			await WindowHelper.WaitFor(() => moved, 5000, "Pre-condition: the Image must be moved by the layout after Loaded");
			await WindowHelper.WaitFor(() => image.IsLoaded, 5000);
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(surface.Image, "An Image moved during the pass keeps its decoded pixels until the next pass loads it");
			Assert.AreSame(surface, GetSurface(bitmap));
			Assert.AreEqual(decodes, decoder.Count(500, 200), "Nothing must be decoded again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_RenderTargetBitmap_Open_Superseded_Then_Abandoned_Surface_Released()
	{
		// A RenderTargetBitmap builds its surface off the UI thread and applies it in a dispatched continuation; an open
		// superseded before that (a new render of a live preview, or Dispose) must still release the surface it built.
		using var decoder = TrackingDecoder.Install();

		try
		{
			var content = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
			var bitmap = new RenderTargetBitmap();
			var image = new Image { Width = 20, Height = 20, Source = bitmap };
			await UITestHelper.Load(new StackPanel { Children = { content, image } }, x => x.IsLoaded);

			await bitmap.RenderAsync(content);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 5000, "Pre-condition: the render is shown");

			// Dispose opens the bitmap again for the Image that shows it.
			var before = decoder.All.Length;
			bitmap.Dispose();

			// Holding the UI thread keeps the dispatched continuation from applying the result while the open completes.
			Assert.IsTrue(SpinWait.SpinUntil(() => decoder.All.Length > before, 5000), "Pre-condition: the open must build its surface");
			var abandoned = decoder.All[before];

			bitmap.Dispose();
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 5000, "The second open must deliver");
			await WindowHelper.WaitForIdle();

			await TestHelper.RetryAssert(() => Assert.IsTrue(abandoned.IsReleased, "The surface built by the superseded open must be released"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Fails_Then_Previous_Image_Not_Shown()
	{
		// WinUI drops the surfaces of a source that failed to open, so the Image shows nothing rather than its previous
		// image, which is then released, whether it was owned or shared through the cache.
		await AssertFailureClearsImage(cacheEnabled: true);
		await AssertFailureClearsImage(cacheEnabled: false);

		static async Task AssertFailureClearsImage(bool cacheEnabled)
		{
			using var cache = SetBitmapImageCache(cacheEnabled);
			using var delay = ReleaseAfterLayoutPass();

			try
			{
				var bitmap = new BitmapImage(new Uri(AssetUri));
				var image = new Image { Source = bitmap };
				var panel = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
				await UITestHelper.Load(panel, x => x.IsLoaded);
				await AddAndWaitForOpened(panel, image);
				await WindowHelper.WaitForIdle();
				var surface = GetSurface(bitmap);
				Assert.IsTrue(image.ActualWidth > 0, "Pre-condition: the Image shows its source");

				var failed = new TaskCompletionSource();
				image.ImageFailed += (_, _) => failed.TrySetResult();
				bitmap.UriSource = new Uri("ms-appx:///Assets/does-not-exist.png");
				await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(0, image.ActualWidth, $"The Image must not show its previous image once its source failed (cache {(cacheEnabled ? "on" : "off")})");
				Assert.IsNull(surface.Image, $"The previous image is released once nothing shows it (cache {(cacheEnabled ? "on" : "off")})");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Changed_During_Notification_Then_Skipped_Subscriber_Keeps_Its_Pixels()
	{
		// An ImageBrush notified before an Image changes the source again from its ImageOpened, to one that is still
		// downloading: the notification stops before the Image, which still shows the first image, so that one must stay
		// until the third arrives.
		var square = await ReadAssetAsync(SquareAssetUri);
		var tall = await ReadAssetAsync(TallAssetUri);
		using var _ = DisableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var bitmap = new BitmapImage();
			var brush = new ImageBrush { ImageSource = bitmap };
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			var opened = TrackOpened(image);
			await bitmap.SetSourceAsync(new MemoryStream(square));
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();
			var first = GetSurface(bitmap);

			var armed = true;
			brush.ImageOpened += (_, _) =>
			{
				if (armed)
				{
					armed = false;
					bitmap.UriSource = server.Uri;
				}
			};

			var second = bitmap.SetSourceAsync(new MemoryStream(tall));
			await WindowHelper.WaitFor(() => !armed, 10000, "Pre-condition: the brush gets the second image and changes the source");
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(first.Image, "The Image the notification skipped still shows the first image, which must stay");

			opened = TrackOpened(image);
			server.ReleaseResponses();
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsNull(first.Image, "Once every subscriber has the third image, the first is released");
			GC.KeepAlive(brush);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Changed_During_Successive_Notifications_Then_Only_Shown_Pixels_Kept()
	{
		// A brush notified before an Image sets the next stream from each ImageOpened, so every notification stops before
		// the Image: the Image keeps showing the first image throughout, while the brush moves through the others, and
		// each image the brush moved past must go.
		var square = await ReadAssetAsync(SquareAssetUri);
		var tall = await ReadAssetAsync(TallAssetUri);
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var bitmap = new BitmapImage();
			var brush = new ImageBrush { ImageSource = bitmap };
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			var opened = TrackOpened(image);
			await bitmap.SetSourceAsync(new MemoryStream(square));
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();
			var first = GetSurface(bitmap);

			var remaining = 6;
			var mostKept = 0;
			var firstKept = true;
			var done = new TaskCompletionSource();
			brush.ImageOpened += (_, _) =>
			{
				mostKept = Math.Max(mostKept, decoder.All.Count(d => !d.IsReleased));
				firstKept &= first.Image is not null;

				if (remaining-- > 0)
				{
					_ = bitmap.SetSourceAsync(new MemoryStream(remaining % 2 == 0 ? square : tall));
				}
				else
				{
					done.TrySetResult();
				}
			};

			_ = bitmap.SetSourceAsync(new MemoryStream(tall));
			await done.Task.WaitAsync(TimeSpan.FromSeconds(20));
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(firstKept, "The Image, skipped by every notification, still shows the first image, which must stay");
			Assert.IsTrue(mostKept <= 4, $"Only the images still shown (and one being replaced) may be kept, not {mostKept}");
			Assert.IsNull(first.Image, "Once the Image has the last image, the first is released");
			Assert.IsTrue(decoder.All.Count(d => !d.IsReleased) <= 1, "Only the image shown is kept at the end");
			GC.KeepAlive(brush);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_PixelWidth_Handler_Changes_Source_Then_Replacement_Raises_ImageOpened()
	{
		// Taking the pixel size of a decode runs property-changed handlers; one that sets a new UriSource supersedes the open,
		// which must then raise nothing (nor mark the new source as opened) and release its decode.
		using var _ = DisableBitmapImageCache();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var bitmap = new BitmapImage();
			var widths = new List<int>();
			bitmap.ImageOpened += (_, _) => widths.Add(bitmap.PixelWidth);

			var armed = true;
			bitmap.RegisterPropertyChangedCallback(BitmapSource.PixelWidthProperty, (_, _) =>
			{
				if (armed && bitmap.PixelWidth == 500)
				{
					armed = false;
					bitmap.UriSource = new Uri(SquareAssetUri);
				}
			});

			bitmap.UriSource = new Uri(AssetUri);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			await WindowHelper.WaitFor(() => !armed && bitmap.IsOpened && bitmap.PixelWidth == 100, 10000, "Pre-condition: the replacement is decoded");
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { 100 }, widths, "Only the replacement raises ImageOpened");
			Assert.IsTrue(decoder.Decoded(500, 200).All(d => d.IsReleased), "The superseded decode is released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_PixelWidth_Handler_Supersedes_Shared_Open_Then_Decode_Released()
	{
		// Taking the pixel size of a shared result runs property-changed handlers; one that sets a new UriSource supersedes
		// the open, which must then stop being a user of the shared decode, so that nothing keeps it.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			// A decode size of its own gives each source its own cache key; both decode to that size.
			var bitmap = NewBitmap(AssetUri, 59, 23);
			var armed = true;
			bitmap.RegisterPropertyChangedCallback(BitmapSource.PixelWidthProperty, (_, _) =>
			{
				if (armed && bitmap.PixelWidth == 59)
				{
					armed = false;
					bitmap.UriSource = new Uri(SquareAssetUri);
				}
			});

			await UITestHelper.Load(new Image { Width = 100, Height = 40, Source = bitmap });
			await WindowHelper.WaitFor(() => !armed && bitmap.IsOpened, 10000, "Pre-condition: the replacement is decoded");
			await WindowHelper.WaitForIdle();

			var superseded = decoder.Decoded(59, 23).First();
			await TestHelper.RetryAssert(() => Assert.IsTrue(superseded.IsReleased, "The shared decode the superseded open got must be released"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_ImageOpened_Handler_Throws_Then_Shared_Decode_Released()
	{
		// A handler that throws turns the open into a failure, which hands nothing over to the source: the open must still
		// stop being a user of the shared decode, so that nothing keeps it.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var bitmap = NewBitmap(AssetUri, 67, 27);
			bitmap.ImageOpened += (_, _) => throw new InvalidOperationException("Thrown by the test");
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			var failed = new TaskCompletionSource();
			image.ImageFailed += (_, _) => failed.TrySetResult();
			await UITestHelper.Load(image);
			await failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			var decoded = decoder.Decoded(67, 27).Single();
			await TestHelper.RetryAssert(() => Assert.IsTrue(decoded.IsReleased, "The shared decode nothing got must be released"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Source_Changed_During_Notification_Then_Skipped_Subscriber_Keeps_Shared_Decode()
	{
		// A subscriber notified before an Image changes the source again when it gets the second image, to one still
		// downloading: the notification stops before the Image, which still shows the first image, so its shared decode
		// must stay until the third arrives. A brush made before the Image is notified before it.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var bitmap = NewBitmap(AssetUri, 61, 25);
			var armed = false;
			var changer = new ImageBrush { ImageSource = bitmap };
			changer.ImageOpened += (_, _) =>
			{
				if (armed)
				{
					armed = false;
					bitmap.UriSource = server.Uri;
				}
			};

			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			await WindowHelper.WaitForIdle();
			var first = GetSurface(bitmap);

			armed = true;
			bitmap.UriSource = new Uri(SquareAssetUri);
			await WindowHelper.WaitFor(() => !armed, 10000, "Pre-condition: the subscriber gets the second image and changes the source");
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(first.Image, "The Image the notification skipped still shows the first image, whose shared decode must stay");

			var opened = TrackOpened(image);
			server.ReleaseResponses();
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsNull(first.Image, "Once every subscriber has the third image, the first shared decode is released");
			GC.KeepAlive(changer);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_PixelWidth_Handler_Switches_To_Cached_Source_Then_Size_Is_The_Replacement()
	{
		// A PixelWidth handler switches the bitmap to a URI whose cached load already completed, so the replacement sets its
		// size before the first open sets its height, which must not overwrite it. Only a brush displays the bitmap, so no
		// Image measure sets the size again.
		using var cache = EnableBitmapImageCache();
		using var server = await GatedImageServer.StartAsync();
		server.ReleaseResponses();

		var warm = new BitmapImage(server.Uri);
		var warmed = TrackOpen(warm);
		var warmBrush = new ImageBrush { ImageSource = warm };
		Assert.IsTrue(await warmed.WaitAsync(TimeSpan.FromSeconds(10)), "Pre-condition: the cache entry completes");

		var bitmap = new BitmapImage();
		var switched = false;
		bitmap.RegisterPropertyChangedCallback(BitmapSource.PixelWidthProperty, (_, _) =>
		{
			if (!switched && bitmap.PixelWidth == 500)
			{
				switched = true;
				bitmap.UriSource = server.Uri;
			}
		});

		var opened = TrackOpen(bitmap);
		var brush = new ImageBrush { ImageSource = bitmap };
		bitmap.UriSource = new Uri(AssetUri);
		Assert.IsTrue(await opened.WaitAsync(TimeSpan.FromSeconds(10)), "Pre-condition: the replacement opens");
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(switched, "Pre-condition: the handler switched the source");
		Assert.AreEqual(100, bitmap.PixelWidth, "The size is the replacement's");
		Assert.AreEqual(100, bitmap.PixelHeight, "The superseded open must not overwrite the replacement's height");
		GC.KeepAlive(brush);
		GC.KeepAlive(warmBrush);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_PixelWidth_Handler_Switches_To_Cached_Source_Then_Superseded_Shared_Decode_Released()
	{
		// As above: the open the handler supersedes stops as soon as its width is set, and must still give up its use of
		// the shared decode it got, so that nothing keeps it. Decode sizes of their own give this test its own cache keys.
		using var cache = EnableBitmapImageCache();
		using var limit = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();
		using var server = await GatedImageServer.StartAsync();
		server.ReleaseResponses();

		var warm = NewBitmap(server.Uri.OriginalString, 63, 21);
		var warmed = TrackOpen(warm);
		var warmBrush = new ImageBrush { ImageSource = warm };
		Assert.IsTrue(await warmed.WaitAsync(TimeSpan.FromSeconds(10)), "Pre-condition: the cache entry completes");
		var decodedBefore = decoder.All.Length;

		var bitmap = new BitmapImage { DecodePixelWidth = 63, DecodePixelHeight = 21 };
		var switched = false;
		bitmap.RegisterPropertyChangedCallback(BitmapSource.PixelWidthProperty, (_, _) =>
		{
			if (!switched && bitmap.PixelWidth == 63)
			{
				switched = true;
				bitmap.UriSource = server.Uri;
			}
		});

		var opened = TrackOpen(bitmap);
		var brush = new ImageBrush { ImageSource = bitmap };
		bitmap.UriSource = new Uri(AssetUri);
		Assert.IsTrue(await opened.WaitAsync(TimeSpan.FromSeconds(10)), "Pre-condition: the replacement opens");
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(switched, "Pre-condition: the handler switched the source");
		var superseded = decoder.All[decodedBefore];
		await TestHelper.RetryAssert(() => Assert.IsTrue(superseded.IsReleased, "The shared decode of the superseded open must be released"));
		GC.KeepAlive(brush);
		GC.KeepAlive(warmBrush);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Changed_By_Last_Subscriber_Then_Previous_Pixels_Released()
	{
		// The same change made by the last subscriber skips nobody: every subscriber shows the second image, so the first
		// goes after the layout pass, without waiting for the third.
		using var _ = DisableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			await WindowHelper.WaitForIdle();
			var first = GetSurface(bitmap);
			var brush = new ImageBrush { ImageSource = bitmap };

			var armed = true;
			brush.ImageOpened += (_, _) =>
			{
				if (armed)
				{
					armed = false;
					bitmap.UriSource = server.Uri;
				}
			};

			bitmap.UriSource = new Uri(TallAssetUri);
			await WindowHelper.WaitFor(() => !armed, 10000, "Pre-condition: the brush gets the second image and changes the source");
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");
			await WindowHelper.WaitForIdle();

			Assert.IsNull(first.Image, "No subscriber shows the first image anymore, so it must not wait for the third");
			GC.KeepAlive(brush);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Rendered_Brush_Changes_UriSource_Then_It_Keeps_Drawing_Until_Replaced(bool useCache)
	{
		// A drawn ImageBrush whose ImageOpened sets a UriSource that is still downloading keeps drawing the image it has,
		// as an Image does, so that image must stay until the download arrives, whether it is owned or shared.
		using var _ = SetBitmapImageCache(useCache);
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();

			// With the cache on, a decode size of its own gives each image its own cache key.
			var bitmap = useCache ? NewBitmap(AssetUri, 71, 31) : new BitmapImage(new Uri(AssetUri));
			var brush = new ImageBrush { ImageSource = bitmap };
			var opened = 0;
			var armed = false;
			brush.ImageOpened += (_, _) =>
			{
				opened++;
				if (armed)
				{
					armed = false;
					bitmap.UriSource = server.Uri;
				}
			};

			var border = new Border { Width = 100, Height = 40, Background = brush };
			await UITestHelper.Load(border);
			await WindowHelper.WaitFor(() => opened == 1, 10000, "Pre-condition: the brush gets the first image");
			await WindowHelper.WaitForIdle();

			armed = true;
			bitmap.UriSource = new Uri(SquareAssetUri);
			await WindowHelper.WaitFor(() => !armed, 10000, "Pre-condition: the brush gets the second image and changes the source");
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");
			await WindowHelper.WaitForIdle();

			var compositionBrush = (CompositionSurfaceBrush)brush.GetOrCreateCompositionBrush(border.Visual.Compositor);
			var drawn = compositionBrush.Surface as CompositionImageSurface;
			Assert.IsNotNull(drawn, "Pre-condition: the brush draws an image");
			Assert.IsNotNull(drawn.Image, "The brush still draws the second image, which must stay until the third arrives");

			server.ReleaseResponses();
			await WindowHelper.WaitFor(() => opened == 3, 10000, "Pre-condition: the brush gets the third image");
			await WindowHelper.WaitForIdle();

			Assert.AreNotSame(drawn, compositionBrush.Surface, "The brush draws the third image");
			Assert.IsNull(drawn.Image, "Once the brush draws the third image, the second is released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Rendered_Brush_ImageSource_Replaced_Then_Previous_Image_Not_Drawn(bool useCache)
	{
		// A drawn ImageBrush given another bitmap, still downloading, draws nothing until it arrives, as on WinUI, where a
		// brush draws its source's surface. It must not keep drawing the first image, released once nothing subscribes to it,
		// whether owned or shared.
		using var _ = SetBitmapImageCache(useCache);
		using var limit = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			// With the cache on, a decode width of its own gives the image its own cache key.
			var brush = new ImageBrush { ImageSource = new BitmapImage { DecodePixelWidth = useCache ? 233 : 0, UriSource = new Uri(AssetUri) } };
			var opened = 0;
			brush.ImageOpened += (_, _) => opened++;

			var border = new Border { Width = 100, Height = 40, Background = brush };
			await UITestHelper.Load(border);
			await WindowHelper.WaitFor(() => opened == 1, 10000, "Pre-condition: the brush gets the first image");
			await WindowHelper.WaitForIdle();

			var compositionBrush = (CompositionSurfaceBrush)brush.GetOrCreateCompositionBrush(border.Visual.Compositor);
			var previous = compositionBrush.Surface as CompositionImageSurface;
			Assert.IsNotNull(previous?.Image, "Pre-condition: the brush draws the first image");

			brush.ImageSource = new BitmapImage(server.Uri);
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the second image is downloading");
			await WindowHelper.WaitForIdle();

			Assert.IsNull(compositionBrush.Surface, "Until the second image arrives, the brush draws nothing, not the first image");
			Assert.IsNull(previous.Image, "Nothing subscribes to the first image anymore, so it is released");

			server.ReleaseResponses();
			await WindowHelper.WaitFor(() => opened == 2, 10000, "Pre-condition: the brush gets the second image");
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(compositionBrush.Surface is CompositionImageSurface { Image: not null }, "The brush draws the second image");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Cached_Open_Handler_Changes_Source_Then_Image_Releases_Replacement_On_Unload()
	{
		// Opening a completed cache entry raises BitmapImage.ImageOpened while the Image subscribes. A handler that switches
		// the Image to an uncached source has the Image subscribe to that one first: it is the subscription the Image must
		// keep, and let go of when unloaded, so the pixels it owns are released.
		using var cache = EnableBitmapImageCache();
		using var limit = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			server.ReleaseResponses();
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			// Completes the cache entry for the server's image.
			var filler = new BitmapImage(server.Uri);
			await AddAndWaitForOpened(panel, new Image { Width = 100, Height = 40, Source = filler });
			var shared = GetSurface(filler);

			var cached = new BitmapImage(server.Uri);
			var uncached = new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache, UriSource = new Uri(AssetUri) };
			var image = new Image { Width = 100, Height = 40, Source = cached };
			var loadedRaised = false;
			var switchedWhileSubscribing = false;
			image.Loaded += (_, _) => loadedRaised = true;
			cached.ImageOpened += (_, _) =>
			{
				if (image.Source == cached)
				{
					switchedWhileSubscribing = image.IsLoaded && !loadedRaised;
					image.Source = uncached;
				}
			};

			var opened = TrackOpen(uncached);
			panel.Children.Add(image);
			Assert.IsTrue(await opened.WaitAsync(TimeSpan.FromSeconds(10)), "Pre-condition: the uncached source opens");
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(switchedWhileSubscribing, "Pre-condition: the cached open raises ImageOpened while the Image subscribes");
			var surface = GetSurface(uncached);

			panel.Children.Remove(image);
			await WindowHelper.WaitForIdle();

			Assert.IsNull(surface.Image, "Unloaded, the Image lets go of the source it switched to, so its pixels are released");

			// The cached source the handler switched away from keeps no use of the shared decode either.
			panel.Children.Clear();
			await WindowHelper.WaitForIdle();
			Assert.IsNull(shared.Image, "Once nothing draws the shared decode, it is released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Cached_Open_Handler_Unloads_And_Disposes_Then_Image_Opens_On_Reload()
	{
		// A displayed bitmap switches to a URI whose cached load completed, so its ImageOpened is raised before the open
		// returns. A handler that unloads the Image and disposes the bitmap ends that open: it must not leave the source as
		// opening, which would have the Image back in the tree wait forever.
		using var cache = EnableBitmapImageCache();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			server.ReleaseResponses();
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			// Completes the cache entry for the server's image.
			await AddAndWaitForOpened(panel, new Image { Width = 100, Height = 40, Source = new BitmapImage(server.Uri) });

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await AddAndWaitForOpened(panel, image);

			var handled = false;
			bitmap.ImageOpened += (_, _) =>
			{
				if (!handled)
				{
					handled = true;
					panel.Children.Remove(image);
					bitmap.Dispose();
				}
			};

			bitmap.UriSource = server.Uri;
			Assert.IsTrue(handled, "Pre-condition: the cached open raises ImageOpened before it returns");
			await WindowHelper.WaitForIdle();

			await AddAndWaitForOpened(panel, image);
			Assert.IsTrue(bitmap.IsOpened, "The Image back in the tree has its source opened again");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Cached_Open_Handler_Changes_Brush_Source_Then_Brush_Draws_Replacement()
	{
		// The same for a brush: the source its ImageOpened handler switched it to, while it subscribed, is the one it draws.
		using var cache = EnableBitmapImageCache();
		using var limit = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			server.ReleaseResponses();
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			// Completes the cache entry for the server's image.
			var filler = new BitmapImage(server.Uri);
			var fillerImage = new Image { Width = 100, Height = 40, Source = filler };
			await AddAndWaitForOpened(panel, fillerImage);
			var shared = GetSurface(filler);

			var cached = new BitmapImage(server.Uri);
			var other = new BitmapImage(new Uri(TallAssetUri));
			var brush = new ImageBrush();
			var switched = false;
			cached.ImageOpened += (_, _) =>
			{
				if (brush.ImageSource == cached)
				{
					switched = true;
					brush.ImageSource = other;
				}
			};

			var border = new Border { Width = 100, Height = 40, Background = brush };
			panel.Children.Add(border);
			await WindowHelper.WaitForLoaded(border);

			var opened = TrackOpen(other);
			brush.ImageSource = cached;
			Assert.IsTrue(switched, "Pre-condition: the cached open raises ImageOpened while the brush subscribes");
			Assert.IsTrue(await opened.WaitAsync(TimeSpan.FromSeconds(10)), "Pre-condition: the other source opens");
			await WindowHelper.WaitForIdle();

			var compositionBrush = (CompositionSurfaceBrush)brush.GetOrCreateCompositionBrush(border.Visual.Compositor);
			Assert.AreSame(GetSurface(other), compositionBrush.Surface, "The brush draws the source it was switched to");

			// The cached source the handler switched away from keeps no use of the shared decode.
			panel.Children.Remove(fillerImage);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(shared.Image, "Once nothing draws the shared decode, it is released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Image_Showing_Replaced_Pixels_Leaves_Then_They_Wait_Like_Undisplayed_Ones()
	{
		// While the replacement downloads, a second Image subscribes and shows nothing yet. The first Image, the only one
		// showing the previous image, keeps it when moved within a pass; once it leaves, the previous image waits like any
		// image nothing displays (here, over the limit by itself: until the end of the next pass), not for the download.
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(100_000);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var first = new StackPanel();
			var second = new StackPanel();
			await UITestHelper.Load(new StackPanel { Children = { first, second } }, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await AddAndWaitForOpened(first, image);
			await WindowHelper.WaitForIdle();
			var previous = GetSurface(bitmap);

			bitmap.UriSource = server.Uri;
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the replacement is downloading");
			var other = new Image { Width = 100, Height = 40, Source = bitmap };
			second.Children.Add(other);
			await WindowHelper.WaitForLoaded(other);
			await WindowHelper.WaitForIdle();

			first.Children.Remove(image);
			second.Children.Add(image);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(previous.Image, "An Image moved within the pass still shows the previous image, which must stay");

			second.Children.Remove(image);
			await WindowHelper.WaitForIdle();
			Assert.IsNull(previous.Image, "Once nothing shows the previous image, it must not wait for the download");

			var opened = TrackOpened(other);
			server.ReleaseResponses();
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_UriSource_Changed_Then_Previous_Shared_Decode_Released()
	{
		// An Image keeps drawing the previous shared image until it draws the new one, and then nothing keeps the previous
		// one, although nothing else changes after that.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			var bitmap = NewBitmap(AssetUri, 83, 33);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await LoadAndWaitForOpened(image);
			var first = GetSurface(bitmap);

			var opened = TrackOpened(image);
			bitmap.UriSource = new Uri(SquareAssetUri);
			Assert.IsNotNull(first.Image, "The Image keeps drawing the first image until it draws the new one");

			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();
			Assert.IsNull(first.Image, "Once the Image draws the new image, nothing draws the first, which is released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Shared_Decode_Given_But_Not_Drawn_Then_Kept()
	{
		// A collapsed Image is given what its source opens but draws it only once it is measured. When the source moves on
		// to an image still downloading, the shared decode the Image was given is held by nothing else, and must stay for
		// when the Image draws it.
		using var _ = EnableBitmapImageCache();
		using var delay = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = NewBitmap(AssetUri, 79, 32);
			var image = new Image { Width = 100, Height = 40, Source = bitmap, Visibility = Visibility.Collapsed };
			panel.Children.Add(image);
			await WindowHelper.WaitFor(() => image.IsLoaded && bitmap.IsOpened, 10000, "Pre-condition: the first image is opened");

			bitmap.UriSource = new Uri(SquareAssetUri);
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "Pre-condition: the second image is opened and given to the Image");
			var given = GetSurface(bitmap);

			bitmap.UriSource = server.Uri;
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");

			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(given.Image, "The Image was given the second image and will draw it, so it must stay");

			server.ReleaseResponses();
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "Pre-condition: the third image is opened");
			await WindowHelper.WaitFor(() => given.Image is null, 5000, "Once the Image was given the third image instead, nothing keeps the second");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	public async Task When_Image_Returns_While_Replacement_Downloads_Then_Shared_Pixels_Kept()
	{
		// An Image that leaves while the replacement downloads lets go of the shared decode it still draws, which then
		// waits; coming back before the wait ends, still drawing it, it uses it again, so it stays until the replacement
		// arrives, and goes like any other once nothing draws it.
		var releaseDelay = TimeSpan.FromMilliseconds(300);
		using var _ = EnableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(releaseDelay);

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = NewBitmap(AssetUri, 73, 29);
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await AddAndWaitForOpened(panel, image);
			await WindowHelper.WaitForIdle();
			var previous = GetSurface(bitmap);

			bitmap.UriSource = server.Uri;
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the replacement is downloading");
			panel.Children.Remove(image);
			panel.Children.Add(image);
			await WindowHelper.WaitForIdle();
			await Task.Delay(releaseDelay * 3);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(previous.Image, "The Image that came back still draws the shared decode, which must stay");

			server.ReleaseResponses();
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "Pre-condition: the replacement is opened");
			await WindowHelper.WaitFor(() => previous.Image is null, 5000, "Once nothing draws the shared decode, it is released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Image_Returns_After_Replacement_Arrived_Then_Replaced_Pixels_Released()
	{
		// An Image that left while the replacement downloaded comes back once it has arrived, still drawing the previous
		// image, and is given the replacement at once: the previous image goes after that pass, not when its wait ends.
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var panel = new StackPanel();
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await AddAndWaitForOpened(panel, image);
			await WindowHelper.WaitForIdle();
			var previous = GetSurface(bitmap);

			bitmap.UriSource = server.Uri;
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the replacement is downloading");
			panel.Children.Remove(image);
			await WindowHelper.WaitForIdle();

			server.ReleaseResponses();
			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "Pre-condition: the replacement arrives while nothing subscribes");
			Assert.IsNotNull(previous.Image, "Pre-condition: the previous image waits within the delay");

			var opened = TrackOpened(image);
			panel.Children.Add(image);
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.IsNull(previous.Image, "Once the Image is given the replacement, the previous image must not wait for the delay");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Image_Returns_After_Source_Cleared_Then_Replaced_Pixels_Not_Kept()
	{
		// An Image that left while the replacement downloaded comes back, still drawing the previous image, after the source
		// was cleared: nothing will replace that image for it, so it must not keep it, nor show it. The image keeps waiting
		// like any undisplayed one, so lowering the limit to 0 releases it after the next pass.
		using var _ = DisableBitmapImageCache();
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Source = bitmap };
			await AddAndWaitForOpened(panel, image);
			await WindowHelper.WaitForIdle();
			var previous = GetSurface(bitmap);

			bitmap.UriSource = server.Uri;
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the replacement is downloading");
			var other = new Image { Source = bitmap };
			panel.Children.Add(other);
			await UITestHelper.WaitForLoaded(other, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			panel.Children.Remove(image);
			await WindowHelper.WaitForIdle();
			bitmap.UriSource = null;
			await WindowHelper.WaitForIdle();

			panel.Children.Add(image);
			await UITestHelper.WaitForLoaded(image, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();
			Assert.IsNotNull(previous.Image, "Pre-condition: the previous image waits within the delay");
			Assert.AreEqual(0, image.ActualWidth, "The Image back after the source was cleared must not show the previous image");

			FeatureConfiguration.Image.MaxUndisplayedImageBytes = 0;
			panel.Children.Add(new Border { Width = 10, Height = 10 });
			await WindowHelper.WaitForIdle();

			Assert.IsNull(previous.Image, "The Image back after the source was cleared must not keep the previous image");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(false, DisplayName = "Cleared")]
	[DataRow(true, DisplayName = "Failed")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Cached_Image_Returns_After_Source_Settled_Then_Shows_Nothing(bool fails)
	{
		// Two Images share a cached bitmap, and the source is cleared or fails while one of them is away. The cache keeps
		// the shared pixels alive, so the Image that comes back must stop drawing them, as the other one did. It raises
		// nothing: as on WinUI, an element that attaches to a source that already failed is not told about it.
		using var cache = EnableBitmapImageCache();

		try
		{
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Source = bitmap };
			var other = new Image { Source = bitmap };
			await AddAndWaitForOpened(panel, image);
			await AddAndWaitForOpened(panel, other);
			await WindowHelper.WaitForIdle();

			panel.Children.Remove(image);
			await WindowHelper.WaitForIdle();

			var otherFailed = 0;
			other.ImageFailed += (_, _) => otherFailed++;
			bitmap.UriSource = fails ? new Uri("ms-appx:///Assets/does-not-exist.png") : null;
			await WindowHelper.WaitFor(() => fails ? otherFailed == 1 : other.ActualWidth == 0, 10000, "Pre-condition: the other Image has the settled source");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(0, other.ActualWidth, "Pre-condition: the other Image shows nothing");

			var raised = 0;
			image.ImageOpened += (_, _) => raised++;
			image.ImageFailed += (_, _) => raised++;
			panel.Children.Add(image);
			await UITestHelper.WaitForLoaded(image, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, image.ActualWidth, "The Image back in the tree must not show the image its source no longer has");
			Assert.AreEqual(0, raised, "The Image back in the tree raises nothing for the settled source");
			Assert.AreEqual(fails ? 1 : 0, otherFailed, "The other Image raised ImageFailed once, when the source failed");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Moved_Between_Delivery_And_Measure_Then_Image_Keeps_Drawing_Its_Pixels(bool cacheEnabled)
	{
		// Two Images draw a bitmap given a second image. In the next pass, before they are measured, a slow third one is
		// started and the first Image moved, so that Image drops the second image and still draws the first, which must stay
		// for it until the third arrives, although every subscriber was given the second.
		using var cache = SetBitmapImageCache(cacheEnabled);
		using var limit = ReleaseAfterLayoutPass();

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var hook = new MeasureHook();
			var first = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
			var second = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
			await UITestHelper.Load(new StackPanel { Children = { hook, first, second } }, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var image = new Image { Source = bitmap };
			var other = new Image { Source = bitmap };
			await AddAndWaitForOpened(first, image);
			await AddAndWaitForOpened(second, other);
			await WindowHelper.WaitForIdle();
			var previous = GetSurface(bitmap);
			Assert.AreEqual(100, image.ActualWidth, 1, "Pre-condition: the Image draws the first image");

			// The brush, notified after the Images, has the hook measured first in the next pass.
			var brush = new ImageBrush { ImageSource = bitmap };
			var armed = false;
			var moved = false;
			brush.ImageOpened += (_, _) =>
			{
				armed = true;
				hook.InvalidateMeasure();
			};
			hook.Measuring = () =>
			{
				if (armed && !moved)
				{
					moved = true;
					bitmap.UriSource = server.Uri;
					first.Children.Remove(image);
					second.Children.Add(image);
				}
			};

			bitmap.UriSource = new Uri(SquareAssetUri);
			await WindowHelper.WaitFor(() => moved, 10000, "Pre-condition: the second image is given and the Image moved before it is measured");
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(40, other.ActualWidth, 1, "Pre-condition: the other Image draws the second image");
			Assert.AreEqual(100, image.ActualWidth, 1, "The moved Image still draws the first image, which must stay until the third arrives");
			Assert.IsNotNull(previous.Image, "The first image stays while the moved Image draws it");

			var opened = TrackOpened(image);
			server.ReleaseResponses();
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(40, image.ActualWidth, 1, "The moved Image draws the third image");
			if (!cacheEnabled)
			{
				Assert.IsNull(previous.Image, "Once the moved Image draws the third image, the first is released");
			}

			GC.KeepAlive(brush);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25128")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Unloaded_Between_Delivery_And_Measure_Then_Drawn_Pixels_Wait_For_It(bool useCache)
	{
		// As above, but the first Image leaves for a while: it never drew the second image, so the first one, which it still
		// draws off the tree, waits like any undisplayed image once the other Image draws the second, and the Image back in
		// the tree before the third arrives draws it again, whether the first image is owned or shared.
		using var _ = SetBitmapImageCache(useCache);
		using var limit = SetMaxUndisplayedImageBytes(long.MaxValue);
		using var delay = SetReleaseDelay(TimeSpan.FromSeconds(10));

		try
		{
			using var server = await GatedImageServer.StartAsync();
			var hook = new MeasureHook();
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
			await UITestHelper.Load(new StackPanel { Children = { hook, panel } }, x => x.IsLoaded);

			// With the cache on, a decode width of its own gives each image its own cache key.
			var bitmap = new BitmapImage { DecodePixelWidth = useCache ? 237 : 0, UriSource = new Uri(AssetUri) };
			var image = new Image { Source = bitmap };
			var other = new Image { Source = bitmap };
			await AddAndWaitForOpened(panel, image);
			await AddAndWaitForOpened(panel, other);
			await WindowHelper.WaitForIdle();
			var previous = GetSurface(bitmap);

			var brush = new ImageBrush { ImageSource = bitmap };
			var armed = false;
			var left = false;
			brush.ImageOpened += (_, _) =>
			{
				armed = true;
				hook.InvalidateMeasure();
			};
			hook.Measuring = () =>
			{
				if (armed && !left)
				{
					left = true;
					bitmap.UriSource = server.Uri;
					panel.Children.Remove(image);
				}
			};

			bitmap.UriSource = new Uri(SquareAssetUri);
			await WindowHelper.WaitFor(() => left, 10000, "Pre-condition: the second image is given and the Image left before it is measured");
			await WindowHelper.WaitFor(() => server.RequestCount >= 1, 5000, "Pre-condition: the third image is downloading");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(40, other.ActualWidth, 1, "Pre-condition: the other Image draws the second image");
			Assert.IsNotNull(previous.Image, "The Image left drawing the first image, which waits for it within the delay");

			panel.Children.Add(image);
			await UITestHelper.WaitForLoaded(image, x => x.IsLoaded);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(100, image.ActualWidth, 1, "Back in the tree, the Image still draws the first image");

			var opened = TrackOpened(image);
			server.ReleaseResponses();
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(40, image.ActualWidth, 1, "The Image draws the third image");
			if (useCache)
			{
				// A shared decode nothing draws waits like any undisplayed one, here until the limit is lowered.
				Assert.IsNotNull(previous.Image, "Pre-condition: the shared first image waits within the delay");
				FeatureConfiguration.Image.MaxUndisplayedImageBytes = 0;
				panel.InvalidateMeasure();
				await WindowHelper.WaitFor(() => previous.Image is null, 5000, "Once nothing draws the shared first image, it is released");
			}
			else
			{
				Assert.IsNull(previous.Image, "Once the Image draws the third image, the first is released after the pass");
			}

			GC.KeepAlive(brush);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Many_Images_Share_A_Replaced_Source_Then_Previous_Image_Kept_Until_Last_Draws_Replacement()
	{
		// Fifty Images and a collapsed one draw a bitmap whose UriSource changes, and one more Image leaves while the
		// replacement loads. The previous image stays while any of them still draws it: the collapsed Image, not measured,
		// keeps it, and once it is shown and draws the replacement, the previous image is released after the pass.
		using var _ = DisableBitmapImageCache();
		using var limit = ReleaseAfterLayoutPass();

		try
		{
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Height = 40 };
			await UITestHelper.Load(panel, x => x.IsLoaded);

			var bitmap = new BitmapImage(new Uri(AssetUri));
			var images = Enumerable.Range(0, 50).Select(_ => new Image { Source = bitmap }).ToArray();
			var collapsed = new Image { Source = bitmap };
			var leaving = new Image { Source = bitmap };
			foreach (var image in images.Concat(new[] { collapsed, leaving }))
			{
				panel.Children.Add(image);
			}

			await WindowHelper.WaitFor(() => images.Concat(new[] { collapsed, leaving }).All(i => i.ActualWidth > 99), 10000, "Pre-condition: every Image draws the first image");
			var previous = GetSurface(bitmap);
			collapsed.Visibility = Visibility.Collapsed;
			await WindowHelper.WaitForIdle();

			bitmap.UriSource = new Uri(TallAssetUri);
			panel.Children.Remove(leaving);
			await WindowHelper.WaitFor(() => images.All(i => i.ActualWidth is > 0 and < 99), 10000, "Pre-condition: the shown Images draw the second image");
			await WindowHelper.WaitForIdle();

			Assert.IsNotNull(previous.Image, "The collapsed Image still draws the first image, which must stay");

			collapsed.Visibility = Visibility.Visible;
			await WindowHelper.WaitFor(() => collapsed.ActualWidth is > 0 and < 99, 10000, "Pre-condition: the collapsed Image, shown, draws the second image");
			await WindowHelper.WaitForIdle();

			Assert.IsNull(previous.Image, "Once the last Image draws the second image, the first is released");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Stream_Set_Again_From_Its_Completion_Then_Previous_Frames_Released()
	{
		// A preview that sets the next frame as soon as SetSourceAsync completes runs inside the notification of the
		// frame it awaited: each change is made by the last subscriber, so the frames shown before must still go.
		var square = await ReadAssetAsync(SquareAssetUri);
		var tall = await ReadAssetAsync(TallAssetUri);
		using var delay = ReleaseAfterLayoutPass();
		using var decoder = TrackingDecoder.Install();

		try
		{
			var bitmap = new BitmapImage();
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			// Measured between frames, without awaiting anything else, so the loop keeps resuming inside the notifications.
			var mostKept = 0;
			for (var i = 0; i < 20; i++)
			{
				await bitmap.SetSourceAsync(new MemoryStream(i % 2 == 0 ? square : tall));
				mostKept = Math.Max(mostKept, decoder.All.Count(d => !d.IsReleased));
			}

			await WindowHelper.WaitFor(() => bitmap.IsOpened, 10000, "Pre-condition: the last frame is decoded");
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(mostKept <= 4, $"Only the frames being shown and replaced may be kept, not {mostKept}");
			Assert.IsTrue(decoder.All.Count(d => !d.IsReleased) <= 2, "Once the frames stop, only the last one is kept");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public async Task When_Source_Changed_While_Displayed_Then_Previous_Pixels_Kept_Until_New_Shown()
	{
		// A source that changes while displayed keeps showing its previous pixels until the new ones are decoded, as on
		// WinUI, so a live source (camera frames, a refreshed preview) does not flicker.
		var square = await ReadAssetAsync(SquareAssetUri);
		var tall = await ReadAssetAsync(TallAssetUri);

		try
		{
			var bitmap = new BitmapImage();
			var image = new Image { Width = 100, Height = 40, Source = bitmap };
			await UITestHelper.Load(image);

			var opened = TrackOpened(image);
			await bitmap.SetSourceAsync(new MemoryStream(square));
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			var first = GetSurface(bitmap);

			// SetSourceAsync completes on the data the source already has, so wait for the Image to open the new stream.
			opened = TrackOpened(image);
			var second = bitmap.SetSourceAsync(new MemoryStream(tall));
			Assert.IsNotNull(first.Image, "The Image keeps showing the previous pixels while the new ones are decoded");

			await second.WaitAsync(TimeSpan.FromSeconds(10));
			await opened.WaitAsync(TimeSpan.FromSeconds(10));
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(150, GetSurface(bitmap)?.Image?.PixelHeight);
			Assert.IsNull(first.Image, "The previous pixels are released once the Image shows the new ones");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public void When_Listener_Unsubscribes_Another_During_Notification_Then_It_Is_Not_Called()
	{
		// WriteableBitmap opens synchronously, so its subscribers are notified inside Invalidate.
		var bitmap = new WriteableBitmap(4, 4);
		var armed = false;
		IDisposable second = null;
		var secondCalls = 0;

		using var first = bitmap.Subscribe(_ =>
		{
			if (armed)
			{
				armed = false;
				second.Dispose();
			}
		});
		second = bitmap.Subscribe(_ => secondCalls++);
		secondCalls = 0;

		armed = true;
		bitmap.Invalidate();

		Assert.AreEqual(0, secondCalls, "A listener unsubscribed by an earlier one during the notification must not be called");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25114")]
	public void When_Listener_Changes_Source_During_Notification_Then_Stale_Data_Not_Delivered()
	{
		var bitmap = new WriteableBitmap(4, 4);
		var armed = false;
		var secondCalls = 0;

		// The first listener changes the source while it is notified: the new data reaches every listener, and the data it
		// replaced (released by the change) must not be delivered after it.
		using var first = bitmap.Subscribe(_ =>
		{
			if (armed)
			{
				armed = false;
				bitmap.Invalidate();
			}
		});
		using var second = bitmap.Subscribe(_ => secondCalls++);
		secondCalls = 0;

		armed = true;
		bitmap.Invalidate();

		Assert.AreEqual(1, secondCalls, "The second listener must get the new data once, and not the replaced data after it");
	}

	// A limit of 0 releases what nothing displays after the layout pass.
	private static IDisposable ReleaseAfterLayoutPass() => SetMaxUndisplayedImageBytes(0);

	private static IDisposable SetMaxUndisplayedImageBytes(long bytes)
	{
		var previous = FeatureConfiguration.Image.MaxUndisplayedImageBytes;
		FeatureConfiguration.Image.MaxUndisplayedImageBytes = bytes;
		return Disposable.Create(() => FeatureConfiguration.Image.MaxUndisplayedImageBytes = previous);
	}

	private static IDisposable SetMaxBitmapImageCacheBytes(long bytes)
	{
		var previous = FeatureConfiguration.Image.MaxBitmapImageCacheBytes;
		FeatureConfiguration.Image.MaxBitmapImageCacheBytes = bytes;
		return Disposable.Create(() => FeatureConfiguration.Image.MaxBitmapImageCacheBytes = previous);
	}

	private static IDisposable SetEncodedIdleExpiry(TimeSpan expiry)
	{
		var previous = Uno.Helpers.EncodedImageCache.IdleExpiry;
		Uno.Helpers.EncodedImageCache.IdleExpiry = expiry;
		return Disposable.Create(() => Uno.Helpers.EncodedImageCache.IdleExpiry = previous);
	}

	private static IDisposable SetReleaseDelay(TimeSpan delay)
	{
		var previous = PendingImageReleases.ReleaseDelay;
		PendingImageReleases.ReleaseDelay = delay;
		return Disposable.Create(() => PendingImageReleases.ReleaseDelay = previous);
	}

	private static IDisposable DisableBitmapImageCache() => SetBitmapImageCache(false);

	private static IDisposable EnableBitmapImageCache() => SetBitmapImageCache(true);

	private static IDisposable SetBitmapImageCache(bool enabled)
	{
		var wasEnabled = FeatureConfiguration.Image.EnableBitmapImageCache;
		FeatureConfiguration.Image.EnableBitmapImageCache = enabled;
		return Disposable.Create(() => FeatureConfiguration.Image.EnableBitmapImageCache = wasEnabled);
	}

	// Not inlined, and returning a bool, so the caller keeps no reference to the cached load.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<bool> IsCached(Uri uri, int decodePixelWidth, int decodePixelHeight)
		=> await BitmapImage.GetCachedImageDataTaskForTesting(uri, decodePixelWidth, decodePixelHeight) is not null;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference> OpenThroughUnusedBrush(Uri uri, int decodePixelWidth, int decodePixelHeight)
	{
		// The brush opens its source as soon as it has one, in the tree or not.
		var bitmap = NewBitmap(uri.OriginalString, decodePixelWidth, decodePixelHeight);
		var opened = TrackOpen(bitmap);
		var brush = new ImageBrush { ImageSource = bitmap };
		Assert.IsTrue(await opened.WaitAsync(TimeSpan.FromSeconds(10)), "Pre-condition: the brush opens its source");
		GC.KeepAlive(brush);
		return new WeakReference(bitmap);
	}

	private static BitmapImage NewBitmap(string uri, int decodePixelWidth, int decodePixelHeight)
		=> new() { DecodePixelWidth = decodePixelWidth, DecodePixelHeight = decodePixelHeight, UriSource = new Uri(uri) };

	// The surface the source delivers to its subscribers; call only while something displays the source, as the probe's
	// own subscription would otherwise open it.
	private static CompositionImageSurface GetSurface(ImageSource source)
	{
		CompositionImageSurface surface = null;
		source.Subscribe(data => surface = data.CompositionSurface as CompositionImageSurface).Dispose();
		return surface;
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

	private static async Task<ImageSource> CreateBitmapSourceAsync()
	{
		var bitmap = new BitmapImage();
		await bitmap.SetSourceAsync(new MemoryStream(await ReadAssetAsync(SquareAssetUri)));
		return bitmap;
	}

	private static async Task<ImageSource> CreateSvgSourceAsync()
	{
		const string markup = "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><rect width='100' height='100' fill='red'/></svg>";
		var svg = new SvgImageSource();
		var status = await svg.SetSourceAsync(new MemoryStream(Encoding.UTF8.GetBytes(markup)).AsRandomAccessStream());
		Assert.AreEqual(SvgImageSourceLoadStatus.Success, status, "Pre-condition: the SVG opens");
		return svg;
	}

	private static async Task<ImageSource> CreateSharedSourceAsync(SharedSource kind) => kind switch
	{
		SharedSource.Svg => await CreateSvgSourceAsync(),
		SharedSource.CachedUri => new BitmapImage(new Uri(SquareAssetUri)),
		_ => await CreateBitmapSourceAsync(),
	};

	// Not inlined, so that no local of the caller keeps the Images alive.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<WeakReference[]> ShowThenUnloadAsync(ImageSource source)
	{
		var panel = new StackPanel();
		await UITestHelper.Load(panel, x => x.IsLoaded);

		var images = new[]
		{
			new Image { Width = 100, Height = 100, Source = source },
			new Image { Width = 100, Height = 100, Source = source },
		};

		foreach (var image in images)
		{
			await AddAndWaitForOpened(panel, image);
		}

		WindowHelper.WindowContent = null;
		await WindowHelper.WaitForIdle();
		Assert.IsFalse(images.Any(image => image.IsLoaded), "Pre-condition: the Images left the tree");

		return images.Select(image => new WeakReference(image)).ToArray();
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
	/// Wraps the registered image codec for the duration of a test and records every image it produces, so a test can
	/// tell whether the pixels of one particular decode were released.
	/// </summary>
	private sealed class TrackingDecoder : IImageEncoderDecoder, IDisposable
	{
		private readonly IImageEncoderDecoder _inner;
		private readonly ConcurrentQueue<TrackedImage> _images = new();

		private TrackingDecoder(IImageEncoderDecoder inner) => _inner = inner;

		public static TrackingDecoder Install()
		{
			var decoder = new TrackingDecoder(ImageEncoderDecoder.Current);
			ImageEncoderDecoder.Current = decoder;
			return decoder;
		}

		public void Dispose() => ImageEncoderDecoder.Current = _inner;

		public TrackedImage[] Decoded(int pixelWidth, int pixelHeight)
			=> _images.Where(i => i.PixelWidth == pixelWidth && i.PixelHeight == pixelHeight).ToArray();

		public int Count(int pixelWidth, int pixelHeight) => Decoded(pixelWidth, pixelHeight).Length;

		public TrackedImage[] All => _images.ToArray();

		public bool TryDecode(Stream stream, int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageFrames frames)
		{
			if (!_inner.TryDecode(stream, targetWidth, targetHeight, out var decoded))
			{
				frames = null;
				return false;
			}

			frames = new ImageFrames(decoded.Frames.Select(Track).ToArray(), decoded.DurationsMs);
			return true;
		}

		public IImage CreateImage(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul)
			=> Track(_inner.CreateImage(pixelWidth, pixelHeight, bgraPremul));

		public ImageFrames CreateFrames(IImage image) => _inner.CreateFrames(image);

		public void Encode(Stream destination, byte[] pixels, int width, int height, BitmapPixelFormat pixelFormat, BitmapAlphaMode alphaMode, BitmapEncoderFormat format, int quality)
			=> _inner.Encode(destination, pixels, width, height, pixelFormat, alphaMode, format, quality);

		private IImage Track(IImage image)
		{
			var tracked = new TrackedImage(image);
			_images.Enqueue(tracked);
			return tracked;
		}
	}

	private sealed class MeasureHook : FrameworkElement
	{
		public Action Measuring { get; set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			Measuring?.Invoke();
			return default;
		}
	}

	private sealed class TrackedImage : DrawingResource, IImage
	{
		private readonly IImage _inner;
		private volatile bool _isReleased;

		public TrackedImage(IImage inner)
		{
			_inner = inner;

			// Read once: the inner image can't be queried once it is released.
			PixelWidth = inner.PixelWidth;
			PixelHeight = inner.PixelHeight;
		}

		public bool IsReleased => _isReleased;

		public int PixelWidth { get; }

		public int PixelHeight { get; }

		public void CopyPixels(Span<byte> destination) => _inner.CopyPixels(destination);

		protected override void Free()
		{
			_inner.Dispose();
			_isReleased = true;
		}
	}

	/// <summary>
	/// An image served over http with an optional response header, and the number of times it was requested: by a loopback
	/// server on desktop, and on WebAssembly, where a test cannot listen, by a stand-in for fetch that answers its own URL
	/// (the browser HttpClient calls globalThis.fetch).
	/// </summary>
	private sealed class ServedImage : IDisposable
	{
		private readonly Func<int> _requestCount;
		private readonly Action _stop;

		private ServedImage(Uri uri, int length, Func<int> requestCount, Action stop)
		{
			Uri = uri;
			Length = length;
			_requestCount = requestCount;
			_stop = stop;
		}

		public static async Task<ServedImage> StartAsync(string header = null, string status = "200 OK")
		{
			var body = await ReadAssetAsync(SquareAssetUri);

			if (OperatingSystem.IsBrowser())
			{
				var (name, value) = header is { Length: > 0 } ? (header[..header.IndexOf(':')], header[(header.IndexOf(':') + 1)..].Trim()) : ("", "");
				var url = InvokeJs($$"""
					const served = globalThis.__unoServedImages ??= new Map();
					if (!globalThis.__unoServedImagesFetch) {
						const inner = globalThis.__unoServedImagesFetch = globalThis.fetch;
						globalThis.fetch = (input, init) => {
							const image = served.get(typeof input === 'string' ? input : input.url);
							if (!image) {
								return inner(input, init);
							}
							image.requests++;
							return Promise.resolve(new Response(image.body, { status: image.status, headers: image.headers }));
						};
					}
					const url = new URL('/__served-images/{{Guid.NewGuid():N}}/square100.png', document.baseURI).href;
					const headers = { 'Content-Type': 'image/png' };
					if ('{{name}}') {
						headers['{{name}}'] = '{{value}}';
					}
					served.set(url, { body: Uint8Array.from(atob('{{Convert.ToBase64String(body)}}'), c => c.charCodeAt(0)), headers, status: {{status[..3]}}, requests: 0 });
					return url;
					""");

				return new ServedImage(
					new Uri(url),
					body.Length,
					() => int.Parse(InvokeJs($"return String(globalThis.__unoServedImages.get('{url}').requests);"), CultureInfo.InvariantCulture),
					() => InvokeJs($$"""
						globalThis.__unoServedImages.delete('{{url}}');
						if (globalThis.__unoServedImages.size === 0) {
							globalThis.fetch = globalThis.__unoServedImagesFetch;
							delete globalThis.__unoServedImagesFetch;
						}
						return '';
						"""));
			}

			var server = await GatedImageServer.StartAsync();
			server.ExtraHeader = header;
			server.Status = status;
			server.ReleaseResponses();
			return new ServedImage(server.Uri, body.Length, () => server.RequestCount, server.Dispose);
		}

		public Uri Uri { get; }

		public int Length { get; }

		public int RequestCount => _requestCount();

		public void Dispose() => _stop();

		private static string InvokeJs(string body)
			=> Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper.InvokeBrowserJs($"(function(){{ {body} }})()");
	}
}
#endif
