#nullable enable

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record WebAssemblyAppNotificationCommand(
	uint Id,
	string NativeTag,
	string Title,
	string Body,
	string Language,
	string Direction,
	string Icon,
	string Image,
	long? Timestamp,
	long? ExpirationTimestamp,
	bool Silent,
	bool RequireInteraction,
	string LaunchArgument,
	string? ProtocolUri,
	WebAssemblyAppNotificationActionCommand[] Actions,
	string[] UnsupportedFeatures);

internal sealed record WebAssemblyAppNotificationActionCommand(
	string Id,
	string Title,
	string Icon,
	string Argument,
	string? ProtocolUri);