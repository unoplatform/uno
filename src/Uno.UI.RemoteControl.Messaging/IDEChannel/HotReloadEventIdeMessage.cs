using System;
using System.Linq;
using Uno.UI.RemoteControl.Messaging.HotReload;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// A message sent by the IDE to the dev-server regarding hot-reload operations.
/// </summary>
/// <param name="Event">The kind of hot-reload message.</param>
public record HotReloadEventIdeMessage(HotReloadEvent Event) : IdeMessage(WellKnownScopes.HotReload);
