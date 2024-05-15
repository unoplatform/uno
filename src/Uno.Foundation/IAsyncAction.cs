namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous action. This is the return type for many Windows Runtime asynchronous
/// methods that don't have a result object, and don't report ongoing progress.
/// </summary>
public partial interface IAsyncAction : IAsyncInfo
{
	/// <summary>
	/// Gets or sets the method that handles the action completed notification.
	/// </summary>
	AsyncActionCompletedHandler Completed { get; set; }

	/// <summary>
	/// Returns the results of the action.
	/// </summary>
	void GetResults();
}
