#nullable enable
#pragma warning disable CS8305

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Uno.Helpers.Serialization;

namespace Windows.UI.Shell.Tasks;

internal static class AppTaskInfoSerializer
{
	private const int CurrentVersion = 1;

	internal static string Serialize(AppTaskInfoSnapshot[] tasks)
	{
		var document = new AppTaskStoreDocument
		{
			Version = CurrentVersion,
			Tasks = tasks.Select(ToStorage).ToArray(),
		};

		return JsonHelper.Serialize(document, AppTaskInfoJsonContext.Default);
	}

	internal static AppTaskInfoSnapshot[] Deserialize(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var document = JsonHelper.Deserialize<AppTaskStoreDocument>(value, AppTaskInfoJsonContext.Default)
			?? throw new InvalidDataException("The persisted app task document is empty.");

		if (document.Version != CurrentVersion)
		{
			throw new InvalidDataException(
				$"The persisted app task document has unsupported version {document.Version}. Expected {CurrentVersion}.");
		}

		var tasks = document.Tasks
			?? throw new InvalidDataException("The persisted app task collection is missing.");
		if (tasks.Any(static task => task is null))
		{
			throw new InvalidDataException("The persisted app task collection contains a null task.");
		}

		return tasks.Select(static task => FromStorage(task!)).ToArray();
	}

	private static AppTaskStoreItem ToStorage(AppTaskInfoSnapshot task) => new()
	{
		Id = task.Id,
		Title = task.Title,
		Subtitle = task.Subtitle,
		DeepLink = task.DeepLink.OriginalString,
		IconUri = task.IconUri.OriginalString,
		State = (int)task.State,
		StartTime = task.StartTime.ToString("O", CultureInfo.InvariantCulture),
		EndTime = task.EndTime?.ToString("O", CultureInfo.InvariantCulture),
		HiddenByUser = task.HiddenByUser,
		Content = new()
		{
			Kind = (int)task.Content.Kind,
			CompletedSteps = (string[])task.Content.CompletedSteps.Clone(),
			ExecutingStep = task.Content.ExecutingStep,
			ImageUri = task.Content.ImageUri?.OriginalString,
			TextSummary = task.Content.TextSummary,
			GeneratedAssets = task.Content.GeneratedAssets.Select(static asset => new AppTaskStoreResultAsset
			{
				Name = asset.Name,
				Context = asset.Context,
				IconUri = asset.IconUri.OriginalString,
				AssetUri = asset.AssetUri.OriginalString,
			}).ToArray(),
			Buttons = task.Content.Buttons.Select(static button => new AppTaskStoreButton
			{
				Text = button.Text,
				ActionUri = button.ActionUri.OriginalString,
			}).ToArray(),
			Question = task.Content.Question,
			TextInputPlaceholder = task.Content.TextInputPlaceholder,
			TextInputActionUriTemplate = task.Content.TextInputActionUriTemplate,
		},
	};

	private static AppTaskInfoSnapshot FromStorage(AppTaskStoreItem task)
	{
		if (string.IsNullOrEmpty(task.Id)
			|| string.IsNullOrEmpty(task.DeepLink)
			|| string.IsNullOrEmpty(task.IconUri))
		{
			throw new InvalidDataException("A persisted app task is missing a required value.");
		}

		var content = task.Content ?? throw new InvalidDataException($"Persisted app task '{task.Id}' has no content.");

		// The store mirrors what the public API produces, so it applies the same normalization instead
		// of treating values the API accepts as corruption.
		var completedSteps = content.CompletedSteps is { } steps
			? steps.Select(static step => step ?? string.Empty).ToArray()
			: Array.Empty<string>();
		var buttons = content.Buttons?.Where(static button => button is not null).ToArray()
			?? Array.Empty<AppTaskStoreButton>();
		var generatedAssets = content.GeneratedAssets?.Where(static asset => asset is not null).ToArray()
			?? Array.Empty<AppTaskStoreResultAsset>();

		return new(
			task.Id,
			task.Title ?? string.Empty,
			task.Subtitle ?? string.Empty,
			CreateAbsoluteUri(task.DeepLink, nameof(task.DeepLink)),
			CreateAbsoluteUri(task.IconUri, nameof(task.IconUri)),
			ParseState(task.State),
			ParseDateTimeOffset(task.StartTime, nameof(task.StartTime)),
			task.EndTime is null ? null : ParseDateTimeOffset(task.EndTime, nameof(task.EndTime)),
			task.HiddenByUser,
			new(
				ParseContentKind(content.Kind),
				completedSteps,
				content.ExecutingStep ?? string.Empty,
				content.ImageUri is null ? null : CreateAbsoluteUri(content.ImageUri, nameof(content.ImageUri)),
				content.TextSummary ?? string.Empty,
				generatedAssets
					.Select(static asset => new AppTaskResultAssetSnapshot(
						asset!.Name ?? string.Empty,
						asset.Context ?? string.Empty,
						CreateAbsoluteUri(asset.IconUri, nameof(asset.IconUri)),
						CreateAbsoluteUri(asset.AssetUri, nameof(asset.AssetUri))))
					.ToArray(),
				buttons
					.Select(static button => new AppTaskButtonSnapshot(
						button!.Text ?? string.Empty,
						CreateAbsoluteUri(button.ActionUri, nameof(button.ActionUri))))
					.ToArray(),
				content.Question ?? string.Empty,
				content.TextInputPlaceholder ?? string.Empty,
				content.TextInputActionUriTemplate ?? string.Empty));
	}

	private static AppTaskState ParseState(int value) => (AppTaskState)value;

	private static AppTaskContentKind ParseContentKind(int value) => value switch
	{
		(int)AppTaskContentKind.SequenceOfSteps => AppTaskContentKind.SequenceOfSteps,
		(int)AppTaskContentKind.PreviewThumbnail => AppTaskContentKind.PreviewThumbnail,
		(int)AppTaskContentKind.TextSummary => AppTaskContentKind.TextSummary,
		(int)AppTaskContentKind.GeneratedAssets => AppTaskContentKind.GeneratedAssets,
		(int)AppTaskContentKind.None => AppTaskContentKind.None,
		_ => throw new InvalidDataException($"Persisted app task content kind '{value}' is invalid."),
	};

	private static DateTimeOffset ParseDateTimeOffset(string? value, string propertyName)
	{
		if (value is null
			|| !DateTimeOffset.TryParseExact(
				value,
				"O",
				CultureInfo.InvariantCulture,
				DateTimeStyles.RoundtripKind,
				out var result))
		{
			throw new InvalidDataException($"Persisted app task property '{propertyName}' is invalid.");
		}

		return result;
	}

	private static Uri CreateAbsoluteUri(string? value, string propertyName)
	{
		if (!Uri.TryCreate(value, UriKind.Absolute, out var result))
		{
			throw new InvalidDataException($"Persisted app task URI property '{propertyName}' is invalid.");
		}

		return result;
	}
}

internal sealed class AppTaskStoreDocument
{
	public int Version { get; set; }

	public AppTaskStoreItem[]? Tasks { get; set; } = Array.Empty<AppTaskStoreItem>();
}

internal sealed class AppTaskStoreItem
{
	public string? Id { get; set; }

	public string? Title { get; set; }

	public string? Subtitle { get; set; }

	public string? DeepLink { get; set; }

	public string? IconUri { get; set; }

	public int State { get; set; }

	public string? StartTime { get; set; }

	public string? EndTime { get; set; }

	public bool HiddenByUser { get; set; }

	public AppTaskStoreContent? Content { get; set; }
}

internal sealed class AppTaskStoreContent
{
	public int Kind { get; set; }

	public string[]? CompletedSteps { get; set; }

	public string? ExecutingStep { get; set; }

	public string? ImageUri { get; set; }

	public string? TextSummary { get; set; }

	public AppTaskStoreResultAsset[]? GeneratedAssets { get; set; }

	public AppTaskStoreButton[]? Buttons { get; set; }

	public string? Question { get; set; }

	public string? TextInputPlaceholder { get; set; }

	public string? TextInputActionUriTemplate { get; set; }
}

internal sealed class AppTaskStoreResultAsset
{
	public string? Name { get; set; }

	public string? Context { get; set; }

	public string? IconUri { get; set; }

	public string? AssetUri { get; set; }
}

internal sealed class AppTaskStoreButton
{
	public string? Text { get; set; }

	public string? ActionUri { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppTaskStoreDocument))]
internal partial class AppTaskInfoJsonContext : JsonSerializerContext
{
}
