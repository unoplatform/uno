namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// LEGACY response to a LEGACY <see cref="UpdateSingleFileRequest"/>.
/// </summary>
/// <param name="RequestId"><see cref="UpdateSingleFileRequest"/> of the request.</param>
/// <param name="Result">Actual path of the modified file.</param>
/// <param name="Error">Error message if any.</param>
/// <param name="HotReloadCorrelationId">Optional correlation ID of pending hot-reload operation. Null if we don't expect this file update to produce any hot-reload result.</param>
public sealed record UpdateSingleFileResponse(
	string RequestId,
	string FilePath,
	FileUpdateResult Result,
	string? Error = null,
	long? HotReloadCorrelationId = null) : IMessage
{
	public const string Name = "UpdateFileResponse";

	string IMessage.Scope => WellKnownScopes.HotReload;

	string IMessage.Name => Name;
}
