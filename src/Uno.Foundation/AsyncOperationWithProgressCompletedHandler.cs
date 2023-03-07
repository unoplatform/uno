using System.Runtime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles the completed event of an asynchronous operation that provides progress updates.
/// </summary>
/// <typeparam name="TResult">Result type.</typeparam>
/// <typeparam name="TProgress">Progress data type.</typeparam>
/// <param name="asyncInfo">The asynchronous operation.</param>
/// <param name="asyncStatus">One of the enumeration values.</param>
public delegate void AsyncOperationWithProgressCompletedHandler<TResult, TProgress>([In] IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, [In] AsyncStatus asyncStatus);
