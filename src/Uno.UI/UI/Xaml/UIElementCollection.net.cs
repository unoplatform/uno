using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;
using View = Windows.UI.Xaml.UIElement;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection
	{
		private readonly List<UIElement> _elements;
		private readonly FrameworkElement _owner;

		private readonly UIElement _owner;

		public UIElementCollection(FrameworkElement view)
		{
			_elements = view._children;
			_owner = view;
		}

		protected override void AddCore(View item) => _owner.AddChild(item);

		private IEnumerable<View> ClearCore()
		{
			var old = _elements.ToArray();
			_elements.Clear();

			return old;
		}

		private bool ContainsCore(View item)
		{
			throw new NotImplementedException();
		}

		private void CopyToCore(View[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		private int CountCore() => _elements.Count;

		private View GetAtIndexCore(int index) => _elements[index];

		public IEnumerator<View> GetEnumerator() => _elements.GetEnumerator();

		private int IndexOfCore(View item) => _elements.IndexOf(item);

		protected override void InsertCore(int index, View item) => _owner.AddChild(item, index);

		private void MoveCore(uint oldIndex, uint newIndex)
		{
			throw new NotImplementedException();
		}

		private View RemoveAtCore(int index)
		{
			var item = _elements.ElementAtOrDefault(index);
			if (item != null)
			{
				_view.RemoveChild(item);
			}
			return item;
		}

		protected override bool RemoveCore(View item) => _owner.RemoveChild(item) != null;

		private View SetAtIndexCore(int index, View value) => _elements[index] = value;
	}
}
