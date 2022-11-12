namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles the completed event of an asynchronous action.
/// </summary>
/// <param name="asyncInfo">The asynchronous action.</param>
/// <param name="asyncStatus">One of the enumeration values.</param>
public delegate void AsyncActionCompletedHandler(IAsyncAction asyncInfo, AsyncStatus asyncStatus);
