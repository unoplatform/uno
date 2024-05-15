namespace Windows.Foundation;

/// <summary>
/// Represents a method that handles general events.
/// </summary>
/// <typeparam name="TSender">Sender type.</typeparam>
/// <typeparam name="TResult">Event data type.</typeparam>
/// <param name="sender">The event source.</param>
/// <param name="args">The event data. If there is no event data, this parameter will be null.</param>
public delegate void TypedEventHandler<TSender, TResult>(
	TSender sender,
	TResult args
);
