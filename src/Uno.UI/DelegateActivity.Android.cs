using Android.App;
using Android.Content;
using Android.Content.PM;
using Uno.UI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;

namespace Uno.UI
{
	/// <summary>
	///  An invisible (Transparent) Activity that starts an Intent and returns the parameters of the  OnActivityResult
	/// </summary>
	[Activity(
		Theme = "@style/Theme.AppCompat.Translucent",
		// This prevents the Activity from being destroyed when the orientation and/or screen size changes.
		// This is important because OnDestroy would otherwise return Result.Canceled before OnActivityResult can return the actual result.
		// Common example: Capture an image in landscape from an app locked to portrait (using MediaPickerService).
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize
	)]
	public class DelegateActivity : BaseActivity
	{
		// Some devices (Galaxy S4) use a second activity to get the result.
		// This dictionary is used to keep track of the original activities that requests the values.
		private static Dictionary<int, DelegateActivity> _originalActivities = new Dictionary<int, DelegateActivity>();

		private TaskCompletionSource<OnActivityResultArgs> _completionSource = new TaskCompletionSource<OnActivityResultArgs>();

		private int _requestCode;

		/// <summary>
		/// Starts an Intent and waits for the given request code
		/// </summary>
		/// <param name="ct">CancellationToken</param>
		/// <param name="intent">The Intent you want to send</param>
		/// <param name="requestCode">A specific Response code, this is useful if you send more than one type of request in parallel</param>
		/// <returns>OnActivityResultArgs : an object containing the parameters of the  OnActivityResult</returns>
		public async Task<OnActivityResultArgs> GetActivityResult(CancellationToken ct, Intent intent, int requestCode = 0)
		{
			try
			{
				_originalActivities[requestCode] = this;
				_requestCode = requestCode;

				//Start the intent
				StartActivityForResult(intent, requestCode);//Wait for the specific request code

				//the Task that returns when OnActivityResult is called
				using (ct.Register(() => _completionSource.TrySetCanceled()))
				{
					var result = await _completionSource.Task;

					return result;
				}
			}
			finally
			{
				Finish();//Close the activity
				_originalActivities.Remove(requestCode);
			}
		}

		/// <summary>
		/// Called when an intent returns with data
		/// </summary>
		/// <param name="requestCode">The request code issued in StartActivityForResult</param>
		/// <param name="resultCode">OK, Cancelled... </param>
		/// <param name="intent">The Intent Data</param>
		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
		{
			base.OnActivityResult(requestCode, resultCode, intent);

			// Some devices (Galaxy S4) use a second activity to get the result.
			// In this case the current instance is not the same as the one that requested the value.
			// In this case we must get the original activity and use it's TaskCompletionSource instead of ours.
			var isCurrentActivityANewOne = false;
			DelegateActivity originalActivity;
			if (_originalActivities.TryGetValue(requestCode, out originalActivity))
			{
				isCurrentActivityANewOne = originalActivity != this;
			}
			else
			{
				originalActivity = this;
			}

			if (isCurrentActivityANewOne)
			{
				// Finish this activity (because we are not the original)
				Finish();
			}

			// Push a new OnActivityResultArgs in the calling activity
			if (originalActivity._requestCode == requestCode)
			{
				originalActivity._completionSource.TrySetResult(new OnActivityResultArgs(requestCode, resultCode, intent));
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			//DelegateActivity could be destroyed by the system before receiving the result,
			//In such a case, we need to complete the _completionSource. In the normal flow, OnDestroy will
			//only be called after the _completionSource's Task has already RanToCompletion
			_completionSource?.TrySetResult(new OnActivityResultArgs(_requestCode, Result.Canceled, null));
		}
	}
}
