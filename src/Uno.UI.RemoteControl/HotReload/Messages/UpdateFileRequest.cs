using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// Request to update (including add or remove) one or more files.
/// </summary>
public class UpdateFileRequest : IMessage, IUpdateFileRequest
{
	public const string Name = nameof(UpdateFileRequest);

	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;

	[JsonIgnore]
	string IMessage.Name => Name;

	/// <inheritdoc />
	public string RequestId { get; set; } = Guid.NewGuid().ToString();

	/// <inheritdoc />
	public bool? ForceSaveOnDisk { get; set; }

	/// <inheritdoc />
	public bool IsForceHotReloadDisabled { get; set; }

	/// <inheritdoc />
	public TimeSpan? ForceHotReloadDelay { get; set; }

	/// <inheritdoc />
	public int? ForceHotReloadAttempts { get; set; }

	/// <summary>
	/// Gets or sets the collection of file edits to be applied.
	/// </summary>
	public ImmutableArray<FileEdit> Edits { get; set; } = ImmutableArray<FileEdit>.Empty;
}
