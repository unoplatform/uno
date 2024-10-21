using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Print;
using Uno.Foundation.Logging;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using Windows.Services.Store.Internal;
using Xamarin.Google.Android.Play.Core.Review;
using Xamarin.Google.Android.Play.Core.Review.Testing;
using Xamarin.Google.Android.Play.Core.Tasks;

namespace Uno.UI.GooglePlay;

public class StoreContextExtension : IStoreContextExtension
{
	private readonly StoreContext _storeContext;

	public StoreContextExtension(object storeContext)
	{
		_storeContext = storeContext as StoreContext ?? throw new ArgumentNullException(nameof(storeContext));
	}

	public async Task<StoreRateAndReviewResult> RequestRateAndReviewAsync(CancellationToken token)
	{
		TaskCompletionSource<StoreRateAndReviewResult>? inAppRateTcs;

		inAppRateTcs = new();

		using var reviewManager = WinRTFeatureConfiguration.StoreContext.TestMode
			? new FakeReviewManager(Application.Context)
			: ReviewManagerFactory.Create(Application.Context);

		using var request = reviewManager.RequestReviewFlow();

		using var listener = new InAppReviewListener(reviewManager, inAppRateTcs);
		request.AddOnCompleteListener(listener);

		return await inAppRateTcs.Task;
	}

	private class InAppReviewListener : Java.Lang.Object, IOnCompleteListener
	{
		private IReviewManager _reviewManager;
		private TaskCompletionSource<StoreRateAndReviewResult>? _inAppRateTcs;
		private Xamarin.Google.Android.Play.Core.Tasks.Task? _launchTask;
		private bool _forceReturn;

		public InAppReviewListener(IReviewManager reviewManager, TaskCompletionSource<StoreRateAndReviewResult>? inAppRateTcs)
		{
			_reviewManager = reviewManager;
			_inAppRateTcs = inAppRateTcs;
		}

		public void OnComplete(Xamarin.Google.Android.Play.Core.Tasks.Task task)
		{
			var context = ContextHelper.Current;
			var activity = (Activity)context;

			if (!task.IsSuccessful || _forceReturn)
			{
				_inAppRateTcs?.TrySetResult(new(_forceReturn ? StoreRateAndReviewStatus.Succeeded : StoreRateAndReviewStatus.Error));
				_launchTask?.Dispose();

				return;
			}

			try
			{
				var reviewInfo = (ReviewInfo?)task.GetResult(Java.Lang.Class.FromType(typeof(ReviewInfo)));

				_forceReturn = true;
				_launchTask = _reviewManager?.LaunchReviewFlow(activity, reviewInfo!);

				_launchTask?.AddOnCompleteListener(this);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError("There was an error launching in-app review. Please try again. Exception: {0}", ex);
				}

				_inAppRateTcs?.TrySetResult(new(StoreRateAndReviewStatus.Error));

				System.Diagnostics.Debug.WriteLine($"Error message: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"Stacktrace: {ex}");
			}
		}
	}
}
