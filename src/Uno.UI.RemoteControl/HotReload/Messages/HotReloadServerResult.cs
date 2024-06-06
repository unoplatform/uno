using System;
using System.Linq;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// The result of a hot-reload operation on server.
/// </summary>
public enum HotReloadServerResult
{
	/// <summary>
	/// Hot-reload completed with no changes.
	/// </summary>
	NoChanges = 0,

	/// <summary>
	/// Successful hot-reload.
	/// </summary>
	Success = 1,

	/// <summary>
	/// Cannot hot-reload due to rude edit.
	/// </summary>
	RudeEdit = 2,

	/// <summary>
	/// Cannot hot-reload due to compilation errors.
	/// </summary>
	Failed = 3,

	/// <summary>
	/// We didn't get any response for that hot-reload operation, result might or might not have been sent to app.
	/// </summary>
	Aborted = 256,

	/// <summary>
	/// The dev-server failed to process the hot-reload sequence.
	/// </summary>
	InternalError = 512
}
