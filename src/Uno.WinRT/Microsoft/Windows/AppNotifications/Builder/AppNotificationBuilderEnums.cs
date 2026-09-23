// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

// Windows App SDK Reference dev/AppNotifications/AppNotificationBuilder/AppNotificationBuilder.idl, commit 6b178e79e59d28efb10ef5c8c68b051d2615c3e6

#nullable enable

using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications.Builder;

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationAudioLooping
{
	None = 0, // Audio will not loop
	Loop = 1, // Audio will loop for the duration of the AppNotification
}

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationButtonStyle
{
	Default = 0,
	Success = 1,
	Critical = 2,
}

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationDuration
{
	Default = 0, // Default value. AppNotification appears for a short while and then goes into Notification Center.
	Long = 1, // AppNotification stays on-screen for longer, and then goes into Notification Center.
}

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationImageCrop
{
	Default = 0, // Uses the default renderer to display the image
	Circle = 1, // Crops the image as a circle.
}

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationScenario
{
	Default = 0, // The normal AppNotification behavior. The AppNotification appears for a short duration, and then automatically dismisses into Notification Center.
	Reminder = 1, // The notification will stay on screen until the user dismisses it or takes action.
	Alarm = 2, // Alarms behave like Reminder, but alarms will additionally loop audio with a default alarm sound.
	IncomingCall = 3, // Incoming call notifications are displayed pre-expanded in a special call format and stay on the user's screen till dismissed.
	Urgent = 4, // Important notifications allow users to have more control over what 1st party and 3rd party apps can send them high-priority AppNotifications (urgent/important) that can break through Focus Assist.
}

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationSoundEvent
{
	Default = 0,
	IM = 1,
	Mail = 2,
	Reminder = 3,
	SMS = 4,
	Alarm = 5,
	Alarm2 = 6,
	Alarm3 = 7,
	Alarm4 = 8,
	Alarm5 = 9,
	Alarm6 = 10,
	Alarm7 = 11,
	Alarm8 = 12,
	Alarm9 = 13,
	Alarm10 = 14,
	Call = 15,
	Call2 = 16,
	Call3 = 17,
	Call4 = 18,
	Call5 = 19,
	Call6 = 20,
	Call7 = 21,
	Call8 = 22,
	Call9 = 23,
	Call10 = 24,
}
