#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications;

partial class AppNotification
{
	internal AppNotificationSnapshot CaptureSnapshot()
	{
		lock (_gate)
		{
			return new AppNotificationSnapshot(
				_id,
				_payload,
				_tag,
				_group,
				_expiration,
				_expiresOnReboot,
				_priority,
				_suppressDisplay,
				_progress?.Clone());
		}
	}
}

internal sealed record AppNotificationSnapshot(
	uint Id,
	string Payload,
	string Tag,
	string Group,
	DateTimeOffset Expiration,
	bool ExpiresOnReboot,
	AppNotificationPriority Priority,
	bool SuppressDisplay,
	AppNotificationProgressData? Progress);
