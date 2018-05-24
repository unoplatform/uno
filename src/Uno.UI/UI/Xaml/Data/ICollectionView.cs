using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Data
{
	public partial interface ICollectionView : IEnumerable<object>, IObservableVector<object>, IList<object>
	{
		bool MoveCurrentTo(object item);
		bool MoveCurrentToPosition(int index);
		bool MoveCurrentToFirst();
		bool MoveCurrentToLast();
		bool MoveCurrentToNext();
		bool MoveCurrentToPrevious();

		IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count);

		IObservableVector<object> CollectionGroups { get; }

		object CurrentItem { get; }
		int CurrentPosition { get; }

		bool HasMoreItems { get; }

		bool IsCurrentAfterLast { get; }
		bool IsCurrentBeforeFirst { get; }

		event EventHandler<object> CurrentChanged;
		event CurrentChangingEventHandler CurrentChanging;
	}
}
