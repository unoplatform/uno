#nullable enable
#pragma warning disable CS8305

using System;

namespace Windows.UI.Shell.Tasks;

internal sealed record AppTaskInfoSnapshot(
	string Id,
	string Title,
	string Subtitle,
	Uri DeepLink,
	Uri IconUri,
	AppTaskState State,
	DateTimeOffset StartTime,
	DateTimeOffset? EndTime,
	bool HiddenByUser,
	AppTaskContentSnapshot Content);

internal sealed record AppTaskContentSnapshot(
	AppTaskContentKind Kind,
	string[] CompletedSteps,
	string ExecutingStep,
	Uri? ImageUri,
	string TextSummary,
	AppTaskResultAssetSnapshot[] GeneratedAssets,
	AppTaskButtonSnapshot[] Buttons,
	string Question,
	string TextInputPlaceholder,
	string TextInputActionUriTemplate);

internal sealed record AppTaskButtonSnapshot(string Text, Uri ActionUri);

internal sealed record AppTaskResultAssetSnapshot(string Name, string Context, Uri IconUri, Uri AssetUri);
