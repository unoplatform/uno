using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Provides methods to let an item be inserted between other items in a drag-and-drop operation.
/// </summary>
public partial interface IInsertionPanel
{
	/// <summary>
	/// Returns the index values of the items that the specified point is between.
	/// </summary>
	/// <param name="position">The point for which to get insertion indexes.</param>
	/// <param name="first">The index of the item before the specified point.</param>
	/// <param name="second">The index of the item after the specified point.</param>
	void GetInsertionIndexes(Point position, out int first, out int second);
}
