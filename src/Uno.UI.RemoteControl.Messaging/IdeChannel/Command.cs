#nullable enable
using System;
using System.Linq;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Description of a command to execute.
/// </summary>
/// <param name="Text">Text content to display to the user.</param>
/// <param name="Name">The name/id of the command (e.g. uno.hotreload.open_hotreload_window).</param>
/// <param name="Parameter">A json serialized parameter to execute the command.</param>
public record Command(string Text, string Name, string? Parameter = null);
