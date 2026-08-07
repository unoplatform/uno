#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record AppleAppNotificationCommand(
	uint Id,
	string RequestIdentifier,
	string CategoryIdentifier,
	string Title,
	string Subtitle,
	string Body,
	string ThreadIdentifier,
	string AttachmentSource,
	string LaunchArgument,
	string? ProtocolUri,
	bool MuteAudio,
	bool SuppressDisplay,
	bool HighPriority,
	AppleAppNotificationActionCommand[] Actions,
	string[] UnsupportedFeatures);

internal sealed record AppleAppNotificationActionCommand(
	string Identifier,
	string Title,
	string Argument,
	string? ProtocolUri,
	string? InputId,
	string? InputButtonTitle,
	string? InputPlaceholder,
	bool Destructive,
	bool Foreground);