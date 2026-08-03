#nullable enable

using System;
using Android.App;
using Android.Content;
using Android.OS;
using System.Threading.Tasks;

namespace Windows.UI.Notifications.Internal;

internal sealed class AndroidToastNotificationSchedulerBackend : IToastNotificationSchedulerBackend
{
	private readonly Context _context = Application.Context;
	private readonly AlarmManager _alarmManager;

	public AndroidToastNotificationSchedulerBackend()
	{
		_alarmManager = _context.GetSystemService(Context.AlarmService) as AlarmManager
			?? throw new InvalidOperationException("Android alarm manager is unavailable.");
	}

	public void Schedule(ToastNotificationScheduleRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);
		var pendingIntent = CreatePendingIntent(record.ScheduleIdentifier, GetPendingIntentFlags(PendingIntentFlags.UpdateCurrent))
			?? throw new InvalidOperationException("Android could not create the scheduled notification PendingIntent.");
		_alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, record.DeliveryTimeUtc.ToUnixTimeMilliseconds(), pendingIntent);
	}

	public void Cancel(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		var pendingIntent = CreatePendingIntent(scheduleIdentifier, GetPendingIntentFlags(PendingIntentFlags.NoCreate));
		if (pendingIntent is not null)
		{
			_alarmManager.Cancel(pendingIntent);
			pendingIntent.Cancel();
		}
	}

	private PendingIntent? CreatePendingIntent(string scheduleIdentifier, PendingIntentFlags flags)
	{
		var intent = new Intent(_context, typeof(AndroidToastNotificationScheduleReceiver));
		intent.SetAction(AndroidToastNotificationScheduleReceiver.DeliveryAction);
		intent.SetData(global::Android.Net.Uri.Parse($"uno-toastschedule://delivery/{scheduleIdentifier}"));
		intent.PutExtra(AndroidToastNotificationScheduleReceiver.ScheduleIdentifierExtra, scheduleIdentifier);
		return PendingIntent.GetBroadcast(_context, GetRequestCode(scheduleIdentifier), intent, flags);
	}

	private static int GetRequestCode(string scheduleIdentifier)
	{
		var identifier = Guid.ParseExact(scheduleIdentifier, "N");
		var bytes = identifier.ToByteArray();
		return BitConverter.ToInt32(bytes, 0);
	}

	private static PendingIntentFlags GetPendingIntentFlags(PendingIntentFlags flags)
		=> Build.VERSION.SdkInt >= BuildVersionCodes.M ? flags | PendingIntentFlags.Immutable : flags;
}

[BroadcastReceiver(Exported = false, Enabled = true)]
internal sealed class AndroidToastNotificationScheduleReceiver : BroadcastReceiver
{
	internal const string DeliveryAction = "uno.toastnotifications.DELIVER";
	internal const string ScheduleIdentifierExtra = "uno.toastnotifications.scheduleIdentifier";

	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action != DeliveryAction || intent.GetStringExtra(ScheduleIdentifierExtra) is not { } scheduleIdentifier)
		{
			return;
		}

		RunAsync(() => ToastNotificationSchedulerRuntime.Deliver(scheduleIdentifier));
	}

	private void RunAsync(Action action)
	{
		var pendingResult = GoAsync();
		_ = Task.Run(() =>
		{
			try
			{
				action();
			}
			finally
			{
				pendingResult?.Finish();
			}
		});
	}
}

[BroadcastReceiver(Exported = false, Enabled = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted })]
internal sealed class AndroidToastNotificationBootReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent?.Action == Intent.ActionBootCompleted)
		{
			var pendingResult = GoAsync();
			_ = Task.Run(() =>
			{
				try
				{
					ToastNotificationSchedulerRuntime.Recover();
				}
				finally
				{
					pendingResult?.Finish();
				}
			});
		}
	}
}
