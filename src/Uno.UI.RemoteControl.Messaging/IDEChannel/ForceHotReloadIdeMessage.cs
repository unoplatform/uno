#nullable enable

using System.Collections.Generic;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public record ForceHotReloadIdeMessage(long CorrelationId) : IdeMessageWithCorrelationId(CorrelationId, WellKnownScopes.HotReload);
