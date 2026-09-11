#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Windows.AppNotifications;

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
		=> _manager.GetAll().Select(LegacyToastNotificationPayloadAdapter.FromAppNotification).ToArray();

	public void RemoveGroup(string group)
	{
		ValidateIdentifier(group, nameof(group));
		_manager.RemoveByGroup(group);
	}

	public void Remove(string tag, string group)
	{
		ValidateIdentifier(tag, nameof(tag));
		ValidateIdentifier(group, nameof(group));
		_manager.RemoveByTagAndGroup(tag, group);
	}

	public void Remove(string tag)
	{
		ValidateIdentifier(tag, nameof(tag));
		_manager.RemoveByTag(tag);
	}

	public void Clear() => _manager.RemoveAll();

	private static void ValidateIdentifier(string value, string parameterName)
	{
		if (string.IsNullOrEmpty(value) || value.Length > 64)
		{
			throw new ArgumentException("A notification identifier between 1 and 64 characters is required.", parameterName);
		}
	}
}
