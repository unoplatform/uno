#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Windows.AppNotifications;
using Windows.UI.Notifications.Internal;

namespace Windows.UI.Notifications;

public partial class ToastNotificationHistory
{
	private readonly AppNotificationManager _manager;

	internal ToastNotificationHistory()
		: this(AppNotificationManager.Default)
	{
	}

	internal ToastNotificationHistory(AppNotificationManager manager)
	{
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	public IReadOnlyList<ToastNotification> GetHistory()
		=> _manager.GetAll()
			.Select(LegacyToastNotificationPayloadAdapter.FromAppNotification)
			.Concat(ToastNotificationSchedulerRuntime.GetDeliveredHistory().Select(ToastNotificationSchedulerRuntime.FromDeliveredRecord))
			.ToArray();

	public void RemoveGroup(string group)
	{
		ValidateIdentifier(group, nameof(group));
		_manager.RemoveByGroup(group);
		ToastNotificationSchedulerRuntime.RemoveDeliveredHistory(record => record.Group == group);
	}

	public void Remove(string tag, string group)
	{
		ValidateIdentifier(tag, nameof(tag));
		ValidateGroup(group, nameof(group));
		_manager.RemoveByTagAndGroup(tag, group);
		ToastNotificationSchedulerRuntime.RemoveDeliveredHistory(record => record.Tag == tag && record.Group == group);
	}

	public void Remove(string tag)
	{
		ValidateIdentifier(tag, nameof(tag));
		_manager.RemoveByTag(tag);
		ToastNotificationSchedulerRuntime.RemoveDeliveredHistory(record => record.Tag == tag && record.Group.Length == 0);
	}

	public void Clear()
	{
		_manager.RemoveAll();
		ToastNotificationSchedulerRuntime.RemoveDeliveredHistory(_ => true);
	}

	private static void ValidateIdentifier(string value, string parameterName)
	{
		if (string.IsNullOrEmpty(value) || value.Length > 64)
		{
			throw new ArgumentException("A notification identifier between 1 and 64 characters is required.", parameterName);
		}
	}

	private static void ValidateGroup(string value, string parameterName)
	{
		if (value is null || value.Length > 64)
		{
			throw new ArgumentException("A notification group of at most 64 characters is required.", parameterName);
		}
	}
}
