#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using UserNotifications;
using Windows.ApplicationModel;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static partial class AppleAppNotificationNativeContent
{
	private static readonly NSString LaunchArgumentKey = new("uno.appnotifications.launchArgument");
	private static readonly NSString ProtocolUriKey = new("uno.appnotifications.protocolUri");
	private static readonly NSString MuteAudioKey = new("uno.appnotifications.muteAudio");
	private static readonly NSString SuppressDisplayKey = new("uno.appnotifications.suppressDisplay");

	public static UNMutableNotificationContent Create(AppleAppNotificationCommand command)
	{
		var userInfo = new NSMutableDictionary
		{
			[LaunchArgumentKey] = new NSString(command.LaunchArgument),
			[MuteAudioKey] = NSNumber.FromBoolean(command.MuteAudio),
			[SuppressDisplayKey] = NSNumber.FromBoolean(command.SuppressDisplay),
		};
		if (command.ProtocolUri is not null)
		{
			userInfo[ProtocolUriKey] = new NSString(command.ProtocolUri);
		}
		foreach (var action in command.Actions)
		{
			userInfo[GetActionKey(action.Identifier, "argument")] = new NSString(action.Argument);
			if (action.ProtocolUri is not null)
			{
				userInfo[GetActionKey(action.Identifier, "protocolUri")] = new NSString(action.ProtocolUri);
			}
			if (action.InputId is not null)
			{
				userInfo[GetActionKey(action.Identifier, "inputId")] = new NSString(action.InputId);
			}
		}

		var content = new UNMutableNotificationContent
		{
			Title = command.Title,
			Subtitle = command.Subtitle,
			Body = command.Body,
			ThreadIdentifier = command.ThreadIdentifier,
			CategoryIdentifier = command.CategoryIdentifier,
			UserInfo = userInfo,
			Sound = command.MuteAudio || command.SuppressDisplay ? null : UNNotificationSound.Default,
		};
		if (OperatingSystem.IsIOSVersionAtLeast(15) || OperatingSystem.IsMacCatalystVersionAtLeast(15))
		{
			content.InterruptionLevel = command.SuppressDisplay
				? UNNotificationInterruptionLevel.Passive2
				: command.HighPriority
					? UNNotificationInterruptionLevel.TimeSensitive2
					: UNNotificationInterruptionLevel.Active2;
		}
		if (TryCreateAttachment(command.AttachmentSource) is { } attachment)
		{
			content.Attachments = new[] { attachment };
		}
		return content;
	}

	public static UNNotificationCategory? CreateCategory(AppleAppNotificationCommand command)
	{
		if (command.CategoryIdentifier.Length == 0 || command.Actions.Length == 0)
		{
			return null;
		}
		var actions = command.Actions.Select(CreateAction).ToArray();
		return UNNotificationCategory.FromIdentifier(
			command.CategoryIdentifier,
			actions,
			Array.Empty<string>(),
			UNNotificationCategoryOptions.None);
	}

	public static bool IsMuted(UNNotificationContent content)
		=> GetBoolean(content.UserInfo, MuteAudioKey);

	public static bool IsSuppressed(UNNotificationContent content)
		=> GetBoolean(content.UserInfo, SuppressDisplayKey);

	public static bool TryGetActivation(
		UNNotificationResponse response,
		out string argument,
		out string? protocolUri,
		out IDictionary<string, string> userInput)
	{
		argument = string.Empty;
		protocolUri = null;
		userInput = new Dictionary<string, string>();
		if (response.IsDismissAction)
		{
			return false;
		}

		var info = response.Notification.Request.Content.UserInfo;
		if (response.IsDefaultAction)
		{
			argument = GetString(info, LaunchArgumentKey);
			protocolUri = GetOptionalString(info, ProtocolUriKey);
			return true;
		}

		var actionIdentifier = response.ActionIdentifier.ToString();
		if (!actionIdentifier.StartsWith(AppleAppNotificationTranslator.ActionIdentifierPrefix, StringComparison.Ordinal))
		{
			return false;
		}
		argument = GetString(info, GetActionKey(actionIdentifier, "argument"));
		protocolUri = GetOptionalString(info, GetActionKey(actionIdentifier, "protocolUri"));
		if (response is UNTextInputNotificationResponse textResponse &&
			GetOptionalString(info, GetActionKey(actionIdentifier, "inputId")) is { Length: > 0 } inputId)
		{
			userInput[inputId] = textResponse.UserText ?? string.Empty;
		}
		return true;
	}

	private static UNNotificationAction CreateAction(AppleAppNotificationActionCommand action)
	{
		var options = UNNotificationActionOptions.None;
		if (action.Destructive)
		{
			options |= UNNotificationActionOptions.Destructive;
		}
		if (action.Foreground || action.ProtocolUri is not null)
		{
			options |= UNNotificationActionOptions.Foreground;
		}
		return action.InputId is null
			? UNNotificationAction.FromIdentifier(action.Identifier, action.Title, options)
			: UNTextInputNotificationAction.FromIdentifier(
				action.Identifier,
				action.Title,
				options,
				action.InputButtonTitle ?? action.Title,
				action.InputPlaceholder ?? string.Empty);
	}

	private static UNNotificationAttachment? TryCreateAttachment(string source)
	{
		var installedPath = source.StartsWith("ms-appx:", StringComparison.OrdinalIgnoreCase)
			? Package.Current.InstalledPath
			: string.Empty;
		if (ResolveAttachmentPath(source, installedPath) is not { } path)
		{
			return null;
		}
		return UNNotificationAttachment.FromIdentifier(
			"uno.appnotifications.attachment",
			NSUrl.FromFilename(path),
			(NSDictionary?)null,
			out _);
	}

	private static NSString GetActionKey(string actionIdentifier, string name)
		=> new($"{actionIdentifier}.{name}");

	private static string GetString(NSDictionary dictionary, NSString key)
		=> dictionary.ObjectForKey(key)?.ToString() ?? string.Empty;

	private static string? GetOptionalString(NSDictionary dictionary, NSString key)
		=> dictionary.ObjectForKey(key)?.ToString() is { Length: > 0 } value ? value : null;

	private static bool GetBoolean(NSDictionary dictionary, NSString key)
		=> dictionary.ObjectForKey(key) is NSNumber number && number.BoolValue;
}