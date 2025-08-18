using System;

namespace Uno.UI.RemoteControl.HotReload;

internal enum HotReloadMode
{
	/// <summary>
	/// Hot reload is not configured
	/// </summary>
	None = 0,

	/// <summary>
	/// Hot reload using Metadata updates
	/// </summary>
	/// <remarks>This can be metadata-updates pushed by either VS or the dev-server.</remarks>
	MetadataUpdates,
}

[Flags]
internal enum MetadataUpdatesSupport
{
	None,

	// Emitter

	/// <summary>
	/// Metadata-updates are emitted by the IDE
	/// </summary>
	Ide = 1 << 0,

	/// <summary>
	/// Metadata-updates are emitted by the dev-server by listening to the file system.
	/// </summary>
	DevServer = 1 << 1,

	// Channels

	/// <summary>
	/// Metadata-updates are pushed directly to the runtime (currently compatible only with the IDE emitter).
	/// </summary>
	Runtime = 1 << 4,

	/// <summary>
	/// Metadata-updates are sent through the App channel using AssemblyDeltaReload frame message (currently compatible only with the DevServer emitter).
	/// </summary>
	RemoteControl = 1 << 5,

	/// <summary>
	/// Metadata-updates are sent to the app through the debugger using the IDE channel (currently compatible only with the DevServer emitter).
	/// </summary>
	Debugger = 1 << 6,
}
