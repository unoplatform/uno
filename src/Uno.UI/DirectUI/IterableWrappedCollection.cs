using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Disposables;
using Windows.Foundation.Collections;

namespace DirectUI
{
	internal class IterableWrappedCollection<T> : IVector<T>, IIterable<T>
	{
		protected IVector<T> m_tpWrappedCollection;

		public virtual void SetWrappedCollection(IVector<T> collection)
		{
			m_tpWrappedCollection = collection;
		}

		public T this[int index]
		{
			get => GetAt(index);
			set => SetAt(index, value);
		}

		public int Count => m_tpWrappedCollection.Count;
		public bool IsReadOnly => m_tpWrappedCollection.IsReadOnly;

		public void Add(T item) => m_tpWrappedCollection.Add(item);
		public void Clear() => m_tpWrappedCollection?.Clear();
		public bool Contains(T item) => m_tpWrappedCollection.Contains(item);
		public void CopyTo(T[] array, int arrayIndex) => m_tpWrappedCollection.CopyTo(array, arrayIndex);
		public IEnumerator<T> GetEnumerator() => m_tpWrappedCollection.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public IIterator<T> GetIterator() => new UnoEnumeratorToIteratorAdapter<T>(GetEnumerator());
		public int IndexOf(T item) => m_tpWrappedCollection.IndexOf(item);
		public void Insert(int index, T item) => m_tpWrappedCollection.Insert(index, item);
		public bool Remove(T item) => m_tpWrappedCollection.Remove(item);
		public void RemoveAt(int index) => m_tpWrappedCollection.RemoveAt(index);
		private T GetAt(int index) => m_tpWrappedCollection[index];
		private void SetAt(int index, T value) => m_tpWrappedCollection[index] = value;
	}

	internal class IterableWrappedObservableCollection<T> : IterableWrappedCollection<T>, IObservableVector<T>
	{
		public event VectorChangedEventHandler<T> VectorChanged;
		private SerialDisposable m_VectorChangedToken = new SerialDisposable();

		public override void SetWrappedCollection(IVector<T> collection)
		{
			UnsubscribeFromCurrentCollection();

			base.SetWrappedCollection(collection);

			if (collection is IObservableVector<T> spAsObservable)
			{
				spAsObservable.VectorChanged += OnVectorChanged;
				m_VectorChangedToken.Disposable = Disposable.Create(() => spAsObservable.VectorChanged -= OnVectorChanged);
			}
		}

		private void OnVectorChanged(IObservableVector<T> sender, IVectorChangedEventArgs @event)
		{
			VectorChanged?.Invoke(this, @event);
		}

		private void UnsubscribeFromCurrentCollection()
		{
			m_VectorChangedToken.Disposable = null;
		}
	}
}
