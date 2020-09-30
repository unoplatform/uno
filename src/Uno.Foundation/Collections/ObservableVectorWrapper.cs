using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.Foundation.Collections;

namespace Windows.Foundation.Collections
{
#if !NET6_0_OR_GREATER // moved to linker file
#if __IOS__
	[global::Foundation.Preserve(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.Preserve(AllMembers = true)]
#endif
#endif
	internal class ObservableVectorWrapper
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
}
