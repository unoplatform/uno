using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class UIElementCollection : ICollection<UIElement>, IEnumerable<UIElement>, IList<UIElement>, INotifyCollectionChanged
	{
		/// <summary>
		/// The owner of this collection.
		/// This is intended to be used to redispatch to a specific instance in ** static ** CollectionChanged handlers.
		/// </summary>
		internal DependencyObject Owner => _owner;

		#region IList implementation

		public int IndexOf(UIElement item)
		{
			return IndexOfCore(item);
		}

		public void Insert(int index, UIElement item)
		{
			item.SetParent(_owner);

			InsertCore(index, item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
		}

		public void RemoveAt(int index)
		{
			var item = RemoveAtCore(index);
			ClearParent(item);

			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
		}

		public UIElement this[int index]
		{
			get { return GetAtIndexCore(index); }
			set
			{
				value.SetParent(_owner);

				var previousItem = SetAtIndexCore(index, value);
				ClearParent(previousItem);

				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, previousItem, index));
			}
		}

		#endregion

		#region ICollection implementation

		public void Add(UIElement item)
		{
#if !__CROSSRUNTIME__ // SetParent is already called in AddCore and calling it here messes up the check inside AddCore for a preexisting parent. VerifyNavigationViewItemToolTipPaneDisplayMode (in DEBUG) fails otherwise because Enter is called multiple times on the same element
			item.SetParent(_owner);
#endif

			AddCore(item);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
		}

		public void Clear()
		{
			var items = ClearCore();

			var hasItems = false;
			foreach (var item in items)
			{
				hasItems = true;

				if (item is DependencyObject provider)
				{
					ClearParent(item);
				}
			}

			if (!hasItems)
			{
				return;
			}

			if (_owner is FrameworkElement fe)
			{
				fe.InvalidateMeasure();
			}

			if (!hasItems)
			{
				return;
			}

			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items.ToList()));
		}

		// A child removed while a Hide animation plays in its subtree stays parented (in the owner's unloading
		// storage) until the animation completes.
		private void ClearParent(UIElement item)
		{
			if (!_owner.IsUnloadingElement(item))
			{
				item.SetParent(null);
			}
		}

		public bool Contains(UIElement item)
		{
			return ContainsCore(item);
		}

		public void CopyTo(UIElement[] array, int arrayIndex)
		{
			CopyToCore(array, arrayIndex);
		}

		public bool Remove(UIElement item)
		{
			if (item != null)
			{
				var removed = RemoveCore(item);
				ClearParent(item);

				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
				return removed;
			}
			else
			{
				return false;
			}
		}

		public int Count
		{
			get
			{
				return CountCore();
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion

		/// <summary>
		/// Moves the item at the specified index to a new location in the collection.
		/// </summary>
		/// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
		/// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
		public void Move(uint oldIndex, uint newIndex)
		{
			if (oldIndex >= Count)
			{
				throw new ArgumentOutOfRangeException(nameof(oldIndex));
			}

			if (newIndex >= Count)
			{
				throw new ArgumentOutOfRangeException(nameof(newIndex));
			}

			if (oldIndex == newIndex)
			{
				return;
			}

			MoveCore(oldIndex, newIndex);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, this[(int)newIndex], (int)newIndex, (int)oldIndex));
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);
		}
	}
}

