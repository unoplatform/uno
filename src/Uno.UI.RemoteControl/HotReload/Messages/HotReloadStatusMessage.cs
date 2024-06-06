using System;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages;

public record HotReloadStatusMessage(
	[property: JsonProperty] HotReloadState State,
	[property: JsonProperty] IImmutableList<HotReloadServerOperationData> Operations)
	: IMessage
{
	public const string Name = nameof(HotReloadStatusMessage);

	/// <inheritdoc />
	[JsonProperty]
	public string Scope => WellKnownScopes.HotReload;

	/// <inheritdoc />
	[JsonProperty]
	string IMessage.Name => Name;
}

public record HotReloadServerOperationData(
	long Id,
	DateTimeOffset StartTime,
	ImmutableHashSet<string> FilePaths,
	DateTimeOffset? EndTime,
	HotReloadServerResult? Result);
