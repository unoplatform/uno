using System.Runtime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles progress update events of an asynchronous operation that provides progress updates.
/// </summary>
/// <typeparam name="TResult">Result type.</typeparam>
/// <typeparam name="TProgress">Progress data type.</typeparam>
/// <param name="asyncInfo">The asynchronous operation.</param>
/// <param name="progressInfo">The progress information.</param>
public delegate void AsyncOperationProgressHandler<TResult, TProgress>([In] IAsyncOperationWithProgress<TResult, TProgress> asyncInfo, [In] TProgress progressInfo);
