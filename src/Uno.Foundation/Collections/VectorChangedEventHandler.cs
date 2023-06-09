namespace Windows.Foundation.Collections;

/// <summary>
/// Represents the method that handles the changed event of an observable vector.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
/// <param name="sender">The observable vector that changed.</param>
/// <param name="event">The description of the change that occurred in the vector.</param>
public delegate void VectorChangedEventHandler<T>(
	IObservableVector<T> sender,
	IVectorChangedEventArgs @event);
