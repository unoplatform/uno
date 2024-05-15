using System.Collections;
using System.Collections.Specialized;
using Uno.Extensions;

namespace Windows.Foundation.Collections;

internal abstract class ObservableVectorWrapper
{
	public event VectorChangedEventHandler<object> VectorChanged;

	public ObservableVectorWrapper(object source)
	{
		if (source is INotifyCollectionChanged observable)
		{
			observable.CollectionChanged += OnSourceCollectionChanged;
		}
	}

	private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		VectorChanged?.Invoke(this as IObservableVector<object>, e.ToVectorChangedEventArgs());
	}

	public static IObservableVector<object> Create(object source)
	{
		if (source is IList list)
		{
			return new ObservableVectorListWrapper(list);
		}

		if (source is IEnumerable enumerable)
		{
			return new ObservableVectorEnumerableWrapper(enumerable);
		}

		return new ObservableVectorEnumerableWrapper(Enumerable.Empty<object>());
	}
}
