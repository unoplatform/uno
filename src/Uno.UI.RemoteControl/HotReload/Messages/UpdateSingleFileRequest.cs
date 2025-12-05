using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
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
	[JsonProperty]
	public string RequestId { get; set; } = Guid.NewGuid().ToString();

	/// <summary>
	/// Gets or sets the file system path to edit.
	/// </summary>
	[JsonProperty]
	public string FilePath { get; set; } = string.Empty;

	/// <summary>
	/// The old text to replace in the file, or `null` to create a new file (only if <see cref="IsCreateDeleteAllowed"/> is true).
	/// </summary>
	[JsonProperty]
	public string? OldText { get; set; }

	/// <summary>
	/// The new text to replace in the file, or `null` to delete the file (only if <see cref="IsCreateDeleteAllowed"/> is true).
	/// </summary>
	[JsonProperty]
	public string? NewText { get; set; }

	/// <summary>
	/// Indicates if the file can be created or deleted.
	/// </summary>
	[JsonProperty]
	public bool IsCreateDeleteAllowed { get; set; }

	/// <inheritdoc />
	public bool? ForceSaveOnDisk { get; set; }

	/// <inheritdoc />
	[JsonProperty]
	public bool IsForceHotReloadDisabled { get; set; }

	/// <inheritdoc />
	[JsonProperty]
	public TimeSpan? ForceHotReloadDelay { get; set; }

	/// <inheritdoc />
	[JsonProperty]
	public int? ForceHotReloadAttempts { get; set; }

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
