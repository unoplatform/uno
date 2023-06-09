using System.Runtime.InteropServices;

namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles the completed event of an asynchronous action that provides progress updates.
/// </summary>
/// <typeparam name="TProgress">Progress data type.</typeparam>
/// <param name="asyncInfo">The asynchronous action.</param>
/// <param name="asyncStatus">One of the enumeration values.</param>
public delegate void AsyncActionWithProgressCompletedHandler<TProgress>([In] IAsyncActionWithProgress<TProgress> asyncInfo, [In] AsyncStatus asyncStatus);
