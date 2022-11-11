namespace Windows.Foundation.Collections;

/// <summary>
/// Supports simple iteration over a collection.
/// </summary>
/// <typeparam name="T">Item type.</typeparam>
public partial interface IIterator<T>
{
	/// <summary>
	/// Gets the current item in the collection.
	/// </summary>
	T Current { get; }

	/// <summary>
	/// Gets a value that indicates whether the iterator
	/// refers to a current item or is at the end of the collection.
	/// </summary>
	bool HasCurrent { get; }

	/// <summary>
	/// Advances the iterator to the next item in the collection.
	/// </summary>
	/// <returns>
	/// True if the iterator refers to a valid item in the collection;
	/// false if the iterator passes the end of the collection.
	/// </returns>
	bool MoveNext();

	/// <summary>
	/// Retrieves multiple items from the iterator.
	/// </summary>
	/// <param name="items">An array that receives the items retrieved from the iterator.</param>
	/// <returns>
	/// The number of items that were retrieved. This value can be less than the size of items
	/// if the end of the iterator is reached.
	/// </returns>
	uint GetMany(T[] items);
}
