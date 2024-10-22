using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Enables collections to support current record management, grouping, and incremental loading (data virtualization).
/// </summary>
/// <remarks>
/// To implement custom behavior for selection currency in your data source,
/// your data source should implement ICollectionViewFactory instead of implementing ICollectionView directly.
/// </remarks>
public partial interface ICollectionView : IEnumerable<object>, IObservableVector<object>, IList<object>
{
	/// <summary>
	/// Returns any collection groups that are associated with the view.
	/// </summary>
	IObservableVector<object> CollectionGroups { get; }

	/// <summary>
	/// Gets the current item in the view.
	/// </summary>
	object CurrentItem { get; }

	/// <summary>
	/// Gets the ordinal position of the CurrentItem within the view.
	/// </summary>
	int CurrentPosition { get; }

	/// <summary>
	/// Gets a sentinel value that supports incremental loading implementations. See also LoadMoreItemsAsync.
	/// </summary>
	bool HasMoreItems { get; }

	/// <summary>
	/// Gets a value that indicates whether the CurrentItem of the view is beyond the end of the collection.
	/// </summary>
	bool IsCurrentAfterLast { get; }

	/// <summary>
	/// Gets a value that indicates whether the CurrentItem of the view is beyond the beginning of the collection.
	/// </summary>
	bool IsCurrentBeforeFirst { get; }

	/// <summary>
	/// Initializes incremental loading from the view.
	/// </summary>
	/// <param name="count">The number of items to load.</param>
	/// <returns>The wrapped results of the load operation.</returns>
	IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count);

	/// <summary>
	/// Sets the specified item to be the CurrentItem in the view.
	/// </summary>
	/// <param name="item">The item to set as the CurrentItem.</param>
	/// <returns>true if the resulting CurrentItem is within the view; otherwise, false.</returns>
	bool MoveCurrentTo(object item);

	/// <summary>
	/// Sets the first item in the view as the CurrentItem.
	/// </summary>
	/// <returns>true if the resulting CurrentItem is an item within the view; otherwise, false.</returns>
	bool MoveCurrentToFirst();

	/// <summary>
	/// Sets the last item in the view as the CurrentItem.
	/// </summary>
	/// <returns>true if the resulting CurrentItem is an item within the view; otherwise, false.</returns>
	bool MoveCurrentToLast();

	/// <summary>
	/// Sets the item after the CurrentItem in the view as the CurrentItem.
	/// </summary>
	/// <returns>true if the resulting CurrentItem is an item within the view; otherwise, false.</returns>
	bool MoveCurrentToNext();

	/// <summary>
	/// Sets the item at the specified index to be the CurrentItem in the view.
	/// </summary>
	/// <param name="index">The index of the item to move to.</param>
	/// <returns>true if the resulting CurrentItem is an item within the view; otherwise, false.</returns>
	bool MoveCurrentToPosition(int index);

	/// <summary>
	/// Sets the item before the CurrentItem in the view as the CurrentItem.
	/// </summary>
	/// <returns>true if the resulting CurrentItem is an item within the view; otherwise, false.</returns>
	bool MoveCurrentToPrevious();

	/// <summary>
	/// When implementing this interface, fire this event after the current item has been changed.
	/// </summary>
	event EventHandler<object> CurrentChanged;

	/// <summary>
	/// When implementing this interface, fire this event before changing the current item. The event handler can cancel this event.
	/// </summary>
	event CurrentChangingEventHandler CurrentChanging;
}
