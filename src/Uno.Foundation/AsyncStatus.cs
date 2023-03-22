namespace Windows.Foundation;

/// <summary>
/// Specifies the status of an asynchronous operation.
/// </summary>
public enum AsyncStatus
{
	/// <summary>
	/// The operation has started.
	/// </summary>
	Started = 0,

	/// <summary>
	/// The operation has completed.
	/// </summary>
	Completed = 1,

	/// <summary>
	/// The operation was canceled.
	/// </summary>
	Canceled = 2,

	/// <summary>
	/// The operation has encountered an error.
	/// </summary>
	Error = 3,
}
