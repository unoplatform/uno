#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record LinuxAppNotificationCommand(
	uint Id,
	string Summary,
	string Body,
	string AppIcon,
	string Category,
	byte Urgency,
	int ExpireTimeoutMilliseconds,
	bool MuteAudio,
	bool SuppressDisplay,
	int? ProgressPercentage,
	string BodyActionKey,
	string LaunchArgument,
	string? ProtocolUri,
	LinuxAppNotificationActionCommand[] Actions,
	string[] UnsupportedFeatures);

internal sealed record LinuxAppNotificationActionCommand(
	string Key,
	string Title,
	string Argument,
	string? ProtocolUri);