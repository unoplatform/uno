using System;
using System.Collections.Immutable;
using Newtonsoft.Json;

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
	[JsonProperty]
	public string RequestId { get; set; } = Guid.NewGuid().ToString();

	/// <inheritdoc />
	[JsonProperty]
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

	/// <summary>
	/// Gets or sets the collection of file edits to be applied.
	/// </summary>
	[JsonProperty]
	public ImmutableArray<FileEdit> Edits { get; set; } = ImmutableArray<FileEdit>.Empty;
}
