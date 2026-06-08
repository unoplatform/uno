using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Represents a collection of weak references to UIElement objects.
	/// </summary>
	public partial class UIElementWeakCollection : IList<UIElement>, IEnumerable<UIElement>
	{
		private readonly List<ManagedWeakReference> _innerList = new List<ManagedWeakReference>();

		/// <summary>
		/// Initializes a new instance of the UIElementWeakCollection class.
		/// </summary>
		public UIElementWeakCollection()
		{
		}

		/// <inheritdoc />
		public uint Size => (uint)_innerList.Count;

		/// <inheritdoc />
		public int IndexOf(UIElement item)
		{
			for (int i = 0; i < _innerList.Count; i++)
			{
				var weakItem = _innerList[i];
				if (TryGetTarget(weakItem, out var target) && target == item)
				{
					return i;
				}
			}
			return -1;
		}

		/// <inheritdoc />
		public void Insert(int index, UIElement item) => _innerList.Insert(index, GetWeakReference(item));

		/// <inheritdoc />
		public void RemoveAt(int index) => _innerList.RemoveAt(index);

		/// <inheritdoc />
		public UIElement this[int index]
		{
			get => TryGetTarget(_innerList[index], out var target) ? target : null;
			set => _innerList[index] = GetWeakReference(value);
		}

		/// <inheritdoc />
		public void Add(UIElement item) => _innerList.Add(GetWeakReference(item));

		/// <inheritdoc />
		public void Clear() => _innerList.Clear();

		/// <inheritdoc />
		public bool Contains(UIElement item) => IndexOf(item) >= 0;

		/// <inheritdoc />
		public void CopyTo(UIElement[] array, int arrayIndex)
		{
			_innerList
				.Select(item => TryGetTarget(item, out var target) ? target : null)
				.ToList()
				.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc />
		public bool Remove(UIElement item)
		{
			var index = IndexOf(item);
			if (index >= 0)
			{
				_innerList.RemoveAt(index);
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public int Count
		{
			get => _innerList.Count;
			set => throw new InvalidOperationException("Cannot set count");
		}

		/// <inheritdoc />
		public bool IsReadOnly
		{
			get => false;
			set => throw new InvalidOperationException("Cannot make read only");
		}

		/// <inheritdoc />
		public IEnumerator<UIElement> GetEnumerator()
		{
			foreach (var item in _innerList)
			{
				if (TryGetTarget(item, out var target))
				{
					yield return target;
				}
			}
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private ManagedWeakReference GetWeakReference(UIElement uiElement) =>
			((IWeakReferenceProvider)uiElement).WeakReference;

		private bool TryGetTarget(ManagedWeakReference weakReference, out UIElement target)
		{
			return weakReference.TryGetTarget(out target);
		}
	}
}
