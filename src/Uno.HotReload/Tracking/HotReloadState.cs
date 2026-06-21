namespace Uno.HotReload.Tracking;

/// <summary>
/// Represents the current state of the hot reload feature in the IDE or server.
/// </summary>
/// <remarks>Values of this enum **MUST** match the Uno.UI.RemoteControl.HotReload.HotReloadState</remarks>
/// <remarks>Use this enumeration to determine whether hot reload is available, initializing, ready to process
/// changes, or currently processing a changeset. The state can be used to control UI elements or workflow based on the
/// server's ability to accept and apply code changes.</remarks>
public enum HotReloadState
{
	/// <summary>
	/// Hot reload is disabled.
	/// Usually this indicates that the server failed to load the workspace.
	/// </summary>
	Disabled = -1,

	/// <summary>
	/// The server is initializing.
	/// Usually this indicates that the server is loading the workspace.
	/// </summary>
	Initializing = 0,

	/// <summary>
	/// Indicates that the IDE/server is ready to process changes.
	/// </summary>
	Ready = 1,

	/// <summary>
	/// The IDE/server is computing changeset.
	/// </summary>
	Processing = 2
}
