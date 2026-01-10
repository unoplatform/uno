using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Windows.Foundation;
using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;

using Foundation;
using System.Threading.Tasks;
using System.Threading;
using Windows.System;
using Windows.UI.Core;
using CoreFoundation;
using StoreKit;

namespace Windows.Services.Store;

public sealed partial class StoreContext
{
	private static HttpClient _httpClient;

	public IAsyncOperation<StoreProductResult> GetStoreProductForCurrentAppAsync()
	{
		return AsyncOperation.FromTask(async ct =>
		{
			try
			{
				var bundleId = NSBundle.MainBundle.BundleIdentifier;
				var url = $"http://itunes.apple.com/lookup?bundleId={bundleId}";

				_httpClient ??= new HttpClient();
				var json = await _httpClient.GetStringAsync(url);

				var regex = TrackParser();
				var match = regex.Match(json);
				if (!match.Success)
				{
					throw new InvalidOperationException("Failed to retrieve trackId from bundleId.");
				}

				var storeId = match.Groups[1].Value;

				return new StoreProductResult
				{
					Product = new StoreProduct
					{
						StoreId = storeId,
						LinkUri = new Uri($"https://itunes.apple.com/app/id{storeId}")
					}
				};
			}
			catch (Exception exception)
			{
				return new StoreProductResult
				{
					ExtendedError = exception
				};
			}
		});
	}

	[GeneratedRegex("trackId[^0-9]*([0-9]*)")]
	private static partial Regex TrackParser();

#if __IOS__
	private async Task<StoreRateAndReviewResult> RequestRateAndReviewAppTaskAsync(CancellationToken cancellationToken)
	{
		try
		{
			await CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var windowScene = GetActiveWindowScene();
				if (windowScene is null)
				{
					throw new InvalidOperationException("Unable to find an active window scene.");
				}

				SKStoreReviewController.RequestReview(windowScene);
			});

			return new StoreRateAndReviewResult(StoreRateAndReviewStatus.Succeeded);
		}
		catch (Exception ex)
		{
			return new StoreRateAndReviewResult(StoreRateAndReviewStatus.Error, ex);
		}
	}

	private static UIWindowScene GetActiveWindowScene()
	{
		if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
		{
			var scenes = UIApplication.SharedApplication.ConnectedScenes;
			var activeScene = scenes.FirstOrDefault(scene => scene.ActivationState == UISceneActivationState.ForegroundActive);
			return activeScene as UIWindowScene;
		}
		else
		{
#pragma warning disable CA1422 // KeyWindow is deprecated in iOS 13+
			return UIApplication.SharedApplication.KeyWindow?.WindowScene;
#pragma warning restore CA1422
		}
	}
#endif
}
