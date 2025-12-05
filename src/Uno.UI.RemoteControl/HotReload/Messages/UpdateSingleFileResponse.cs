using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// LEGACY response to a LEGACY <see cref="UpdateSingleFileRequest"/>.
/// </summary>
/// <param name="RequestId"><see cref="UpdateSingleFileRequest"/> of the request.</param>
/// <param name="Result">Actual path of the modified file.</param>
/// <param name="Error">Error message if any.</param>
/// <param name="HotReloadCorrelationId">Optional correlation ID of pending hot-reload operation. Null if we don't expect this file update to produce any hot-reload result.</param>
public sealed record UpdateSingleFileResponse(
	[property: JsonProperty] string RequestId,
	[property: JsonProperty] string FilePath,
	[property: JsonProperty] FileUpdateResult Result,
	[property: JsonProperty] string? Error = null,
	[property: JsonProperty] long? HotReloadCorrelationId = null) : IMessage
{
	public const string Name = "UpdateFileResponse";

	[JsonIgnore]
	string IMessage.Scope => WellKnownScopes.HotReload;

	[JsonIgnore]
	string IMessage.Name => Name;
}
