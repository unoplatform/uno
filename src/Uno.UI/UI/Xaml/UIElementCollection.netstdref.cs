using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;
using UIElement = Windows.UI.Xaml.UIElement;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection
	{

		private readonly List<UIElement> _elements;
		private readonly FrameworkElement _owner;

		internal UIElementCollection(FrameworkElement view)
		{
			_elements = view._children;
			_owner = view;
		}

		private void AddCore(UIElement item) => _owner.AddChild(item);

		private IEnumerable<UIElement> ClearCore()
		{
			var old = _elements.ToArray();
			_elements.Clear();

			return old;
		}

		private bool ContainsCore(UIElement item)
		{
			throw new NotImplementedException();
		}

		private void CopyToCore(UIElement[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		private int CountCore() => _elements.Count;

		private UIElement GetAtIndexCore(int index) => _elements[index];

		private IEnumerator<UIElement> GetEnumerator() => _elements.GetEnumerator();

		IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => _elements.GetEnumerator();

		private int IndexOfCore(UIElement item) => _elements.IndexOf(item);

		private void InsertCore(int index, UIElement item) => _owner.AddChild(item, index);

		private void MoveCore(uint oldIndex, uint newIndex)
		{
			throw new NotImplementedException();
		}

		private UIElement RemoveAtCore(int index)
		{
			var item = _elements.ElementAtOrDefault(index);
			if (item != null)
			{
				_owner.RemoveChild(item);
			}
			return item;
		}

		private bool RemoveCore(UIElement item) => _owner.RemoveChild(item) != null;

		private UIElement SetAtIndexCore(int index, UIElement value) => _elements[index] = value;
	}
}
