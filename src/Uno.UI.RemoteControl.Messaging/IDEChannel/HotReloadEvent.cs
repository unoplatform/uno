using System;
using System.Linq;

namespace Uno.UI.RemoteControl.Messaging.HotReload;

public enum HotReloadEvent
{
	/// <summary>
	/// Hot-reload is not available (and will not be).
	/// WARNING This is raised only by dev-server, not by the IDE
	/// </summary>
	/// <remarks>This can be the case if initialization failed.</remarks>
	Disabled,

	/// <summary>
	/// Initializing hot-reload processor (e.g. loading workspace for dev-server based HR).
	/// WARNING This is raised only by dev-server, not by the IDE.
	/// </summary>
	Initializing,

	/// <summary>
	/// Processor is ready, waiting for files changes to trigger.
	/// WARNING This is raised only by dev-server, not by the IDE.
	/// </summary>
	Ready,

	/// <summary>
	/// Processor is processing files changes.
	/// WARNING This is raised only by dev-server, not by the IDE (unless we request a force hot-reload from the client).
	/// </summary>
	ProcessingFiles,

	/// <summary>
	/// Hot-reload completed (errors might come after!)
	/// </summary>
	Completed,

	/// <summary>
	/// Hot-reload completed with no changes
	/// </summary>
	NoChanges,

	/// <summary>
	/// Hot-reload failed (usually due to compilation errors)
	/// </summary>
	Failed,

	RudeEdit,

	/// <summary>
	/// Hot-reload cannot be applied (rude edit), a dialog has been prompt to the user ... and he just gave a response!
	/// WARNING This is raised only by IDE, not by the dev-server.
	/// </summary>
	RudeEditDialogButton
}
