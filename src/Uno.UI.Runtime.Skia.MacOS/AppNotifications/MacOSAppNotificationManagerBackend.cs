#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Runtime.Skia.MacOS;

internal sealed class MacOSAppNotificationManagerBackend : IAppNotificationManagerBackend, IAppNotificationProgressUpdateCapability
{
	private static readonly MacOSAppNotificationManagerBackend _instance = new();

	private MacOSAppNotificationManagerBackend()
	{
	}

	public static void RegisterExtension()
	{
		ApiExtensibility.Register(typeof(IAppNotificationManagerBackend), _ => _instance);
		ApiExtensibility.Register(typeof(IToastNotificationSchedulerBackend), _ => MacOSToastNotificationSchedulerBackend.Instance);
	}

	public bool IsSupported => MacOSAppNotificationRuntime.IsSupported;

	public AppNotificationSetting Setting => MacOSAppNotificationRuntime.Setting;

	public string? BootIdentifier => null;

	public bool SupportsProgressUpdates => AppleAppNotificationCapabilities.SupportsProgressUpdates;

	public void Register() => MacOSAppNotificationRuntime.RequestAuthorization();

	public void Register(string displayName, Uri iconUri) => Register();

	public void Unregister()
	{
	}

	public void UnregisterAll()
	{
	}

	public bool TryShow(AppNotificationEnvelope notification)
		=> TryPost(AppleAppNotificationTranslator.Translate(notification));

	public bool TryUpdate(AppNotificationStateRecord notification)
		=> TryPost(AppleAppNotificationTranslator.Translate(notification.ToEnvelope()));

	public void Remove(AppNotificationStateRecord notification)
	{
		if (!MacOSAppNotificationRuntime.RemoveNotification(notification.Id))
		{
			throw new InvalidOperationException("macOS could not remove the app notification.");
		}
	}

	public void RemoveAll()
	{
		if (!MacOSAppNotificationRuntime.RemoveAll(AppleAppNotificationTranslator.RequestIdentifierPrefix))
		{
			throw new InvalidOperationException("macOS could not remove all app notifications.");
		}
	}

	public IReadOnlyCollection<uint>? GetActiveNotificationIds()
		=> MacOSAppNotificationRuntime.GetActiveNotificationIds();

	private static bool TryPost(AppleAppNotificationCommand command)
	{
		var posted = MacOSAppNotificationRuntime.TryPost(command);
		if (command.UnsupportedFeatures.Length > 0 && typeof(MacOSAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(MacOSAppNotificationManagerBackend).Log().LogWarning(
				$"macOS app notifications do not support {string.Join(", ", command.UnsupportedFeatures)}; those features were ignored.");
		}
		return posted;
	}
}

internal sealed class MacOSToastNotificationSchedulerBackend : IToastNotificationSchedulerBackend, INativeToastNotificationSchedulerBackend
{
	private MacOSToastNotificationSchedulerBackend()
	{
	}

	public static MacOSToastNotificationSchedulerBackend Instance { get; } = new();

	public void Schedule(ToastNotificationScheduleRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);
		var command = AppleAppNotificationTranslator.TranslateScheduled(
			record.ScheduleIdentifier,
			record.Payload,
			record.Tag,
			record.Group,
			record.SuppressPopup);
		var delay = record.DeliveryTimeUtc - DateTimeOffset.UtcNow;
		if (!MacOSAppNotificationRuntime.TryPost(command, delay))
		{
			throw new InvalidOperationException("macOS rejected the scheduled toast notification.");
		}
	}

	public void Cancel(string scheduleIdentifier)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		if (!MacOSAppNotificationRuntime.RemoveScheduled(scheduleIdentifier))
		{
			throw new InvalidOperationException("macOS could not remove the scheduled toast notification.");
		}
	}

	public IReadOnlyCollection<string>? GetPendingScheduleIdentifiers()
		=> MacOSAppNotificationRuntime.GetPendingScheduleIdentifiers();

	public IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers()
		=> MacOSAppNotificationRuntime.GetDeliveredScheduleIdentifiers();

	public IReadOnlyCollection<string>? GetDeliveryReceiptIdentifiers()
		=> AppleToastNotificationDeliveryReceiptStore.GetIdentifiers();

	public bool TryPersistDeliveryReceipt(string scheduleIdentifier)
		=> AppleToastNotificationDeliveryReceiptStore.TryPersist(scheduleIdentifier);

	public void ConsumeDeliveryReceipt(string scheduleIdentifier)
		=> AppleToastNotificationDeliveryReceiptStore.TryConsume(scheduleIdentifier);

	public void CleanupDeliveryReceipts(IReadOnlyCollection<string> retainedScheduleIdentifiers)
		=> AppleToastNotificationDeliveryReceiptStore.TryCleanup(retainedScheduleIdentifiers);

	public bool TryPersistDeliveredHistory(ToastNotificationScheduleRecord record)
		=> AppleToastNotificationDeliveredHistoryStore.TryPersist(record);

	public IReadOnlyCollection<ToastNotificationScheduleRecord>? GetDeliveredHistory()
		=> AppleToastNotificationDeliveredHistoryStore.GetAll();

	public bool TryRemoveDeliveredHistory(string scheduleIdentifier)
		=> AppleToastNotificationDeliveredHistoryStore.TryRemove(scheduleIdentifier);

	public bool TryCleanupDeliveredHistory(IReadOnlyCollection<string> activeScheduleIdentifiers)
		=> AppleToastNotificationDeliveredHistoryStore.TryCleanup(activeScheduleIdentifiers);
}