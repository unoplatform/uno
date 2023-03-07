using System.Runtime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles progress update events of an asynchronous action that provides progress updates.
/// </summary>
/// <typeparam name="TProgress">Progress data type.</typeparam>
/// <param name="asyncInfo">The asynchronous action.</param>
/// <param name="progressInfo">The progress information.</param>
public delegate void AsyncActionProgressHandler<TProgress>([In] IAsyncActionWithProgress<TProgress> asyncInfo, [In] TProgress progressInfo);
