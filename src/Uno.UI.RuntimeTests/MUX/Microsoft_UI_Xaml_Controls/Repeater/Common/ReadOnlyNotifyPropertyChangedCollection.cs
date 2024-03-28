using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common
{
	public class ReadOnlyNotifyPropertyChangedCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged, IKeyIndexMapping
	{
		public ReadOnlyNotifyPropertyChangedCollection() { }

		public ReadOnlyNotifyPropertyChangedCollection(IEnumerable<T> data)
		{
			Data = new ObservableCollection<T>(data);
		}

		#region IReadOnlyList<T>

		public int Count => Data.Count;

		public T this[int index] => Data[index];

		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException("This is not implemented and should not have be used");
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException("This is not implemented and should not have be used");
		}

		#endregion

		#region INotifyCollectionChanged

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		protected virtual void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			this.CollectionChanged?.Invoke(this, e);
		}

		#endregion

		public string KeyFromIndex(int index)
		{
			return this[index].GetHashCode().ToString();
		}

		public int IndexFromKey(string key)
		{
			throw new Exception();
		}

		public ObservableCollection<T> Data
		{
			get
			{
				if (_data == null)
				{
					_data = new ObservableCollection<T>();
				}
				return _data;
			}

			set
			{
				if (_data != value)
				{
					// Listen for future changes
					if (_data != null)
						_data.CollectionChanged -= this.OnCollectionChanged;

					_data = value;

					if (_data != null)
						_data.CollectionChanged += this.OnCollectionChanged;
				}

				// Raise a reset event
				this.OnCollectionChanged(
					this,
					new NotifyCollectionChangedEventArgs(
						NotifyCollectionChangedAction.Reset));
			}
		}

		private ObservableCollection<T> _data;
	}
}
