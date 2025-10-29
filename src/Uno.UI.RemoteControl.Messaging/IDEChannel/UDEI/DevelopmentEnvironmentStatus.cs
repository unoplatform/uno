#nullable enable
namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

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
