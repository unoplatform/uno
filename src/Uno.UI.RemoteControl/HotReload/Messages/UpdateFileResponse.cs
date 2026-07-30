using System.Collections.Immutable;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// In response to a <see cref="UpdateFileRequest"/>.
/// </summary>
/// <param name="RequestId"><see cref="UpdateFileRequest.RequestId"/> of the request.</param>
/// <param name="GlobalError">Global error message while processing the request.</param>
/// <param name="Results">Results of the edition.</param>
/// <param name="HotReloadCorrelationId">Optional correlation ID of pending hot-reload operation. Null if we don't expect this file update to produce any hot-reload result.</param>
public sealed record UpdateFileResponse(
	string RequestId,
	string? GlobalError,
	ImmutableArray<FileEditResult> Results,
	long? HotReloadCorrelationId = null) : IMessage
{
	public const string Name = "UpdateFileResponse_2";

	string IMessage.Scope => WellKnownScopes.HotReload;

	string IMessage.Name => Name;
}
