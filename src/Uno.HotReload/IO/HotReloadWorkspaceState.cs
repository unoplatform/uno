namespace Uno.HotReload.IO;

/// <summary>
/// Lifecycle of the hot-reload compilation workspace, as reported to the
/// <see cref="WorkspaceGatedFileUpdater"/> by the hot-reload server processor.
/// </summary>
public enum HotReloadWorkspaceState
{
	/// <summary>
	/// No configuration received yet: it is not known whether a workspace will be created.
	/// </summary>
	NotConfigured,

	/// <summary>
	/// Hot reload is IDE-driven (e.g. Visual Studio): no workspace will be created and
	/// file updates flow directly to the editor.
	/// </summary>
	NoWorkspace,

	/// <summary>
	/// The workspace is initializing: the baseline solution is being captured from disk.
	/// </summary>
	Initializing,

	/// <summary>
	/// The workspace is ready: the baseline has been captured and the file-system observer is active.
	/// </summary>
	Ready,

	/// <summary>
	/// The workspace failed to initialize and will not recover on this connection.
	/// </summary>
	Failed,

	/// <summary>
	/// The processor (i.e. the connection) has been disposed.
	/// </summary>
	Disposed,
}
