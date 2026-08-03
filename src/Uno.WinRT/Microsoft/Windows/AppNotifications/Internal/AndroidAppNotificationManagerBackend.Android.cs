#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed class AndroidAppNotificationManagerBackend : IAppNotificationManagerBackend
{
	private const string DefaultChannelId = "uno.appnotifications.default";
	private const string HighChannelId = "uno.appnotifications.high";
	private const string SilentChannelId = "uno.appnotifications.silent";
	private const string HighSilentChannelId = "uno.appnotifications.high.silent";
	private const string SuppressedChannelId = "uno.appnotifications.suppressed";
	private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
	private readonly Context _context = Application.Context;
	private readonly NotificationManagerCompat _notificationManager;

	public AndroidAppNotificationManagerBackend()
	{
		_notificationManager = NotificationManagerCompat.From(_context)
			?? throw new InvalidOperationException("Android notification manager is unavailable.");
	}

	public bool IsSupported => AndroidAppNotificationSettingEvaluator.IsSupported((int)Build.VERSION.SdkInt);

	public string? BootIdentifier
	{
		get
		{
			try
			{
				return File.ReadAllText("/proc/sys/kernel/random/boot_id").Trim();
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				return null;
			}
		}
	}

	public AppNotificationSetting Setting
	{
		get
		{
			var requiresRuntimePermission = Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu;
			return AndroidAppNotificationSettingEvaluator.Evaluate(
				requiresRuntimePermission,
				!requiresRuntimePermission || global::Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(PostNotificationsPermission),
				!requiresRuntimePermission || ContextCompat.CheckSelfPermission(_context, PostNotificationsPermission) == Permission.Granted,
				_notificationManager.AreNotificationsEnabled());
		}
	}

	public void Register()
	{
		EnsureChannel(DefaultChannelId, "App notifications", NotificationImportance.Default, silent: false);
		EnsureChannel(HighChannelId, "High-priority app notifications", NotificationImportance.High, silent: false);
		EnsureChannel(SilentChannelId, "Silent app notifications", NotificationImportance.Default, silent: true);
		EnsureChannel(HighSilentChannelId, "Silent high-priority app notifications", NotificationImportance.High, silent: true);
		EnsureChannel(SuppressedChannelId, "Suppressed app notifications", NotificationImportance.Low, silent: true);
	}

	public void Register(string displayName, Uri iconUri) => Register();

	public void Unregister()
	{
	}

	public void UnregisterAll()
	{
	}

	public bool TryShow(AppNotificationEnvelope notification)
		=> TryPost(notification);

	public bool TryUpdate(AppNotificationStateRecord notification)
		=> TryPost(notification.ToEnvelope());

	public void Remove(AppNotificationStateRecord notification)
		=> _notificationManager.Cancel(AndroidAppNotificationTranslator.NativeTag, unchecked((int)notification.Id));

	public void RemoveAll()
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.M)
		{
			return;
		}

		foreach (var notification in _notificationManager.ActiveNotifications ?? Array.Empty<global::Android.Service.Notification.StatusBarNotification>())
		{
			if (notification.Tag == AndroidAppNotificationTranslator.NativeTag)
			{
				_notificationManager.Cancel(notification.Tag, notification.Id);
			}
		}
	}

	public IReadOnlyCollection<uint>? GetActiveNotificationIds()
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.M)
		{
			return null;
		}
		var activeNotifications = _notificationManager.ActiveNotifications;
		return activeNotifications is null
			? Array.Empty<uint>()
			: activeNotifications
				.Where(notification => notification.Tag == AndroidAppNotificationTranslator.NativeTag)
				.Select(notification => unchecked((uint)notification.Id))
				.ToArray();
	}

	private bool TryPost(AppNotificationEnvelope notification)
	{
		var command = AndroidAppNotificationTranslator.Translate(notification, DateTimeOffset.UtcNow);
		var channelId = GetChannelId(command);
		EnsureChannel(channelId, GetChannelName(command), GetChannelImportance(command), command.MuteAudio || command.SuppressDisplay);
		var appInfo = _context.ApplicationInfo;
		var smallIcon = appInfo?.Icon ?? 0;
		if (smallIcon == 0)
		{
			LogWarning("The Android application has no notification icon, so the app notification was not posted.");
			return false;
		}

		var builder = new NotificationCompat.Builder(_context, channelId);
		builder.SetSmallIcon(smallIcon);
		builder.SetContentTitle(command.Title);
		builder.SetContentText(command.Body);
		builder.SetAutoCancel(true);
		builder.SetSilent(command.SuppressDisplay);
		builder.SetPriority(command.SuppressDisplay
			? NotificationCompat.PriorityLow
			: command.HighPriority ? NotificationCompat.PriorityHigh : NotificationCompat.PriorityDefault);
		if (Build.VERSION.SdkInt < BuildVersionCodes.O && !command.MuteAudio && !command.SuppressDisplay)
		{
			builder.SetDefaults(NotificationCompat.DefaultSound);
		}
		builder.SetContentIntent(CreatePendingIntent(command.BodyActivation, inputId: null));
		for (var index = 0; index < command.Actions.Length; index++)
		{
			var action = command.Actions[index];
			var pendingIntent = CreatePendingIntent(new AndroidAppNotificationActivationCommand(action.Argument, action.ProtocolUri), action.InputId);
			if (pendingIntent is null)
			{
				continue;
			}

			var actionBuilder = new NotificationCompat.Action.Builder(0, action.Content, pendingIntent);
			if (action.InputId is not null)
			{
				var remoteInputBuilder = new AndroidX.Core.App.RemoteInput.Builder(action.InputId);
				remoteInputBuilder.SetLabel(action.InputLabel ?? action.InputId);
				var remoteInput = remoteInputBuilder.Build();
				if (remoteInput is not null)
				{
					actionBuilder.AddRemoteInput(remoteInput);
				}
			}
			var nativeAction = actionBuilder.Build();
			if (nativeAction is not null)
			{
				builder.AddAction(nativeAction);
			}
		}

		if (!string.IsNullOrEmpty(command.Attribution))
		{
			builder.SetSubText(command.Attribution);
		}
		if (!string.IsNullOrEmpty(command.ProgressTitle))
		{
			builder.SetContentTitle(command.ProgressTitle);
		}
		if (!string.IsNullOrEmpty(command.ProgressStatus))
		{
			builder.SetContentText(command.ProgressStatus);
		}
		if (!string.IsNullOrEmpty(command.ProgressValueString))
		{
			builder.SetSubText(command.ProgressValueString);
		}
		if (command.ProgressValue is { } progressValue)
		{
			builder.SetProgress(1000, progressValue, indeterminate: false);
		}
		if (!string.IsNullOrEmpty(command.Group))
		{
			builder.SetGroup(command.Group);
		}
		if (command.DisplayTimestampMilliseconds is { } displayTimestamp)
		{
			builder.SetWhen(displayTimestamp);
			builder.SetShowWhen(true);
		}
		if (command.TimeoutMilliseconds is { } timeout)
		{
			builder.SetTimeoutAfter(Math.Max(1, timeout));
		}

		using var largeIcon = LoadLocalBitmap(command.LargeIconSource);
		if (largeIcon is not null)
		{
			builder.SetLargeIcon(largeIcon);
		}
		using var bigPicture = LoadLocalBitmap(command.BigPictureSource);
		if (bigPicture is not null)
		{
			var style = new NotificationCompat.BigPictureStyle();
			style.BigPicture(bigPicture);
			if (!string.IsNullOrEmpty(command.BigPictureAlternateText))
			{
				style.SetContentDescription(command.BigPictureAlternateText);
			}
			builder.SetStyle(style);
		}
		if (command.UnsupportedFeatures.Length > 0 && typeof(AndroidAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(AndroidAppNotificationManagerBackend).Log().LogWarning($"Android app notifications do not support {string.Join(", ", command.UnsupportedFeatures)}; those features were ignored.");
		}

		var nativeNotification = builder.Build();
		if (nativeNotification is null)
		{
			LogWarning("AndroidX failed to build the app notification.");
			return false;
		}
		try
		{
			_notificationManager.Notify(command.NativeTag, command.NativeId, nativeNotification);
			return true;
		}
		catch (Java.Lang.SecurityException exception)
		{
			LogWarning($"Android rejected the app notification: {exception.Message}");
			return false;
		}
	}

	private void EnsureChannel(string channelId, string name, NotificationImportance importance, bool silent)
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.O)
		{
			return;
		}
		if (_context.GetSystemService(Context.NotificationService) is NotificationManager manager)
		{
			var channel = new NotificationChannel(channelId, name, importance);
			if (silent)
			{
				channel.SetSound(null, null);
				channel.EnableVibration(false);
			}
			manager.CreateNotificationChannel(channel);
		}
	}

	private static string GetChannelId(AndroidAppNotificationCommand command)
		=> command.SuppressDisplay
			? SuppressedChannelId
			: command.HighPriority
				? command.MuteAudio ? HighSilentChannelId : HighChannelId
				: command.MuteAudio ? SilentChannelId : DefaultChannelId;

	private static string GetChannelName(AndroidAppNotificationCommand command)
		=> command.SuppressDisplay
			? "Suppressed app notifications"
			: command.HighPriority
				? command.MuteAudio ? "Silent high-priority app notifications" : "High-priority app notifications"
				: command.MuteAudio ? "Silent app notifications" : "App notifications";

	private static NotificationImportance GetChannelImportance(AndroidAppNotificationCommand command)
		=> command.SuppressDisplay
			? NotificationImportance.Low
			: command.HighPriority ? NotificationImportance.High : NotificationImportance.Default;

	private PendingIntent? CreatePendingIntent(AndroidAppNotificationActivationCommand activation, string? inputId)
	{
		Intent intent;
		var mutable = inputId is not null && activation.ProtocolUri is null;
		if (activation.ProtocolUri is not null)
		{
			intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(activation.ProtocolUri));
		}
		else
		{
			intent = AndroidAppNotificationActivation.CreateIntent(_context, activation.Argument, inputId);
		}

		var flags = PendingIntentFlags.OneShot |
			(mutable ? PendingIntentFlags.Mutable : PendingIntentFlags.Immutable);
		return PendingIntent.GetActivity(_context, Guid.NewGuid().GetHashCode(), intent, flags);
	}

	private static void LogWarning(string message)
	{
		if (typeof(AndroidAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(AndroidAppNotificationManagerBackend).Log().LogWarning(message);
		}
	}

	private static Bitmap? LoadLocalBitmap(string? source)
	{
		if (string.IsNullOrEmpty(source) || !Uri.TryCreate(source, UriKind.Absolute, out var uri))
		{
			return null;
		}

		if (uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase))
		{
			if (DrawableHelper.FindResourceIdFromPath(uri.AbsolutePath.TrimStart('/'), logFailure: false) is { } resourceId)
			{
				return BitmapFactory.DecodeResource(Application.Context.Resources!, resourceId);
			}
		}
		else if (uri.IsFile)
		{
			return BitmapFactory.DecodeFile(uri.LocalPath);
		}
		else if (typeof(AndroidAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(AndroidAppNotificationManagerBackend).Log().LogWarning($"App notification image '{source}' is not a local Android asset and was ignored.");
		}
		return null;
	}
}
