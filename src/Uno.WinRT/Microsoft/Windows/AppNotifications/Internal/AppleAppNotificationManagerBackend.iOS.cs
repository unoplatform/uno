#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Logging;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed class AppleAppNotificationManagerBackend : IAppNotificationManagerBackend
{
	public bool IsSupported => true;

	public AppNotificationSetting Setting => AppleAppNotificationRuntime.Setting;

	public string? BootIdentifier => null;

	public void Register() => AppleAppNotificationRuntime.RequestAuthorization();

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
	{
		AppleAppNotificationRuntime.Remove(AppleAppNotificationTranslator.RequestIdentifierPrefix + notification.Id);
		return TryPost(AppleAppNotificationTranslator.Translate(notification.ToEnvelope()));
	}

	public void Remove(AppNotificationStateRecord notification)
		=> AppleAppNotificationRuntime.Remove(AppleAppNotificationTranslator.RequestIdentifierPrefix + notification.Id);

	public void RemoveAll()
		=> AppleAppNotificationRuntime.RemoveAll(AppleAppNotificationTranslator.RequestIdentifierPrefix);

	public IReadOnlyCollection<uint>? GetActiveNotificationIds()
		=> AppleAppNotificationRuntime.GetActiveNotificationIds();

	private static bool TryPost(AppleAppNotificationCommand command)
	{
		var posted = AppleAppNotificationRuntime.TryPost(command);
		if (command.UnsupportedFeatures.Length > 0 && typeof(AppleAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(AppleAppNotificationManagerBackend).Log().LogWarning(
				$"Apple app notifications do not support {string.Join(", ", command.UnsupportedFeatures)}; those features were ignored.");
		}
		return posted;
	}
}