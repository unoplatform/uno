// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationBuilderUtility.h, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using System.Text;

namespace Microsoft.Windows.AppNotifications.Builder;

internal static partial class AppNotificationBuilderUtility
{
	public const int MaxPayloadCharacters = 5120;
	public const int MaxTextElements = 3;
	public const int MaxButtonElements = 5;
	public const int MaxInputElements = 5;
	public const int MaxSelectionElements = 5;

	public static string GetWinSoundEventString(AppNotificationSoundEvent soundEvent)
	{
		return soundEvent switch
		{
			AppNotificationSoundEvent.IM => "ms-winsoundevent:Notification.IM",
			AppNotificationSoundEvent.Mail => "ms-winsoundevent:Notification.Mail",
			AppNotificationSoundEvent.Reminder => "ms-winsoundevent:Notification.Reminder",
			AppNotificationSoundEvent.SMS => "ms-winsoundevent:Notification.SMS",
			AppNotificationSoundEvent.Alarm => "ms-winsoundevent:Notification.Looping.Alarm",
			AppNotificationSoundEvent.Alarm2 => "ms-winsoundevent:Notification.Looping.Alarm2",
			AppNotificationSoundEvent.Alarm3 => "ms-winsoundevent:Notification.Looping.Alarm3",
			AppNotificationSoundEvent.Alarm4 => "ms-winsoundevent:Notification.Looping.Alarm4",
			AppNotificationSoundEvent.Alarm5 => "ms-winsoundevent:Notification.Looping.Alarm5",
			AppNotificationSoundEvent.Alarm6 => "ms-winsoundevent:Notification.Looping.Alarm6",
			AppNotificationSoundEvent.Alarm7 => "ms-winsoundevent:Notification.Looping.Alarm7",
			AppNotificationSoundEvent.Alarm8 => "ms-winsoundevent:Notification.Looping.Alarm8",
			AppNotificationSoundEvent.Alarm9 => "ms-winsoundevent:Notification.Looping.Alarm9",
			AppNotificationSoundEvent.Alarm10 => "ms-winsoundevent:Notification.Looping.Alarm10",
			AppNotificationSoundEvent.Call => "ms-winsoundevent:Notification.Looping.Call",
			AppNotificationSoundEvent.Call2 => "ms-winsoundevent:Notification.Looping.Call2",
			AppNotificationSoundEvent.Call3 => "ms-winsoundevent:Notification.Looping.Call3",
			AppNotificationSoundEvent.Call4 => "ms-winsoundevent:Notification.Looping.Call4",
			AppNotificationSoundEvent.Call5 => "ms-winsoundevent:Notification.Looping.Call5",
			AppNotificationSoundEvent.Call6 => "ms-winsoundevent:Notification.Looping.Call6",
			AppNotificationSoundEvent.Call7 => "ms-winsoundevent:Notification.Looping.Call7",
			AppNotificationSoundEvent.Call8 => "ms-winsoundevent:Notification.Looping.Call8",
			AppNotificationSoundEvent.Call9 => "ms-winsoundevent:Notification.Looping.Call9",
			AppNotificationSoundEvent.Call10 => "ms-winsoundevent:Notification.Looping.Call10",
			_ => "ms-winsoundevent:Notification.Default",
		};
	}

	public static string EncodeXml(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		var encodedValue = new StringBuilder(value.Length);
		foreach (var character in value)
		{
			encodedValue.Append(character switch
			{
				'&' => "&amp;",
				'\"' => "&quot;",
				'<' => "&lt;",
				'>' => "&gt;",
				'\'' => "&apos;",
				_ => character.ToString(),
			});
		}

		return encodedValue.ToString();
	}

	// Decoding process based off the Windows Community Toolkit:
	// https://github.com/CommunityToolkit/WindowsCommunityToolkit/blob/rel/7.1.0/Microsoft.Toolkit.Uwp.Notifications/Toasts/ToastArguments.cs#L389inline
	// Uno: argument encoding and decoding are shared with payload parsing in AppNotificationArgumentCodec.
}
