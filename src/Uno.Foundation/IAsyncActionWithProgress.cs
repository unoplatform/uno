namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous action that can report progress updates to callers. This is the return type
/// for all Windows Runtime asynchronous methods that don't have a result object, but do report progress to callback listeners.
/// </summary>
/// <typeparam name="TProgress">Progress data type.</typeparam>
public partial interface IAsyncActionWithProgress<TProgress> : IAsyncInfo
{
	/// <summary>
	/// Gets or sets the method that handles the action completed notification.
	/// </summary>
	AsyncActionWithProgressCompletedHandler<TProgress> Completed { get; set; }

	/// <summary>
	/// Gets or sets the callback method that receives progress notification.
	/// </summary>
	AsyncActionProgressHandler<TProgress> Progress { get; set; }

	/// <summary>
	/// Returns the results of the action.
	/// </summary>
	void GetResults();
}
