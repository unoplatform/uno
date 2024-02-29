#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using AndroidX.Fragment.App;
using Uno.UI;

namespace Uno.Helpers.Activities
{
	[Activity(
				Theme = "@style/Theme.AppCompat.Translucent",
				ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize
			)]
	internal class AwaitableResultActivity : FragmentActivity
	{
		private static event Action<AwaitableResultActivity>? Resumed;

		// Some devices (Galaxy S4) use a second activity to get the result.
		// This dictionary is used to keep track of the original activities that requests the values.
		private Dictionary<int, AwaitableResultActivity> _originalActivities = new Dictionary<int, AwaitableResultActivity>();

		private TaskCompletionSource<ActivityResult> _resultCompletionSource = new TaskCompletionSource<ActivityResult>();
		private int _requestCode;

		internal static async Task<AwaitableResultActivity> StartAsync(CancellationToken cancellationToken = default)
		{
			if (ContextHelper.Current == null)
			{
				throw new InvalidOperationException("ContextHelper.Current is null, API called too early in the applicaiton lifecycle.");
			}

			var taskCompletionSource = new TaskCompletionSource<AwaitableResultActivity>();

			void handler(AwaitableResultActivity instance) => taskCompletionSource.TrySetResult(instance);

			try
			{
				using (cancellationToken.Register(() => taskCompletionSource.TrySetCanceled()))
				{
					Resumed += handler;

					ContextHelper.Current.StartActivity(typeof(AwaitableResultActivity));

					return await taskCompletionSource.Task;
				}
			}
			finally
			{
				Resumed -= handler;
			}
		}

		internal async Task<ActivityResult> StartActivityForResultAsync(Intent intent, int requestCode = 0, CancellationToken cacellationToken = default)
		{
			try
			{
				_originalActivities[requestCode] = this;
				_requestCode = requestCode;

				StartActivityForResult(intent, requestCode);

				using (cacellationToken.Register(() => _resultCompletionSource.TrySetCanceled()))
				{
					return await _resultCompletionSource.Task;
				}
			}
			finally
			{
				// Close the activity
				Finish();

				_originalActivities.Remove(requestCode);
			}
		}

		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent? intent)
		{
			base.OnActivityResult(requestCode, resultCode, intent);

			// Some devices (Galaxy S4) use a second activity to get the result.
			// In this case the current instance is not the same as the one that requested the value.
			// In this case we must get the original activity and use it's TaskCompletionSource instead of ours.
			var isCurrentActivityANewOne = false;

			if (_originalActivities.TryGetValue(requestCode, out var originalActivity))
			{
				isCurrentActivityANewOne = originalActivity != this;
			}
			else
			{
				originalActivity = this;
			}

			if (isCurrentActivityANewOne)
			{
				// Close this activity (because we are not the original)
				Finish();
			}

			// Push a new request code in the calling activity
			if (originalActivity._requestCode == requestCode)
			{
				originalActivity._resultCompletionSource.TrySetResult(new ActivityResult(_requestCode, resultCode, intent));
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			_resultCompletionSource?.TrySetResult(new ActivityResult(_requestCode, Result.Canceled, null));
		}

		protected override void OnResume()
		{
			base.OnResume();

			Resumed?.Invoke(this);
		}
	}
}
#endif
