using System;
using System.Linq;

namespace Uno.UI.RemoteControl.HotReload;

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
