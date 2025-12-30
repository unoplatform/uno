using System;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// In response to a <see cref="UpdateFileRequest"/>.
/// </summary>
/// <param name="RequestId"><see cref="UpdateFileRequest.RequestId"/> of the request.</param>
/// <param name="GlobalError">Global error message while processing the request.</param>
/// <param name="Results">Results of the edition.</param>
/// <param name="HotReloadCorrelationId">Optional correlation ID of pending hot-reload operation. Null if we don't expect this file update to produce any hot-reload result.</param>
public sealed record UpdateFileResponse(
	[property: JsonProperty] string RequestId,
	[property: JsonProperty] string? GlobalError,
	[property: JsonProperty] ImmutableArray<FileEditResult> Results,
	[property: JsonProperty] long? HotReloadCorrelationId = null) : IMessage
{
	public const string Name = "UpdateFileResponse_2";

	[JsonIgnore]
	string IMessage.Scope => WellKnownScopes.HotReload;

	[JsonIgnore]
	string IMessage.Name => Name;
}
