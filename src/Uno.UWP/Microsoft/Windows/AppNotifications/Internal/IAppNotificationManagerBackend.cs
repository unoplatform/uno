#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.Windows.AppNotifications.Internal;

internal interface IAppNotificationManagerBackend
{
	bool IsSupported { get; }

	AppNotificationSetting Setting { get; }

	string? BootIdentifier { get; }

	void Register();

	void Register(string displayName, Uri iconUri);

	void Unregister();

	void UnregisterAll();

	bool TryShow(AppNotificationEnvelope notification);

	bool TryUpdate(AppNotificationStateRecord notification);

	void Remove(AppNotificationStateRecord notification);

	void RemoveAll();

	IReadOnlyCollection<uint>? GetActiveNotificationIds();
}
