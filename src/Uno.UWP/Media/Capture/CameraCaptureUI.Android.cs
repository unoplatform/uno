using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using AndroidX.Fragment.App;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Extensions;
using Windows.Foundation;
using Windows.Storage;
using static Android.Content.Res.Resources;

namespace Windows.Media.Capture
{
	public partial class CameraCaptureUI
	{
		private const int CameraRequestCode = 2;

		private async Task<StorageFile?> CaptureFile(CancellationToken ct, CameraCaptureUIMode mode)
		{
			await ValidateRequiredPermissions(ct);

			var mediaPickerActivity = await StartMediaPickerActivity(ct);

			// An intent to take a picture/video
			var action = mode switch
			{
				CameraCaptureUIMode.Photo => MediaStore.ActionImageCapture,
				CameraCaptureUIMode.Video => MediaStore.ActionVideoCapture,
				_ => MediaStore.ActionImageCapture,
			};

			var takePictureIntent = new Intent(action);

			// On some device (like nexus phone), we need to add extra to the intent to be able to get the Uri of the image
			// http://stackoverflow.com/questions/9890757/android-camera-data-intent-returns-null
			var photoUri = mediaPickerActivity.ContentResolver!.Insert(MediaStore.Images.Media.ExternalContentUri!, new ContentValues());
			takePictureIntent.PutExtra(MediaStore.ExtraOutput, photoUri);

			var result = await mediaPickerActivity.GetActivityResult(ct, takePictureIntent, CameraRequestCode);

			if (result.ResultCode != Result.Ok)
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Picture not taken. Result: {result.ResultCode}");
				}

				// No picture return null
				return null;
			}

			return await CreateTempImage(
				ContextHelper.Current.ContentResolver!.OpenInputStream(photoUri!)!,
				Path.GetExtension(photoUri!.Path)!
			);
		}

		private async Task<MediaPickerActivity> StartMediaPickerActivity(CancellationToken ct)
		{
			var tcs = new TaskCompletionSource<MediaPickerActivity>();

			void handler(MediaPickerActivity instance) => tcs.TrySetResult(instance);

			try
			{
				using (ct.Register(() => tcs.TrySetCanceled()))
				{
					MediaPickerActivity.Resumed += handler;

					ContextHelper.Current.StartActivity(typeof(MediaPickerActivity));

					// Wait for the MediaPickerActivity to start
					return await tcs.Task;
				}
			}
			finally
			{
				MediaPickerActivity.Resumed -= handler;
			}
		}

		private async Task ValidateRequiredPermissions(CancellationToken ct)
		{
			if (shouldCheckWriteExternalStoragePermission() &&
				!await PermissionsHelper.TryGetWriteExternalStoragePermission(ct))
			{
				throw new UnauthorizedAccessException("Requires WRITE_EXTERNAL_STORAGE permission on Android 9 and lower.");
			}

			if (!await PermissionsHelper.TryGetCameraPermission(ct))
			{
				throw new UnauthorizedAccessException("Requires CAMERA permission");
			}

			static bool shouldCheckWriteExternalStoragePermission()
			{
				// https://developer.android.com/training/data-storage/shared/media#request-permissions
				// On devices that run Android 10 or higher, you don't need any storage-related permissions to access and modify media files that your app owns
				// If your app is used on a device that runs Android 9 or lower, or if your app has temporarily
				// opted out of scoped storage, you must request the READ_EXTERNAL_STORAGE permission to access any media file.
				// If you want to modify media files, you must request the WRITE_EXTERNAL_STORAGE permission, as well.
				return (int)Build.VERSION.SdkInt <= (int)BuildVersionCodes.P ||
					Android.OS.Environment.IsExternalStorageLegacy;
			}
		}

		[Activity(
			Theme = "@style/Theme.AppCompat.Translucent",

			// This prevents the Activity from being destroyed when the orientation and/or screen size changes.
			// This is important because OnDestroy would otherwise return Result.Canceled before OnActivityResult can return the actual result.
			// Common example: Capture an image in landscape from an app locked to portrait.
			ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize
		)]
		internal class MediaPickerActivity : FragmentActivity
		{
			public static event Action<MediaPickerActivity>? Resumed;

			// Some devices (Galaxy S4) use a second activity to get the result.
			// This dictionary is used to keep track of the original activities that requests the values.
			private static Dictionary<int, MediaPickerActivity> _originalActivities = new Dictionary<int, MediaPickerActivity>();

			private TaskCompletionSource<MediaPickerActivityResult> _resultCompletionSource = new TaskCompletionSource<MediaPickerActivityResult>();
			private int _requestCode;

			internal async Task<MediaPickerActivityResult> GetActivityResult(CancellationToken ct, Intent intent, int requestCode = 0)
			{
				try
				{
					_originalActivities[requestCode] = this;
					_requestCode = requestCode;

					StartActivityForResult(intent, requestCode);

					using (ct.Register(() => _resultCompletionSource.TrySetCanceled()))
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
					originalActivity._resultCompletionSource.TrySetResult(new MediaPickerActivityResult(_requestCode, resultCode, intent));
				}
			}

			protected override void OnDestroy()
			{
				base.OnDestroy();

				// MediaPickerActivity could be destroyed by the system before receiving the result,
				// In such a case, we need to complete the _resultCompletionSource. In the normal flow, OnDestroy will
				// only be called after the _resultCompletionSource's Task has already RanToCompletion
				_resultCompletionSource?.TrySetResult(new MediaPickerActivityResult(_requestCode, Result.Canceled, null));
			}

			protected override void OnResume()
			{
				base.OnResume();

				Resumed?.Invoke(this);
			}
		}

		internal class MediaPickerActivityResult
		{
			public MediaPickerActivityResult(int requestCode, Result resultCode, Intent? intent)
			{
				RequestCode = requestCode;
				ResultCode = resultCode;
				Intent = intent;
			}

			public int RequestCode { get; }

			public Result ResultCode { get; }

			public Intent? Intent { get; }
		}
	}
}
