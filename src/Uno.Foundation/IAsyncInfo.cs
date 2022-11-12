using System;

namespace Windows.Foundation;

/// <summary>
/// Supports asynchronous actions and operations. IAsyncInfo is a base interface for <see cref="IAsyncAction"/>,
/// <see cref="IAsyncActionWithProgress{TProgress}"/>, <see cref="IAsyncOperation{TResult}"/> and <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/>,
/// each of which support combinations of return type and progress for an asynchronous method.
/// </summary>
public partial interface IAsyncInfo 
{
	/// <summary>
	/// Gets a string that describes an error condition of the asynchronous operation.
	/// </summary>
	Exception ErrorCode { get; }

	/// <summary>
	/// Gets the handle of the asynchronous operation.
	/// </summary>
	uint Id { get; }

	/// <summary>
	/// Gets a value that indicates the status of the asynchronous operation.
	/// </summary>
	AsyncStatus Status { get; }

	/// <summary>
	/// Cancels the asynchronous operation.
	/// </summary>
	void Cancel();

	/// <summary>
	/// Closes the asynchronous operation.	
	/// </summary>
	void Close();
}
