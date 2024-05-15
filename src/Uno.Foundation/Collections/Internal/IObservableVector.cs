namespace Windows.Foundation.Collections;

internal interface IObservableVector
{
	/// <summary>
	/// Occurs when the vector changes.
	/// </summary>
	event VectorChangedEventHandler UntypedVectorChanged;

	object this[int index] { get; }

	int Count { get; }

	int IndexOf(object item);

	void Add(object item);

	void Insert(int index, object item);

	void RemoveAt(int index);
}
