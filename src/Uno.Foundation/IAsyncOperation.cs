namespace Windows.Foundation;

/// <summary>
/// Represents an asynchronous operation, which returns a result upon completion. This is the return type
/// for many Windows Runtime asynchronous methods that have results but don't report progress.
/// </summary>
/// <typeparam name="TResult">Result type.</typeparam>
public partial interface IAsyncOperation<TResult> : IAsyncInfo
{
	/// <summary>
	/// Gets or sets the method that handles the operation completed notification.
	/// </summary>
	AsyncOperationCompletedHandler<TResult> Completed { get; set; }

	/// <summary>
	/// Returns the results of the operation.
	/// </summary>
	/// <returns>The results of the operation.</returns>
	TResult GetResults();
}
