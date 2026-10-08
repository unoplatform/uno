#if __SKIA__
#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging;

[TestClass]
[RunsOnUIThread]
public class Given_BitmapImage
{
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
}
#endif
