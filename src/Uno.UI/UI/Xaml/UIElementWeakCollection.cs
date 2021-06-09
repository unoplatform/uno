using System;
using System.Collections.Generic;

namespace Windows.UI.Xaml
{
	public partial class UIElementWeakCollection : IList<UIElement>, IEnumerable<UIElement>
	{
		private readonly List<WeakReference<UIElement>> _innerList = new List<WeakReference<UIElement>>();

		public UIElementWeakCollection()
		{
		}

		public uint Size => (uint)_innerList.Count;

		public int IndexOf(UIElement item)
		{
			for (int i = 0; i < _innerList.Count; i++)
			{
				var weakItem = _innerList[i];
				if (weakItem.TryGetTarget(out var target) && target == item)
				{
					return i;
				}
			}
			return -1;
		}
		public void Insert(int index, UIElement item) => _innerList.Insert(index, new WeakReference<UIElement>(item));

		public void RemoveAt(int index) => _innerList.RemoveAt(index);

		public UIElement this[int index]
		{
			get => _innerList[index].TryGetTarget(out var target) ? target : null;
			set => _innerList[index] = new WeakReference<UIElement>(value);
		}
		public void Add(UIElement item) => _innerList.Add(new WeakReference<UIElement>(item));

		public void Clear() => _innerList.Clear();

		public bool Contains(UIElement item) => IndexOf(item) >= 0;

		public void CopyTo(global::Windows.UI.Xaml.UIElement[] array, int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}
		public bool Remove(global::Windows.UI.Xaml.UIElement item)
		{
			throw new global::System.NotSupportedException();
		}
		public int Count
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		public bool IsReadOnly
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		public global::System.Collections.Generic.IEnumerator<global::Windows.UI.Xaml.UIElement> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
	}
}
