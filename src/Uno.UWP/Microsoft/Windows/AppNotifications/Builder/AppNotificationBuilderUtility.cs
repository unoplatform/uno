#nullable enable

using System;
using System.Text;

namespace Microsoft.Windows.AppNotifications.Builder;

internal static class AppNotificationBuilderUtility
{
	public const int MaxPayloadCharacters = 5120;
	public const int MaxTextElements = 3;
	public const int MaxButtonElements = 5;
	public const int MaxInputElements = 5;
	public const int MaxSelectionElements = 5;

	public static string EncodeXml(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}

		var encoded = new StringBuilder(value.Length);
		foreach (var character in value)
		{
			encoded.Append(character switch
			{
				'&' => "&amp;",
				'\"' => "&quot;",
				'\'' => "&apos;",
				'<' => "&lt;",
				'>' => "&gt;",
				_ => character.ToString(),
			});
		}

		return encoded.ToString();
	}

	public static string GetAbsoluteUri(Uri value, string parameterName)
	{
		ArgumentNullException.ThrowIfNull(value, parameterName);
		if (!value.IsAbsoluteUri)
		{
			throw new ArgumentException("An absolute URI is required.", parameterName);
		}
		return value.AbsoluteUri;
	}

	public static string GetSoundEventUri(AppNotificationSoundEvent soundEvent)
	{
		return soundEvent switch
		{
			AppNotificationSoundEvent.IM => "ms-winsoundevent:Notification.IM",
			AppNotificationSoundEvent.Mail => "ms-winsoundevent:Notification.Mail",
			AppNotificationSoundEvent.Reminder => "ms-winsoundevent:Notification.Reminder",
			AppNotificationSoundEvent.SMS => "ms-winsoundevent:Notification.SMS",
			>= AppNotificationSoundEvent.Alarm and <= AppNotificationSoundEvent.Alarm10
				=> GetLoopingSoundUri("Alarm", soundEvent - AppNotificationSoundEvent.Alarm),
			>= AppNotificationSoundEvent.Call and <= AppNotificationSoundEvent.Call10
				=> GetLoopingSoundUri("Call", soundEvent - AppNotificationSoundEvent.Call),
			_ => "ms-winsoundevent:Notification.Default",
		};
	}

	private static string GetLoopingSoundUri(string eventName, int offset)
	{
		var suffix = offset == 0 ? string.Empty : ((int)offset + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
		return $"ms-winsoundevent:Notification.Looping.{eventName}{suffix}";
	}
}
