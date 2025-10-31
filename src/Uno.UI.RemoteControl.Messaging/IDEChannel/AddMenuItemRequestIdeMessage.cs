#nullable enable
using System;
using System.Linq;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Request to the IDE to add a menu item.
/// </summary>
/// <param name="Command">The command to add in the IDE menus.</param>
public record AddMenuItemRequestIdeMessage(Command Command) : IdeMessage(WellKnownScopes.Ide);
