namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles the completed event of an asynchronous operation.
/// </summary>
/// <typeparam name="TResult">Result type.</typeparam>
/// <param name="asyncInfo">The asynchronous operation.</param>
/// <param name="asyncStatus">One of the enumeration values.</param>
public delegate void AsyncOperationCompletedHandler<TResult>(IAsyncOperation<TResult> asyncInfo, AsyncStatus asyncStatus);
