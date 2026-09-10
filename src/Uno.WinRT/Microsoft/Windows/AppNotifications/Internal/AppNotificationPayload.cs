#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.Windows.AppNotifications.Builder;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed record AppNotificationPayload(
	string LaunchArgument,
	ImmutableDictionary<string, string> LaunchArguments,
	AppNotificationScenario Scenario,
	AppNotificationDuration Duration,
	DateTimeOffset? DisplayTimestamp,
	bool UseButtonStyle,
	string ActivationType,
	string ProtocolActivationTargetApplicationPfn,
	string Language,
	string BaseUri,
	bool AddImageQuery,
	ImmutableArray<AppNotificationTextData> Texts,
	AppNotificationTextData? Attribution,
	ImmutableArray<AppNotificationImageData> Images,
	ImmutableArray<AppNotificationProgressData> ProgressBars,
	ImmutableArray<AppNotificationInputData> Inputs,
	ImmutableArray<AppNotificationActionData> Actions,
	AppNotificationAudioData? Audio)
{
	public AppNotificationTextData? Title => Texts.Length > 0 ? Texts[0] : null;

	public AppNotificationTextData? Body => Texts.Length > 1 ? Texts[1] : null;
}

internal sealed record AppNotificationTextData(
	string Content,
	string Language,
	int? MaxLines,
	bool IncomingCallAlignment);

internal sealed record AppNotificationImageData(
	string Source,
	string AlternateText,
	AppNotificationImagePlacement Placement,
	AppNotificationImageCrop Crop,
	bool AddImageQuery);

internal sealed record AppNotificationProgressData(
	string? Title,
	string Status,
	string Value,
	string? ValueStringOverride);

internal sealed record AppNotificationInputData(
	string Id,
	AppNotificationInputKind Kind,
	string Title,
	string PlaceHolderText,
	string DefaultInput,
	ImmutableArray<AppNotificationSelectionData> Selections);

internal sealed record AppNotificationSelectionData(string Id, string Content);

internal sealed record AppNotificationActionData(
	string Content,
	string RawArguments,
	ImmutableDictionary<string, string> Arguments,
	string ActivationType,
	string ProtocolActivationTargetApplicationPfn,
	bool ContextMenuPlacement,
	string ImageUri,
	string InputId,
	AppNotificationButtonStyle ButtonStyle,
	string ToolTip,
	bool PendingUpdate);

internal sealed record AppNotificationAudioData(string Source, bool Loop, bool Silent);

internal enum AppNotificationImagePlacement
{
	Inline,
	Hero,
	AppLogoOverride,
}

internal enum AppNotificationInputKind
{
	Text,
	Selection,
}
