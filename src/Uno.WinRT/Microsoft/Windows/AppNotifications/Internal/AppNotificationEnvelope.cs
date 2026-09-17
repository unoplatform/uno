#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record AppNotificationEnvelope(
	uint Id,
	AppNotificationPayload? ParsedPayload,
	string Tag,
	string Group,
	DateTimeOffset Expiration,
	bool ExpiresOnReboot,
	bool SuppressDisplay,
	AppNotificationPriority Priority,
	AppNotificationProgressSnapshot? Progress = null,
	string RawPayload = "")
{
	// Native backends carry XML unchanged; only portable translators require this reduced projection.
	public AppNotificationPayload Payload => ParsedPayload ?? AppNotificationPayloadParser.Parse(RawPayload);
}
