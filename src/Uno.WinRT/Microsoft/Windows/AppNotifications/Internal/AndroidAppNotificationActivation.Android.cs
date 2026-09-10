#nullable enable

using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AndroidAppNotificationActivation
{
	private const string IntentAction = "uno.appnotifications.ACTIVATE";
	private const string ArgumentExtra = "uno.appnotifications.argument";
	private const string InputIdExtra = "uno.appnotifications.inputId";

	public static bool TryHandleIntent(Intent? intent)
	{
		if (intent?.Action != IntentAction)
		{
			return false;
		}

		var userInput = new Dictionary<string, string>();
		var inputId = intent.GetStringExtra(InputIdExtra);
		if (!string.IsNullOrEmpty(inputId) && AndroidX.Core.App.RemoteInput.GetResultsFromIntent(intent) is { } results && results.GetCharSequence(inputId) is { } value)
		{
			userInput[inputId] = value;
		}

		return AppNotificationActivationBroker.Publish(new AppNotificationActivation(
			intent.GetStringExtra(ArgumentExtra) ?? string.Empty,
			userInput));
	}

	public static Intent CreateIntent(Context context, string argument, string? inputId)
	{
		var intent = new Intent(context, typeof(AndroidAppNotificationActivationActivity));
		intent.SetAction(IntentAction);
		intent.SetData(global::Android.Net.Uri.Parse($"uno-appnotification://activation/{Guid.NewGuid():N}"));
		intent.PutExtra(ArgumentExtra, argument);
		if (!string.IsNullOrEmpty(inputId))
		{
			intent.PutExtra(InputIdExtra, inputId);
		}
		return intent;
	}
}

[Activity(Exported = false, NoHistory = true, ExcludeFromRecents = true, Theme = "@android:style/Theme.NoDisplay")]
internal sealed class AndroidAppNotificationActivationActivity : Activity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		AndroidAppNotificationActivation.TryHandleIntent(Intent);

		if (PackageName is { } packageName && PackageManager?.GetLaunchIntentForPackage(packageName) is { } launchIntent)
		{
			launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
			StartActivity(launchIntent);
		}
		Finish();
	}
}
