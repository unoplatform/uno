#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Uno.UI;

namespace Uno
{

	[Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
	public class AsyncActivity : Activity
	{
		// Some devices (Galaxy S4) use a second activity to get the result.
		// This dictionary is used to keep track of the original activities that requests the values.
		private static readonly Dictionary<int, AsyncActivity> _originalActivities = new Dictionary<int, AsyncActivity>();
		private readonly TaskCompletionSource<OnActivityResultArgs> _completionSource = new TaskCompletionSource<OnActivityResultArgs>();
		private static event Action<AsyncActivity> Handler;

		public int RequestCode { get; private set; }

		public AsyncActivity() { }

		protected override void OnResume()
		{
			base.OnResume();

			Handler?.Invoke(this);
		}

		public static async Task<T> InitialiseActivity<T>(Intent intent, int requestCode = 0) where T : AsyncActivity
		{
			var finished = new TaskCompletionSource<AsyncActivity>();

			var taskCompletionSource = new TaskCompletionSource<AsyncActivity>();
			void handler(AsyncActivity instance) => taskCompletionSource.TrySetResult(instance);

			try
			{
				Handler += handler;
				ContextHelper.Current.StartActivity(typeof(T));

				var result = await taskCompletionSource.Task as T;
				result.RequestCode = requestCode;
				result.Intent = intent;
				return result;
			}
			finally
			{
				Handler -= handler;
			}
		}

		public async Task<OnActivityResultArgs> Start()
		{
			try
			{
				_originalActivities[RequestCode] = this;

				StartActivityForResult(Intent, RequestCode);

				//the Task that returns when OnActivityResult is called
				var result = await _completionSource.Task;
				return result;
			}
			finally
			{
				Finish();//Close the activity
				_originalActivities.Remove(RequestCode);
			}
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent intent)
		{
			base.OnActivityResult(requestCode, resultCode, intent);

			// Some devices (Galaxy S4) use a second activity to get the result.
			// In this case the current instance is not the same as the one that requested the value.
			// In this case we must get the original activity and use it's TaskCompletionSource instead of ours.
			var currentActivityIsANewOne = false;
			if (_originalActivities.TryGetValue(requestCode, out var originalActivity))
			{
				currentActivityIsANewOne = originalActivity != this;
			}
			else
			{
				originalActivity = this;
			}

			if (currentActivityIsANewOne)
			{
				// Finish this activity (because we are not the original)
				Finish();
			}

			// Push a new OnActivityResultArgs in the calling activity
			if (originalActivity.RequestCode == requestCode)
			{
				originalActivity._completionSource.TrySetResult(new OnActivityResultArgs(requestCode, resultCode, intent));
			}
		}
		protected override void OnDestroy()
		{
			base.OnDestroy();

			//AsyncActivity could be destroyed by the system before receiving the result,
			//In such a case, we need to complete the _completionSource. In the normal flow, OnDestroy will
			//only be called after the _completionSource's Task has already RanToCompletion
			_completionSource?.TrySetResult(new OnActivityResultArgs(RequestCode, Result.Canceled, null));
		}
	}
}
#endif
