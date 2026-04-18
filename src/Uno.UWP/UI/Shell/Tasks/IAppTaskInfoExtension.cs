using Windows.UI.Shell.Tasks;

namespace Uno.UI.Shell.Tasks;

/// <summary>
/// Extension interface for platform-specific implementations of app task
/// integration with the operating system's shell (taskbar/dock).
/// </summary>
internal interface IAppTaskInfoExtension
{
	/// <summary>
	/// Returns whether app tasks are supported on the current platform.
	/// </summary>
	bool IsSupported();

	/// <summary>
	/// Retrieves all tasks that the app has created.
	/// </summary>
	AppTaskInfo[] FindAll();

	/// <summary>
	/// Called when a new app task has been created.
	/// </summary>
	void OnTaskCreated(AppTaskInfo task);

	/// <summary>
	/// Called when an app task has been updated (state, titles, or content).
	/// </summary>
	void OnTaskUpdated(AppTaskInfo task);

	/// <summary>
	/// Called when an app task has been removed.
	/// </summary>
	void OnTaskRemoved(AppTaskInfo task);
}
