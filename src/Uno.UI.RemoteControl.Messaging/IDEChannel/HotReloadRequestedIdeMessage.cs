#nullable enable

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// A message sent by the IDE to the dev-server to confirm a <see cref="ForceHotReloadIdeMessage"/> request has been processed.
/// </summary>
/// <param name="RequestId"><see cref="ForceHotReloadIdeMessage.CorrelationId"/> of the request.</param>
/// <param name="Result">Result of the request.</param>
public record HotReloadRequestedIdeMessage(long RequestId, Result Result) : IdeMessage(WellKnownScopes.HotReload);
