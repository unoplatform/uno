#nullable disable

using Windows.Foundation.Metadata;

namespace Windows.Foundation.Collections
{
	public enum CollectionChange
	{
		/// <summary>
		/// The collection is changed.
		/// </summary>
		Reset = 0,

		/// <summary>
		/// An item is added to the collection.
		/// </summary>
		ItemInserted = 1,

		/// <summary>
		/// An item is removed from the collection.
		/// </summary>
		ItemRemoved = 2,

		/// <summary>
		/// An item is changed in the collection.
		/// </summary>
		ItemChanged = 3
	}
}