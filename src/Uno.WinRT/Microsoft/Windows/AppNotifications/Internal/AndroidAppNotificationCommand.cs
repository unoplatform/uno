#nullable enable

using System;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record AndroidAppNotificationCommand(
	int NativeId,
	string? NativeTag,
	string Title,
	string Body,
	string Attribution,
	string Group,
	string? LargeIconSource,
	string? BigPictureSource,
	string BigPictureAlternateText,
	long? DisplayTimestampMilliseconds,
	long? TimeoutMilliseconds,
	bool MuteAudio,
	bool SuppressDisplay,
	bool HighPriority,
	string ProgressTitle,
	string ProgressStatus,
	string ProgressValueString,
	int? ProgressValue,
	AndroidAppNotificationActivationCommand BodyActivation,
	AndroidAppNotificationActionCommand[] Actions,
	string[] UnsupportedFeatures);

internal sealed record AndroidAppNotificationActivationCommand(string Argument, string? ProtocolUri);

internal sealed record AndroidAppNotificationActionCommand(
	string Content,
	string Argument,
	string? ProtocolUri,
	string? InputId,
	string? InputLabel);
