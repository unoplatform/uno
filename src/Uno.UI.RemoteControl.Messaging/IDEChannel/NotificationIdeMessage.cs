#nullable enable
using System;
using System.Linq;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.Messaging.IDEChannel;

/// <summary>
/// A message sent by the sev-server to the IDE to notify the user.
/// </summary>
/// <param name="Title">Title of the notification.</param>
/// <param name="Message">The message of the notification.</param>
/// <param name="Commands">For call-to-action notification, set of commands to show with the notification.</param>
public record NotificationIdeMessage(string Title, string Message, Command[] Commands) : IdeMessage(WellKnownScopes.Ide);

/// <summary>
/// Description of a command to execute.
/// </summary>
/// <param name="Text">Text content to display to the user.</param>
/// <param name="Name">The name/id of the command (e.g. uno.hotreload.open_hotreload_window).</param>
/// <param name="Parameter">A json serialized parameter to execute the command.</param>
public record Command(string Text, string Name, string? Parameter = null);
