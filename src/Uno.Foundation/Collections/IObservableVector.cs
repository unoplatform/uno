using System.Collections.Generic;

namespace Windows.Foundation.Collections;

/// <summary>
/// Notifies listeners of changes to the vector.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public partial interface IObservableVector<T> : IList<T>
{
	/// <summary>
	/// Occurs when the vector changes.
	/// </summary>
	event VectorChangedEventHandler<T> VectorChanged;
}
