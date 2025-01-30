#nullable enable
using System;
using System.Linq;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// A message sent by the sev-server to the IDE to notify the user.
/// </summary>
/// <param name="Title">Kind of the notification.</param>
/// <param name="Title">Title of the notification (might not be visible on all IDEs).</param>
/// <param name="Message">The message of the notification.</param>
/// <param name="Commands">For call-to-action notification, set of commands to show with the notification.</param>
public record NotificationRequestIdeMessage(NotificationKind Kind, string Title, string Message, Command[]? Commands = default) : IdeMessage(WellKnownScopes.Ide);

/// <summary>
/// Kind of <see cref="NotificationRequestIdeMessage"/>
/// </summary>
public enum NotificationKind
{
	Information,
	Error
}
