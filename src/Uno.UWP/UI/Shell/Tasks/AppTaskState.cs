namespace Windows.UI.Shell.Tasks;

/// <summary>
/// Defines constants that specify the state of an app task.
/// </summary>
public enum AppTaskState
{
	/// <summary>
	/// Task is created and running.
	/// </summary>
	Running = 0,

	/// <summary>
	/// Task completed.
	/// </summary>
	Completed = 1,

	/// <summary>
	/// Task is paused and needs user attention to continue.
	/// </summary>
	NeedsAttention = 2,

	/// <summary>
	/// Task is paused, but can be resumed without user intervention.
	/// </summary>
	Paused = 3,

	/// <summary>
	/// Task stopped permanently because of an unrecoverable error.
	/// </summary>
	Error = 4,
}
