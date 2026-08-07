#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppleAppNotificationTranslator
{
	internal const string RequestIdentifierPrefix = "uno.appnotifications.";
	internal const string ScheduledRequestIdentifierPrefix = "uno.toastschedules.";
	internal const string CategoryIdentifierPrefix = "uno.appnotifications.category.";
	internal const string ActionIdentifierPrefix = "uno.appnotifications.action.";

	public static AppleAppNotificationCommand Translate(AppNotificationEnvelope notification)
		=> Translate(notification, RequestIdentifierPrefix + notification.Id);

	public static AppleAppNotificationCommand TranslateScheduled(
		string scheduleIdentifier,
		string payload,
		string tag,
		string group,
		bool suppressDisplay)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		ArgumentNullException.ThrowIfNull(payload);
		return Translate(
			new AppNotificationEnvelope(
				0,
				AppNotificationPayloadParser.Parse(payload),
				tag,
				group,
				DateTimeOffset.FromFileTime(0),
				false,
				suppressDisplay,
				AppNotificationPriority.Default),
			ScheduledRequestIdentifierPrefix + scheduleIdentifier);
	}

	public static bool TryGetNotificationId(string requestIdentifier, out uint id)
	{
		id = 0;
		return requestIdentifier.StartsWith(RequestIdentifierPrefix, StringComparison.Ordinal) &&
			uint.TryParse(requestIdentifier.AsSpan(RequestIdentifierPrefix.Length), out id) &&
			id != 0;
	}

	public static bool TryGetScheduleIdentifier(string requestIdentifier, out string scheduleIdentifier)
	{
		if (requestIdentifier.StartsWith(ScheduledRequestIdentifierPrefix, StringComparison.Ordinal) &&
			requestIdentifier.Length > ScheduledRequestIdentifierPrefix.Length)
		{
			scheduleIdentifier = requestIdentifier[ScheduledRequestIdentifierPrefix.Length..];
			return true;
		}
		scheduleIdentifier = string.Empty;
		return false;
	}

	private static AppleAppNotificationCommand Translate(AppNotificationEnvelope notification, string requestIdentifier)
	{
		var unsupportedFeatures = new List<string>();
		var appLogo = notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.AppLogoOverride);
		var attachment = notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.Hero)
			?? notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.Inline);
		if (appLogo is not null)
		{
			unsupportedFeatures.Add("app-logo overrides");
		}
		if (notification.Payload.Images.Count(image => image.Placement is AppNotificationImagePlacement.Hero or AppNotificationImagePlacement.Inline) > 1)
		{
			unsupportedFeatures.Add("multiple attachments");
		}
		if (notification.Payload.Inputs.Any(input => input.Kind == AppNotificationInputKind.Selection))
		{
			unsupportedFeatures.Add("selection inputs");
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
		if (notification.Payload.Actions.Any(action => !string.IsNullOrEmpty(action.ProtocolActivationTargetApplicationPfn)))
		{
			unsupportedFeatures.Add("protocol target application IDs");
		}
		if (notification.ExpiresOnReboot)
		{
			unsupportedFeatures.Add("expires-on-reboot");
		}
		if (notification.Payload.Audio is { Source.Length: > 0 } or { Loop: true })
		{
			unsupportedFeatures.Add("custom audio");
		}
		if (notification.Payload.Actions.Count(action => !action.ContextMenuPlacement) > 4)
		{
			unsupportedFeatures.Add("more than four actions");
		}

		var actionKey = ComputeActionSetKey(notification.Payload);
		var actions = notification.Payload.Actions
			.Where(action => !action.ContextMenuPlacement)
			.Take(4)
			.Select((action, index) =>
			{
				var input = action.ActivationType == "protocol"
					? null
					: notification.Payload.Inputs.FirstOrDefault(input => input.Kind == AppNotificationInputKind.Text && input.Id == action.InputId);
				return new AppleAppNotificationActionCommand(
					ActionIdentifierPrefix + actionKey + "." + index,
					string.IsNullOrEmpty(action.Content) ? action.ToolTip : action.Content,
					action.RawArguments,
					action.ActivationType == "protocol" ? action.RawArguments : null,
					input?.Id,
					string.IsNullOrEmpty(action.Content) ? action.ToolTip : action.Content,
					input is null ? null : !string.IsNullOrEmpty(input.PlaceHolderText) ? input.PlaceHolderText : input.Title,
					action.ButtonStyle == Builder.AppNotificationButtonStyle.Critical,
					action.ActivationType == "foreground");
			})
			.ToArray();
		var subtitle = notification.Payload.Texts.Length > 2
			? notification.Payload.Texts[1].Content
			: notification.Payload.Attribution?.Content ?? string.Empty;
		if (notification.Payload.Texts.Length > 2 && notification.Payload.Attribution is not null)
		{
			unsupportedFeatures.Add("attribution text");
		}

		return new AppleAppNotificationCommand(
			notification.Id,
			requestIdentifier,
			actions.Length == 0 ? string.Empty : CategoryIdentifierPrefix + actionKey,
			notification.Payload.Title?.Content ?? string.Empty,
			subtitle,
			notification.Payload.Texts.Length > 2
				? notification.Payload.Texts[2].Content
				: notification.Payload.Body?.Content ?? string.Empty,
			notification.Group,
			attachment?.Source ?? string.Empty,
			notification.Payload.LaunchArgument,
			notification.Payload.ActivationType == "protocol" ? notification.Payload.LaunchArgument : null,
			notification.Payload.Audio?.Silent == true,
			notification.SuppressDisplay,
			notification.Priority == AppNotificationPriority.High || notification.Payload.Scenario == Builder.AppNotificationScenario.Urgent,
			actions,
			unsupportedFeatures.ToArray());
	}

	private static string ComputeActionSetKey(AppNotificationPayload payload)
	{
		var value = string.Join("\u001f", payload.Actions
			.Where(action => !action.ContextMenuPlacement)
			.Take(4)
			.Select(action =>
			{
				var input = action.ActivationType == "protocol"
					? null
					: payload.Inputs.FirstOrDefault(input => input.Kind == AppNotificationInputKind.Text && input.Id == action.InputId);
				return string.Join("\u001e",
					string.IsNullOrEmpty(action.Content) ? action.ToolTip : action.Content,
					action.ActivationType == "foreground" || action.ActivationType == "protocol",
					action.ButtonStyle == Builder.AppNotificationButtonStyle.Critical,
					input is not null,
					input is null ? string.Empty : string.IsNullOrEmpty(action.Content) ? action.ToolTip : action.Content,
					input is null ? string.Empty : !string.IsNullOrEmpty(input.PlaceHolderText) ? input.PlaceHolderText : input.Title);
			}));
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).Substring(0, 24).ToLowerInvariant();
	}
}