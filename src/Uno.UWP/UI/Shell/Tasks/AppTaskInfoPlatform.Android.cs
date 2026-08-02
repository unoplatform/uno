#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Uno.Foundation.Logging;
using Uno.UI.Shell.Tasks;
using AUri = Android.Net.Uri;

namespace Windows.UI.Shell.Tasks;

internal static partial class AppTaskInfoPlatform
{
	internal static partial IAppTaskInfoExtension? CreateExtension() => AndroidAppTaskInfoExtension.Instance;
}

internal sealed class AndroidAppTaskInfoExtension : AppTaskInfoExtensionBase
{
	private const string ChannelId = "uno-platform-app-tasks";
	private const string NotificationTagPrefix = "UnoPlatform.AppTask.";

	internal static AndroidAppTaskInfoExtension Instance { get; } = new();

	private readonly HashSet<string> _postedTaskIds = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _signatures = new(StringComparer.Ordinal);

	private AndroidAppTaskInfoExtension()
	{
	}

	public override bool IsSupported()
	{
		var context = Application.Context;
		if (context?.GetSystemService(Context.NotificationService) is not NotificationManager)
		{
			return false;
		}

		return !OperatingSystem.IsAndroidVersionAtLeast(33)
			|| context.CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Permission.Granted;
	}

	protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
	{
		var context = Application.Context
			?? throw new InvalidOperationException("The Android application context is not initialized.");
		var manager = context.GetSystemService(Context.NotificationService) as NotificationManager
			?? throw new InvalidOperationException("The Android notification service is unavailable.");

		EnsureNotificationChannel(manager);

		var currentTaskIds = tasks.Select(static task => task.Id).ToHashSet(StringComparer.Ordinal);
		if (OperatingSystem.IsAndroidVersionAtLeast(23)
			&& manager.GetActiveNotifications() is { } activeNotifications)
		{
			foreach (var notification in activeNotifications)
			{
				if (notification.Tag is { } tag
					&& tag.StartsWith(NotificationTagPrefix, StringComparison.Ordinal)
					&& !currentTaskIds.Contains(tag.Substring(NotificationTagPrefix.Length)))
				{
					manager.Cancel(tag, notification.Id);
				}
			}
		}

		foreach (var removedTaskId in _postedTaskIds.Except(currentTaskIds).ToArray())
		{
			manager.Cancel(NotificationTagPrefix + removedTaskId, id: 0);
			_postedTaskIds.Remove(removedTaskId);
			_signatures.Remove(removedTaskId);
		}

		foreach (var task in tasks)
		{
			var signature = GetSignature(task);
			if (_signatures.TryGetValue(task.Id, out var previousSignature)
				&& previousSignature == signature)
			{
				continue;
			}

			manager.Notify(NotificationTagPrefix + task.Id, id: 0, CreateNotification(context, task));
			_postedTaskIds.Add(task.Id);
			_signatures[task.Id] = signature;
		}

		return Task.CompletedTask;
	}

	private static void EnsureNotificationChannel(NotificationManager manager)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26)
			&& manager.GetNotificationChannel(ChannelId) is null)
		{
			manager.CreateNotificationChannel(
				new NotificationChannel(ChannelId, "App tasks", NotificationImportance.Default)
				{
					Description = "Progress and actions for long-running app tasks.",
				});
		}
	}

	private static Notification CreateNotification(Context context, AppTaskInfoSnapshot task)
	{
		var builder = OperatingSystem.IsAndroidVersionAtLeast(26)
			? new Notification.Builder(context, ChannelId)
			: new Notification.Builder(context);
		var icon = context.ApplicationInfo?.Icon ?? 0;
		if (icon == 0)
		{
			icon = Android.Resource.Drawable.StatSysDownload;
		}

		builder
			.SetSmallIcon(icon)
			.SetContentTitle(task.Title)
			.SetContentText(GetContentText(task))
			.SetSubText(task.State.ToString())
			.SetContentIntent(CreateActivityPendingIntent(context, task.DeepLink, GetRequestCode(task.Id, 0)))
			.SetOngoing(task.State is AppTaskState.Running or AppTaskState.Paused or AppTaskState.NeedsAttention)
			.SetAutoCancel(task.State is AppTaskState.Completed or AppTaskState.Error)
			.SetOnlyAlertOnce(task.State is AppTaskState.Running or AppTaskState.Paused)
			.SetCategory(Notification.CategoryProgress);

		if (task.State == AppTaskState.Running)
		{
			builder.SetProgress(max: 0, progress: 0, indeterminate: true);
		}

		var expandedText = string.IsNullOrEmpty(task.Content.Question)
			? GetContentText(task)
			: $"{task.Content.Question}\n{GetContentText(task)}";
		builder.SetStyle(new Notification.BigTextStyle().BigText(expandedText));

		for (var index = 0; index < task.Content.Buttons.Length; index++)
		{
			var button = task.Content.Buttons[index];
			builder.AddAction(new Notification.Action.Builder(
				icon,
				button.Text,
				CreateActivityPendingIntent(context, button.ActionUri, GetRequestCode(task.Id, index + 1))).Build());
		}

		if (!string.IsNullOrEmpty(task.Content.TextInputActionUriTemplate))
		{
			var replyIntent = new Intent(context, typeof(AppTaskTextInputReceiver));
			replyIntent.SetAction(AppTaskTextInputReceiver.ActionSubmit);
			replyIntent.PutExtra(AppTaskTextInputReceiver.ExtraTaskId, task.Id);

			var replyPendingIntent = PendingIntent.GetBroadcast(
				context,
				GetRequestCode(task.Id, task.Content.Buttons.Length + 1),
				replyIntent,
				PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable);
			var remoteInput = new RemoteInput.Builder(AppTaskTextInputReceiver.RemoteInputKey)
				.SetLabel(task.Content.TextInputPlaceholder)
				.Build();
			var replyAction = new Notification.Action.Builder(icon, "Submit", replyPendingIntent)
				.AddRemoteInput(remoteInput)
				.Build();
			builder.AddAction(replyAction);
		}

		return builder.Build();
	}

	private static PendingIntent? CreateActivityPendingIntent(Context context, Uri uri, int requestCode)
	{
		var intent = new Intent(Intent.ActionView, AUri.Parse(uri.OriginalString));
		intent.SetPackage(context.PackageName);
		intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

		return PendingIntent.GetActivity(
			context,
			requestCode,
			intent,
			PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
	}

	private static string GetContentText(AppTaskInfoSnapshot task)
	{
		if (!string.IsNullOrEmpty(task.Content.ExecutingStep))
		{
			return task.Content.ExecutingStep;
		}

		if (!string.IsNullOrEmpty(task.Content.TextSummary))
		{
			return task.Content.TextSummary;
		}

		if (task.Content.GeneratedAssets.Length > 0)
		{
			return string.Join(", ", task.Content.GeneratedAssets.Select(static asset => asset.Name));
		}

		return task.Subtitle;
	}

	private static int GetRequestCode(string taskId, int actionIndex)
	{
		var taskHash = Guid.TryParse(taskId, out var id) ? id.GetHashCode() : StringComparer.Ordinal.GetHashCode(taskId);
		return HashCode.Combine(taskHash, actionIndex);
	}

	private static string GetSignature(AppTaskInfoSnapshot task) =>
		string.Join(
			'\n',
			task.State,
			task.Title,
			task.Subtitle,
			task.DeepLink,
			task.Content.ExecutingStep,
			task.Content.TextSummary,
			task.Content.Question,
			task.Content.TextInputPlaceholder,
			task.Content.TextInputActionUriTemplate,
			string.Join('\n', task.Content.Buttons.Select(static button => $"{button.Text}|{button.ActionUri}")),
			string.Join('\n', task.Content.GeneratedAssets.Select(static asset => $"{asset.Name}|{asset.AssetUri}")));
}

[BroadcastReceiver(Enabled = true, Exported = false)]
internal sealed class AppTaskTextInputReceiver : BroadcastReceiver
{
	internal const string ActionSubmit = "uno.platform.appTasks.SUBMIT";
	internal const string ExtraTaskId = "uno.platform.appTasks.taskId";
	internal const string RemoteInputKey = "uno.platform.appTasks.userTextInput";

	public override void OnReceive(Context? context, Intent? intent)
	{
		if (context is null
			|| intent?.Action != ActionSubmit
			|| intent.GetStringExtra(ExtraTaskId) is not { } taskId
			|| AppTaskInfoRegistry.TryGet(taskId)?.Content.TextInputActionUriTemplate is not { Length: > 0 } template
			|| RemoteInput.GetResultsFromIntent(intent)?.GetCharSequence(RemoteInputKey)?.ToString() is not { } input)
		{
			return;
		}

		var actionUri = template.Replace(
			AppTaskValidation.UserTextInputPlaceholder,
			Uri.EscapeDataString(input),
			StringComparison.Ordinal);
		var launchIntent = new Intent(Intent.ActionView, AUri.Parse(actionUri));
		launchIntent.SetPackage(context.PackageName);
		launchIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

		try
		{
			context.StartActivity(launchIntent);
		}
		catch (ActivityNotFoundException error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Unable to launch the app task text-input action.", error);
			}
		}
	}
}
