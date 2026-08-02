#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections;

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
	string TextInputActionUriTemplate)
{
	private static readonly IEqualityComparer ArrayComparer = StructuralComparisons.StructuralEqualityComparer;

	public bool Equals(AppTaskContentSnapshot? other) =>
		ReferenceEquals(this, other)
		|| (other is not null
			&& Kind == other.Kind
			&& ArrayComparer.Equals(CompletedSteps, other.CompletedSteps)
			&& ExecutingStep == other.ExecutingStep
			&& ImageUri == other.ImageUri
			&& TextSummary == other.TextSummary
			&& ArrayComparer.Equals(GeneratedAssets, other.GeneratedAssets)
			&& ArrayComparer.Equals(Buttons, other.Buttons)
			&& Question == other.Question
			&& TextInputPlaceholder == other.TextInputPlaceholder
			&& TextInputActionUriTemplate == other.TextInputActionUriTemplate);

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(Kind);
		hash.Add(ArrayComparer.GetHashCode(CompletedSteps));
		hash.Add(ExecutingStep, StringComparer.Ordinal);
		hash.Add(ImageUri);
		hash.Add(TextSummary, StringComparer.Ordinal);
		hash.Add(ArrayComparer.GetHashCode(GeneratedAssets));
		hash.Add(ArrayComparer.GetHashCode(Buttons));
		hash.Add(Question, StringComparer.Ordinal);
		hash.Add(TextInputPlaceholder, StringComparer.Ordinal);
		hash.Add(TextInputActionUriTemplate, StringComparer.Ordinal);
		return hash.ToHashCode();
	}
}

internal sealed record AppTaskButtonSnapshot(string Text, Uri ActionUri);

internal sealed record AppTaskResultAssetSnapshot(string Name, string Context, Uri IconUri, Uri AssetUri);
