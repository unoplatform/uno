#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class LinuxAppNotificationTranslator
{
	internal const string BodyActionKey = "default";
	internal const string ActionKeyPrefix = "uno.appnotifications.action.";

	public static LinuxAppNotificationCommand Translate(AppNotificationEnvelope notification, DateTimeOffset now)
	{
		var unsupportedFeatures = new List<string>();
		if (notification.Payload.Texts.Length > 2)
		{
			unsupportedFeatures.Add("additional text");
		}
		if (notification.Payload.Inputs.Length > 0)
		{
			unsupportedFeatures.Add("inputs");
		}
		if (notification.Payload.Images.Any(image => image.Placement == AppNotificationImagePlacement.Hero))
		{
			unsupportedFeatures.Add("hero images");
		}
		if (notification.Payload.Actions.Any(action => action.ContextMenuPlacement))
		{
			unsupportedFeatures.Add("context-menu actions");
		}
		if (notification.Payload.Actions.Any(action => action.PendingUpdate))
		{
			unsupportedFeatures.Add("pending-update actions");
		}
		if (notification.Payload.Actions.Any(action => !string.IsNullOrEmpty(action.ProtocolActivationTargetApplicationPfn)))
		{
			unsupportedFeatures.Add("protocol target application IDs");
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
		if (notification.Payload.Actions.Count(action => !action.ContextMenuPlacement) > 5)
		{
			unsupportedFeatures.Add("more than five actions");
		}

		var appIcon = notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.AppLogoOverride)?.Source
			?? notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.Inline)?.Source
			?? string.Empty;
		var actions = notification.Payload.Actions
			.Where(action => !action.ContextMenuPlacement)
			.Take(5)
			.Select((action, index) => new LinuxAppNotificationActionCommand(
				ActionKeyPrefix + index,
				string.IsNullOrEmpty(action.Content) ? action.ToolTip : action.Content,
				action.RawArguments,
				action.ActivationType == "protocol" ? action.RawArguments : null))
			.ToArray();
		var expiration = notification.Expiration.ToUniversalTime();
		var expireTimeout = expiration <= DateTimeOffset.FromFileTime(0)
			? -1
			: (int)Math.Clamp(
				Math.Ceiling((expiration - now.ToUniversalTime()).TotalMilliseconds),
				1,
				int.MaxValue);
		var progress = notification.Progress;
		int? progressPercentage = progress is not null && double.IsFinite(progress.Value)
			? (int)Math.Round(Math.Clamp(progress.Value, 0d, 1d) * 100d, MidpointRounding.AwayFromZero)
			: null;

		return new LinuxAppNotificationCommand(
			notification.Id,
			notification.Payload.Title?.Content ?? string.Empty,
			notification.Payload.Body?.Content ?? string.Empty,
			appIcon,
			GetCategory(notification.Payload.Scenario),
			notification.Priority == AppNotificationPriority.High || notification.Payload.Scenario == Builder.AppNotificationScenario.Urgent ? (byte)2 : (byte)1,
			expireTimeout,
			notification.Payload.Audio?.Silent == true,
			notification.SuppressDisplay,
			progressPercentage,
			BodyActionKey,
			notification.Payload.LaunchArgument,
			notification.Payload.ActivationType == "protocol" ? notification.Payload.LaunchArgument : null,
			actions,
			unsupportedFeatures.ToArray());
	}

	private static string GetCategory(Builder.AppNotificationScenario scenario)
		=> scenario switch
		{
			Builder.AppNotificationScenario.Alarm => "alarm",
			Builder.AppNotificationScenario.Reminder => "appointment",
			Builder.AppNotificationScenario.IncomingCall => "call.incoming",
			_ => string.Empty,
		};
}