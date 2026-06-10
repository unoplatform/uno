using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Uno.Extensions;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// LEGACY Request to update a SINGLE file.
/// </summary>
public class UpdateSingleFileRequest : IMessage, IUpdateFileRequest
{
	public const string Name = "UpdateFile";

	/// <inheritdoc />
	public string RequestId { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Gets or sets the file system path to edit.
	/// </summary>
	public string FilePath { get; set; } = string.Empty;

	/// <summary>
	/// The old text to replace in the file, or `null` to create a new file (only if <see cref="IsCreateDeleteAllowed"/> is true).
	/// </summary>
	public string? OldText { get; set; }

	/// <summary>
	/// The new text to replace in the file, or `null` to delete the file (only if <see cref="IsCreateDeleteAllowed"/> is true).
	/// </summary>
	public string? NewText { get; set; }

	/// <summary>
	/// Indicates if the file can be created or deleted.
	/// </summary>
	public bool IsCreateDeleteAllowed { get; set; }

	/// <inheritdoc />
	public bool? ForceSaveOnDisk { get; set; }

	/// <inheritdoc />
	public bool IsForceHotReloadDisabled { get; set; }

	/// <inheritdoc />
	public TimeSpan? ForceHotReloadDelay { get; set; }

	/// <inheritdoc />
	public int? ForceHotReloadAttempts { get; set; }

	[JsonIgnore]
	public ImmutableArray<FileEdit> Edits => [new(FilePath, OldText, NewText, IsCreateDeleteAllowed)];

	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;

	[JsonIgnore]
	string IMessage.Name => Name;

	/// <summary>
	/// LEGACY, indicates if valid for the legacy processor to handle it.
	/// </summary>
	/// <returns></returns>
	[MemberNotNullWhen(true, nameof(FilePath), nameof(OldText), nameof(NewText))]
	public bool IsValid()
		=> !FilePath.IsNullOrEmpty() &&
			OldText is not null &&
			NewText is not null;
}
