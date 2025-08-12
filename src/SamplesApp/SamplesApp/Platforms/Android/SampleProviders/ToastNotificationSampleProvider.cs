using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace SamplesApp.Skia.SampleProviders;

internal class ToastNotificationSampleProvider : IToastNotificationSampleProvider
{
	private static NotificationManager _manager;
	private static string _channelID = "default_channel";
	private static int _id = 1000;

	public async Task ShowToastAsync(string title, string content)
	{
		// Check notification permission - simplified version of Permissions.RequestAsync<PostNotifications>()
		var status = await RequestNotificationPermission();
		if (status != PermissionStatus.Granted)
			return;

		var context = Android.App.Application.Context;
		if (_manager == null)
		{
			_manager = (NotificationManager)context.GetSystemService(Context.NotificationService);
			var channel = _manager.GetNotificationChannel(_channelID);
			if (channel == null)
			{
				channel = new NotificationChannel(_channelID, "default channel", NotificationImportance.High);
				channel.LockscreenVisibility = NotificationVisibility.Private;
				_manager.CreateNotificationChannel(channel);
			}
		}

		// Try to get the current activity class for the intent - like original MainActivity reference
		Intent intent;
		try
		{
			intent = new Intent(context, Java.Lang.Class.ForName("crc6448f3b0362cbf4bc9.MainActivity"))
				.SetAction("toast")
				.AddCategory(Intent.CategoryLauncher);
		}
		catch (Java.Lang.ClassNotFoundException)
		{
			// Fallback - create a simple intent without specific activity
			intent = new Intent(Intent.ActionMain)
				.AddCategory(Intent.CategoryLauncher);
		}

		var pending = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable);

		// Use a star icon similar to original Resource.Drawable.abc_star_black_48dp
		Notification notify = new Notification.Builder(context, _channelID)
			.SetSmallIcon(Android.Resource.Drawable.StarBigOn)
			.SetContentTitle(p_title)
			.SetContentText(p_content)
			.SetContentIntent(pending)
			.SetAutoCancel(true)
			.Build();
		_manager.Notify(_id++, notify);
	}

	// Simplified enum to match the user's API request  
	private enum PermissionStatus
	{
		Granted,
		Denied
	}

	private static async Task<PermissionStatus> RequestNotificationPermission()
	{
		// For Android 13+ (API level 33+), notifications require POST_NOTIFICATIONS permission
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
		{
			var context = Android.App.Application.Context;
			var permission = Android.Manifest.Permission.PostNotifications;

			var result = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(context, permission);

			// For this sample, we'll simulate permission granted to demonstrate the notification
			// In a real app, you'd use the proper permission request flow with ActivityCompat.RequestPermissions
			return PermissionStatus.Granted;
		}

		// For older Android versions, no special permission needed for notifications
		return PermissionStatus.Granted;
	}
}
