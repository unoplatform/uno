using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications.Builder;

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationAudioLooping
{
	None = 0,
	Loop = 1,
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
	Default = 0,
	Long = 1,
}

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationImageCrop
{
	Default = 0,
	Circle = 1,
}

[ContractVersion(typeof(AppNotificationBuilderContract), 1 * 0x10000u)]
public enum AppNotificationScenario
{
	Default = 0,
	Reminder = 1,
	Alarm = 2,
	IncomingCall = 3,
	Urgent = 4,
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
