using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Interop;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// This implementation combines ItemsSourceView with InspectingDataSource to match behavior
	/// </summary>
	public class ItemsSourceView : INotifyCollectionChanged
	{
		private int m_cachedSize = -1;
		private IList m_vector = null;
		private IKeyIndexMapping m_uniqueIdMaping;

		public ItemsSourceView(object source)
		{
			if (source == null)
			{
				throw new ArgumentNullException("Argument 'source' is null.");
			}

			var vector = source as IList<object>;
			if (vector != null)
			{
				m_vector = vector;
				ListenToCollectionChanges();
			}
			else
			{
				// The bindable interop interface are abi compatible with the corresponding
				// WinRT interfaces.
				var bindableVector = source as IList;
				if (bindableVector != null)
				{
					m_vector.set(reinterpret_cast <const IVector<object>&> (bindableVector));
					ListenToCollectionChanges();
				}
				else
				{
					var iterable = source.try_as<IIterable<object>>();
					if (iterable)
					{
						m_vector.set(WrapIterable(iterable));
					}
					else
					{
						var bindableIterable = source.try_as<IBindableIterable>();
						if (bindableIterable)
						{
							m_vector.set(WrapIterable(reinterpret_cast <const IIterable<object> &> (bindableIterable)));
						}
						else
						{
							throw hresult_invalid_argument(L"Argument 'source' is not a supported vector.");
						}
					}
				}
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


		~InspectingDataSource()
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

		//IVector<object> WrapIterable(const IIterable<object>& iterable)
		//{
		//	var vector = make < Vector < object, MakeVectorParam< VectorFlag.DependencyObjectBase > () >> ();
		//	var iterator = iterable.First();
		//	while (iterator.HasCurrent())
		//	{
		//		vector.Append(iterator.Current());
		//		iterator.MoveNext();
		//	}

		//	return vector;
		//}

		//private void UnListenToCollectionChanges()
		//{
		//	var notifyCollection = m_notifyCollectionChanged.safe_get()
		//			if ()
		//	{
		//		notifyCollection.CollectionChanged(m_eventToken);
		//	}

		//	else if (var bindableObservableCollection = m_bindableObservableVector.safe_get())
		//					{
		//		bindableObservableCollection.VectorChanged(m_eventToken);
		//	}

		//					else if (var observableCollection = m_observableVector.safe_get())
		//					{
		//		observableCollection.VectorChanged(m_eventToken);
		//	}
		//}

		void ListenToCollectionChanges()
		{
			if (m_vector == null)
			{
				throw new InvalidOperationException("Backing vector not set");
			}
			var incc = m_vector as INotifyCollectionChanged;
			if (incc != null)
			{
				m_eventToken = incc.CollectionChanged({ this, &OnCollectionChanged });
				m_notifyCollectionChanged = incc;
			}
			else
			{
				var bindableObservableVector = m_vector.try_as<IBindableObservableVector>();
				if (bindableObservableVector)
				{
					m_eventToken = bindableObservableVector.VectorChanged({ this, &OnBindableVectorChanged });
					m_bindableObservableVector.set(bindableObservableVector);
				}
				else
				{
					var observableVector = m_vector.try_as<IObservableVector<object>>();
					if (observableVector)
					{
						m_eventToken = observableVector.VectorChanged({ this, &OnVectorChanged });
						m_observableVector.set(observableVector);
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
					newItems.Append(null);
					break;
				case CollectionChange.ItemRemoved:
					action = NotifyCollectionChangedAction.Remove;
					oldStartingIndex = (int)e.Index;
					oldItems.Append(null);
					break;
				case CollectionChange.ItemChanged:
					action = NotifyCollectionChangedAction.Replace;
					oldStartingIndex = (int)e.Index;
					newStartingIndex = oldStartingIndex;
					newItems.Append(null);
					oldItems.Append(null);
					break;
				case CollectionChange.Reset:
					action = NotifyCollectionChangedAction.Reset;
					break;
				default:
					throw new InvalidOperationException("Unsupported collection change");
			}

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
