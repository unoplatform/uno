using System;

#nullable enable
namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Message sent to the IDE to update the status of a component within the uno development environment.
/// </summary>
/// <param name="Component">Identifier of the component to which this message refers.</param>
/// <param name="Status">The status of the component.</param>
/// <param name="Description">User-friendly description of the current status (e.g. "Searching for uno packages.").</param>
/// <param name="Details">The details (if any) for logging purposes (For error, this should contain the Exception.Message).</param>
/// <param name="ErrorTrace">For error cases, the stack trace for logging purposes.</param>
/// <param name="Actions">Set of actions the user can perform regarding the current status (a good practice is to always provide at least one action)</param>
public partial record DevelopmentEnvironmentStatusIdeMessage(
	DevelopmentEnvironmentComponent Component,
	DevelopmentEnvironmentStatus Status,
	string? Description,
	string? Details,
	string? ErrorTrace,
	Command[] Actions) : IdeMessage("udei");
