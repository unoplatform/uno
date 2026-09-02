#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record AppNotificationEnvelope(
	uint Id,
	AppNotificationPayload Payload,
	string Tag,
	string Group,
	DateTimeOffset Expiration,
	bool ExpiresOnReboot,
	bool SuppressDisplay,
	AppNotificationPriority Priority,
	AppNotificationProgressSnapshot? Progress = null,
	string RawPayload = "");
