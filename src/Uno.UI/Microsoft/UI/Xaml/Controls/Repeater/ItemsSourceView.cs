// MUX Reference InspectingDataSource.cpp, commit 37ade09

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Interop;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a standardized view of the supported interactions between a given ItemsSource object and an ItemsRepeater control.
	/// </summary>
	/// <remarks>
	/// This implementation combines ItemsSourceView with InspectingDataSource to match behavior.	
	/// </remarks>
	public class ItemsSourceView : INotifyCollectionChanged
	{
		private int m_cachedSize = -1;

		private readonly IList m_vector;

		private readonly IKeyIndexMapping m_uniqueIdMaping;
		private INotifyCollectionChanged m_notifyCollectionChanged;
		private IBindableObservableVector m_bindableObservableVector;
		private IObservableVector<object> m_observableVector;

		public ItemsSourceView(object source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("Argument 'source' is null.");
			}

			var list = source as IList;
			if (list != null)
			{
				m_vector = list;
				ListenToCollectionChanges();
			}
			else
			{
				var enumerable = source as IEnumerable;
				if (enumerable != null)
				{
					m_vector = WrapIterable(enumerable);
				}
				throw new ArgumentException("Argument 'source' is not a supported vector.");
			}

			m_uniqueIdMaping = source as IKeyIndexMapping;
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		//#pragma region IDataSource

		public int Count
		{
			get
			{
				if (m_cachedSize == -1)
				{
					// Call the override the very first time. After this,
					// we can just update the size when there is a data source change.
					m_cachedSize = GetSizeCore();
				}

				return m_cachedSize;
			}
		}

		public object GetAt(int index)
		{
			return GetAtCore(index);
		}

		public bool HasKeyIndexMapping
		{
			get
			{
				return HasKeyIndexMappingCore();
			}
		}

		public string KeyFromIndex(int index)
		{
			return KeyFromIndexCore(index);
		}

		public int IndexFromKey(string id)
		{
			return IndexFromKeyCore(id);
		}

		internal void OnItemsSourceChanged(NotifyCollectionChangedEventArgs args)
		{
			m_cachedSize = GetSizeCore();
			CollectionChanged?.Invoke(this, args);
		}


		~ItemsSourceView()
		{
			UnListenToCollectionChanges();
		}

		private int GetSizeCore()
		{
			return m_vector.Count;
		}

		private object GetAtCore(int index)
		{
			return m_vector[index];
		}

		bool HasKeyIndexMappingCore()
		{
			return m_uniqueIdMaping != null;
		}

		string KeyFromIndexCore(int index)
		{
			if (m_uniqueIdMaping != null)
			{
				return m_uniqueIdMaping.KeyFromIndex(index);
			}
			else
			{
				throw new InvalidOperationException("ID mapping not set.");
			}
		}

		int IndexFromKeyCore(string id)
		{
			if (m_uniqueIdMaping != null)
			{
				return m_uniqueIdMaping.IndexFromKey(id);
			}
			else
			{
				throw new InvalidOperationException("ID mapping not set.");
			}
		}

		//#pragma endregion

		private int IndexOf(object value)
		{
			int index = -1;
			if (m_vector != null && value != null)
			{
				var v = -1;
				v = m_vector.IndexOf(value);
				if (v > -1)
				{
					index = v;
				}
			}
			return index;
		}

		private IList WrapIterable(IEnumerable enumerable)
		{
			var vector = new List<object>();
			foreach (var obj in enumerable)
			{
				vector.Add(obj);
			}
			return vector;
		}

		private void UnListenToCollectionChanges()
		{
			if (m_notifyCollectionChanged != null)
			{
				m_notifyCollectionChanged.CollectionChanged -= OnCollectionChanged;
			}
			else if (m_bindableObservableVector != null)
			{
				m_bindableObservableVector.VectorChanged -= OnBindableVectorChanged;
			}
			else if (m_observableVector != null)
			{
				m_observableVector.VectorChanged -= OnVectorChanged;
			}
		}

		void ListenToCollectionChanges()
		{
			if (m_vector == null)
			{
				throw new InvalidOperationException("Backing vector not set");
			}
			var incc = m_vector as INotifyCollectionChanged;
			if (incc != null)
			{
				incc.CollectionChanged += OnCollectionChanged;
				m_notifyCollectionChanged = incc;
			}
			else
			{
				var bindableObservableVector = m_vector as IBindableObservableVector;
				if (bindableObservableVector != null)
				{
					bindableObservableVector.VectorChanged += OnBindableVectorChanged;
					m_bindableObservableVector = bindableObservableVector;
				}
				else
				{
					var observableVector = m_vector as IObservableVector<object>;
					if (observableVector != null)
					{
						observableVector.VectorChanged += OnVectorChanged;
						m_observableVector = observableVector;
					}
				}
			}
		}

		private void OnCollectionChanged(
					object sender,
					NotifyCollectionChangedEventArgs e)
		{
			OnItemsSourceChanged(e);
		}

		void OnBindableVectorChanged(IBindableObservableVector sender, object e)
		{
			OnVectorChanged(
				sender as IObservableVector<object>,
				e as IVectorChangedEventArgs);
		}

		void OnVectorChanged(
			IObservableVector<object> sender,
			IVectorChangedEventArgs e)
		{
			// We need to build up NotifyCollectionChangedEventArgs here to raise the event.
			// There is opportunity to make this faster by caching the args if it does 
			// show up as a perf issue.
			// Also note that we do not access the data - we just add nullptr. We just 
			// need the count.

			NotifyCollectionChangedAction action;
			int oldStartingIndex = -1;
			int newStartingIndex = -1;

			var oldItems = new List<object>();
			var newItems = new List<object>();

			switch (e.CollectionChange)
			{
				case CollectionChange.ItemInserted:
					action = NotifyCollectionChangedAction.Add;
					newStartingIndex = (int)e.Index;
					newItems.Add(null);
					OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(action, newItems, newStartingIndex));
					break;
				case CollectionChange.ItemRemoved:
					action = NotifyCollectionChangedAction.Remove;
					oldStartingIndex = (int)e.Index;
					oldItems.Add(null);
					OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(action, oldItems, oldStartingIndex));
					break;
				case CollectionChange.ItemChanged:
					action = NotifyCollectionChangedAction.Replace;
					oldStartingIndex = (int)e.Index;
					newStartingIndex = oldStartingIndex;
					newItems.Add(null);
					oldItems.Add(null);
					OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(action, newItems, oldItems, newStartingIndex));
					break;
				case CollectionChange.Reset:
					action = NotifyCollectionChangedAction.Reset;
					OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(action));
					break;
				default:
					throw new InvalidOperationException("Unsupported collection change");
			}

			// Uno specific: WinUI uses an internal overload of NotifyCollectionChangedEventArgs constructor with 5 args,
			// we use a specific overload for each case instead

			//OnItemsSourceChanged(
			//	new NotifyCollectionChangedEventArgs(
			//		action,
			//		newItems,
			//		oldItems,
			//		newStartingIndex,
			//		oldStartingIndex);
		}
	}
}
