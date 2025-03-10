using System.Collections.Generic;

namespace Windows.UI.Xaml.Data;

/// <summary>
/// Manages whether items and ranges of items in the data source are selected in the list control.
/// </summary>
public partial interface ISelectionInfo
{
	/// <summary>
	/// Marks the items in the data source specified by *itemIndexRange* as selected in the list control.
	/// </summary>
	/// <param name="itemIndexRange">A range of items in the data source.</param>
	void SelectRange(ItemIndexRange itemIndexRange);

	/// <summary>
	/// Marks the items in the data source specified by *itemIndexRange* as not selected in the list control.
	/// </summary>
	/// <param name="itemIndexRange">A range of items in the data source.</param>
	void DeselectRange(ItemIndexRange itemIndexRange);

	/// <summary>
	/// Provides info about whether the item in the data source at the specified *index* is selected in the list control.
	/// </summary>
	/// <param name="index">The index of an item in the data source.</param>
	/// <returns>**true** if the item in the data source at the specified *index* is selected in the list control; otherwise, **false**.</returns>
	bool IsSelected(int index);

	/// <summary>
	/// Returns the collection of ranges of items in the data source that are selected in the list control.
	/// </summary>
	/// <returns>A collection of ranges of items in the data source that are selected in the list control.</returns>
	IReadOnlyList<ItemIndexRange> GetSelectedRanges();
}
