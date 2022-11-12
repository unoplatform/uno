namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous operation that can report progress updates to callers.
/// This is the return type for many Windows Runtime asynchronous methods that have results and also report progress.
/// </summary>
/// <typeparam name="TResult">Result type.</typeparam>
/// <typeparam name="TProgress">Progress data type.</typeparam>
public partial interface IAsyncOperationWithProgress<TResult, TProgress> : IAsyncInfo
{
	/// <summary>
	/// Gets or sets the method that handles the operation completed notification.
	/// </summary>
	AsyncOperationWithProgressCompletedHandler<TResult, TProgress> Completed { get; set; }
	
	/// <summary>
	/// Gets or sets the method that handles progress notifications.
	/// </summary>
	AsyncOperationProgressHandler<TResult, TProgress> Progress { get; set; }

	/// <summary>
	/// Returns the results of the operation.
	/// </summary>
	/// <returns>The results of the operation.</returns>
	TResult GetResults();
}
