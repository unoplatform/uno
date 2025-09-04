#nullable enable
using System;

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
public record DevelopmentEnvironmentStatusIdeMessage(
	DevelopmentEnvironmentComponent Component,
	DevelopmentEnvironmentStatus Status,
	string? Description,
	string? Details,
	string? ErrorTrace,
	Command[] Actions) : IdeMessage("udei");

/// <summary>
/// Represents a component within the development environment, including its identifier and description.
/// </summary>
/// <param name="Id">
/// The unique identifier of the development environment component
/// (e.g. uno.dev_server)
/// </param>
/// <param name="Description">
/// A user-friendly description of the development environment component
/// (e.g. "The local server that allows the application to interact with the IDE and the file-system.").
/// </param>
public record DevelopmentEnvironmentComponent(string Id, string Description)
{
	// Well-known components of the development environment, this is **NOT** an exhaustive list!
	public static DevelopmentEnvironmentComponent Solution { get; } = new("uno.solution", "Load of the solution, resolution of nuget packages and validation of uno's SDK version.");
	public static DevelopmentEnvironmentComponent UnoCheck { get; } = new("uno.check", "Validates all external dependencies has been installed on the computer.");
	public static DevelopmentEnvironmentComponent DevServer { get; } = new("uno.dev_server", "The local server that allows the application to interact with the IDE and the file-system.");
}

/// <summary>
/// Possible statuses of a component within the uno development environment.
/// </summary>
public enum DevelopmentEnvironmentStatus
{
	/// <summary>
	/// The given component of the uno development environment is initializing.
	/// </summary>
	Initializing,

	/// <summary>
	/// The given component of the uno development environment is disabled.
	/// (e.g. the solution is not a Uno solution)
	/// </summary>
	Disabled,

	/// <summary>
	/// Indicates that the given component of the uno development environment is ready for use.
	/// </summary>
	Ready,

	/// <summary>
	///	Represents an error status of a given component of the uno development environment which we are trying to recover from.
	/// (e.g. dev-server crashed and is being restarted)
	/// </summary>
	Warning,

	/// <summary>
	/// Represents an error status of a given component of the uno development environment.
	/// Unlike the warning state, this is a final state.
	/// </summary>
	Error,
}
