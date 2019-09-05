using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;
using View = Windows.UI.Xaml.UIElement;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection : BatchCollection<UIElement>
	{
		private readonly List<UIElement> _elements;
		private readonly FrameworkElement _view;

		public UIElementCollection(FrameworkElement view) : base(view)
		{
			_elements = view._children;
			this._view = view;
		}

		protected override void AddCore(View item) => _view.AddChild(item);

		protected override IEnumerable<View> ClearCore()
		{
			var old = _elements.ToArray();
			_elements.Clear();

			return old;
		}

		protected override bool ContainsCore(View item)
		{
			throw new NotImplementedException();
		}

		protected override void CopyToCore(View[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		protected override int CountCore() => _elements.Count;

		protected override View GetAtIndexCore(int index) => _elements[index];

		protected override List<View>.Enumerator GetEnumeratorCore() => _elements.GetEnumerator();

		protected override int IndexOfCore(View item) => _elements.IndexOf(item);

		protected override void InsertCore(int index, View item) => _view.AddChild(item, index);

		protected override void MoveCore(uint oldIndex, uint newIndex)
		{
			throw new NotImplementedException();
		}

		protected override View RemoveAtCore(int index)
		{
			var item = _elements.ElementAtOrDefault(index);
			if (item != null)
			{
				_view.RemoveChild(item);
			}
			return item;
		}

		protected override bool RemoveCore(View item) => _view.RemoveChild(item) != null;

		protected override View SetAtIndexCore(int index, View value) => _elements[index] = value;
	}
}
