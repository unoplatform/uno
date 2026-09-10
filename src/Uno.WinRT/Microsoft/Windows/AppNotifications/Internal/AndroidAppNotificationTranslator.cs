#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AndroidAppNotificationTranslator
{
	internal const string NativeTag = "uno.appnotifications";

	public static AndroidAppNotificationCommand Translate(AppNotificationEnvelope notification, DateTimeOffset now)
	{
		var unsupportedFeatures = new List<string>();
		if (notification.Payload.Texts.Length > 2)
		{
			unsupportedFeatures.Add("additional text");
		}
		if (notification.Payload.Inputs.Any(input => input.Kind == AppNotificationInputKind.Selection))
		{
			unsupportedFeatures.Add("selection inputs");
		}
		if (notification.Payload.ProgressBars.Length > 0)
		{
			unsupportedFeatures.Add("progress");
		}
		if (notification.ExpiresOnReboot)
		{
			unsupportedFeatures.Add("expires-on-reboot");
		}
		var appLogo = notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.AppLogoOverride);
		var richImage = notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.Hero)
			?? notification.Payload.Images.FirstOrDefault(image => image.Placement == AppNotificationImagePlacement.Inline);
		var expiration = notification.Expiration.ToUniversalTime();
		long? timeout = expiration <= DateTimeOffset.FromFileTime(0)
			? null
			: Math.Max(0, (long)(expiration - now.ToUniversalTime()).TotalMilliseconds);
		var actions = notification.Payload.Actions
			.Where(action => !action.ContextMenuPlacement)
			.Take(3)
			.Select(action =>
			{
				var isProtocol = action.ActivationType == "protocol";
				var input = isProtocol ? null : notification.Payload.Inputs.FirstOrDefault(input =>
					input.Kind == AppNotificationInputKind.Text &&
					input.Id == action.InputId);
				var inputLabel = input is null
					? null
					: !string.IsNullOrEmpty(input.Title)
						? input.Title
						: !string.IsNullOrEmpty(input.PlaceHolderText) ? input.PlaceHolderText : input.Id;
				return new AndroidAppNotificationActionCommand(
					string.IsNullOrEmpty(action.Content) ? action.ToolTip : action.Content,
					action.RawArguments,
					isProtocol ? action.RawArguments : null,
					input?.Id,
					inputLabel);
			})
			.ToArray();
		if (notification.Payload.Actions.Any(action => action.ContextMenuPlacement))
		{
			unsupportedFeatures.Add("context-menu actions");
		}
		if (notification.Payload.Actions.Count(action => !action.ContextMenuPlacement) > 3)
		{
			unsupportedFeatures.Add("more than three actions");
		}
		if (notification.Payload.Actions.Any(action => action.PendingUpdate))
		{
			unsupportedFeatures.Add("pending-update actions");
		}
		if (notification.Payload.Actions.Any(action => !string.IsNullOrEmpty(action.ProtocolActivationTargetApplicationPfn)))
		{
			unsupportedFeatures.Add("protocol target application IDs");
		}
		if (notification.Payload.Actions.Any(action => action.ActivationType == "protocol" && !string.IsNullOrEmpty(action.InputId)))
		{
			unsupportedFeatures.Add("protocol action inputs");
		}
		var progress = notification.Progress;
		int? progressValue = progress is not null && double.IsFinite(progress.Value)
			? (int)Math.Round(Math.Clamp(progress.Value, 0d, 1d) * 1000d, MidpointRounding.AwayFromZero)
			: null;

		return new AndroidAppNotificationCommand(
			unchecked((int)notification.Id),
			NativeTag,
			notification.Payload.Title?.Content ?? string.Empty,
			notification.Payload.Body?.Content ?? string.Empty,
			notification.Payload.Attribution?.Content ?? string.Empty,
			notification.Group,
			appLogo?.Source,
			richImage?.Source,
			richImage?.AlternateText ?? string.Empty,
			notification.Payload.DisplayTimestamp?.ToUnixTimeMilliseconds(),
			timeout,
			notification.Payload.Audio?.Silent == true,
			notification.SuppressDisplay,
			notification.Priority == AppNotificationPriority.High,
			progress?.Title ?? string.Empty,
			progress?.Status ?? string.Empty,
			progress?.ValueStringOverride ?? string.Empty,
			progressValue,
			new AndroidAppNotificationActivationCommand(
				notification.Payload.LaunchArgument,
				notification.Payload.ActivationType == "protocol" ? notification.Payload.LaunchArgument : null),
			actions,
			unsupportedFeatures.ToArray());
	}
}
