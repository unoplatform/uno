namespace Windows.Foundation.Collections;

/// <summary>
/// Provides data for the changed event of a vector.
/// </summary>
public partial interface IVectorChangedEventArgs
{
	/// <summary>
	/// Gets the type of change that occurred in the vector.
	/// </summary>
	CollectionChange CollectionChange { get; }

	/// <summary>
	/// Gets the position where the change occurred in the vector.
	/// </summary>
	uint Index { get; }
}
