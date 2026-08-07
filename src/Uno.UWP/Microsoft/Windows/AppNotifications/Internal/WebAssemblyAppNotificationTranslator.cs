#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class WebAssemblyAppNotificationTranslator
{
	private const string NativeTagPrefix = "uno.appnotifications.";

	public static WebAssemblyAppNotificationCommand Translate(AppNotificationEnvelope notification)
	{
		var unsupportedFeatures = new List<string>();
		if (notification.Payload.Texts.Length > 2)
		{
			unsupportedFeatures.Add("additional text");
		}
		if (notification.Payload.Attribution is not null)
		{
			unsupportedFeatures.Add("attribution text");
		}
		if (notification.Payload.Inputs.Length > 0)
		{
			unsupportedFeatures.Add("inputs");
		}
		if (notification.Payload.ProgressBars.Length > 0 || notification.Progress is not null)
		{
			unsupportedFeatures.Add("progress");
		}
		if (notification.Payload.Actions.Any(action => action.ContextMenuPlacement))
		{
			unsupportedFeatures.Add("context-menu actions");
		}
		if (notification.Payload.Actions.Any(action => action.PendingUpdate))
		{
			unsupportedFeatures.Add("pending-update actions");
		}
		if (notification.Payload.Actions.Any(action => !action.ContextMenuPlacement))
		{
			unsupportedFeatures.Add("actions");
		}
		if (notification.Payload.Actions.Any(action => !string.IsNullOrEmpty(action.ProtocolActivationTargetApplicationPfn)))
		{
			unsupportedFeatures.Add("protocol target application IDs");
		}
		if (notification.Payload.Actions.Any(action => !string.IsNullOrEmpty(action.InputId)))
		{
			unsupportedFeatures.Add("action inputs");
		}
		if (notification.ExpiresOnReboot)
		{
			unsupportedFeatures.Add("expires-on-reboot");
		}
		if (notification.SuppressDisplay)
		{
			unsupportedFeatures.Add("suppressed display");
		}
		if (notification.Payload.Audio is { Source.Length: > 0 } or { Loop: true })
		{
			unsupportedFeatures.Add("custom audio");
		}

		var icon = notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.AppLogoOverride)?.Source ?? string.Empty;
		var image = notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.Hero)?.Source
			?? notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.Inline)?.Source
			?? string.Empty;
		var language = notification.Payload.Title?.Language;
		if (string.IsNullOrEmpty(language))
		{
			language = notification.Payload.Language;
		}
		var actions = notification.Payload.Actions
			.Where(action => !action.ContextMenuPlacement)
			.Select((action, index) => new WebAssemblyAppNotificationActionCommand(
				$"action-{index}",
				string.IsNullOrEmpty(action.Content) ? action.ToolTip : action.Content,
				action.ImageUri,
				action.RawArguments,
				action.ActivationType == "protocol" ? action.RawArguments : null))
			.ToArray();
		var expiration = notification.Expiration.ToUniversalTime();

		return new WebAssemblyAppNotificationCommand(
			notification.Id,
			NativeTagPrefix + notification.Id,
			notification.Payload.Title?.Content ?? string.Empty,
			notification.Payload.Body?.Content ?? string.Empty,
			language ?? string.Empty,
			GetDirection(language),
			icon,
			image,
			notification.Payload.DisplayTimestamp?.ToUnixTimeMilliseconds(),
			expiration <= DateTimeOffset.FromFileTime(0) ? null : expiration.ToUnixTimeMilliseconds(),
			notification.Payload.Audio?.Silent == true || notification.SuppressDisplay,
			notification.Payload.Duration == Builder.AppNotificationDuration.Long || notification.Priority == AppNotificationPriority.High,
			notification.Payload.LaunchArgument,
			notification.Payload.ActivationType == "protocol" ? notification.Payload.LaunchArgument : null,
			actions,
			unsupportedFeatures.ToArray());
	}

	private static string GetDirection(string? language)
	{
		if (string.IsNullOrEmpty(language))
		{
			return "auto";
		}

		var separator = language.IndexOf('-');
		var primaryLanguage = separator < 0 ? language : language[..separator];
		return primaryLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase) ||
			primaryLanguage.Equals("fa", StringComparison.OrdinalIgnoreCase) ||
			primaryLanguage.Equals("he", StringComparison.OrdinalIgnoreCase) ||
			primaryLanguage.Equals("ur", StringComparison.OrdinalIgnoreCase)
			? "rtl"
			: "ltr";
	}
}