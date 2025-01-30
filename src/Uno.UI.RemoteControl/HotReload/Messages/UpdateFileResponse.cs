using System;
using System.Linq;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// In response to a <see cref="UpdateFile"/> request.
/// </summary>
/// <param name="RequestId"><see cref="UpdateFile.RequestId"/> of the request.</param>
/// <param name="FilePath">Actual path of the modified file.</param>
/// <param name="Result">Result of the edition.</param>
/// <param name="HotReloadCorrelationId">Optional correlation ID of pending hot-reload operation. Null if we don't expect this file update to produce any hot-reload result.</param>
public sealed record UpdateFileResponse(
	[property: JsonProperty] string RequestId,
	[property: JsonProperty] string FilePath,
	[property: JsonProperty] FileUpdateResult Result,
	[property: JsonProperty] string? Error = null,
	[property: JsonProperty] long? HotReloadCorrelationId = null) : IMessage
{
	public const string Name = nameof(UpdateFileResponse);

	[JsonIgnore]
	string IMessage.Scope => WellKnownScopes.HotReload;

	[JsonIgnore]
	string IMessage.Name => Name;
}
