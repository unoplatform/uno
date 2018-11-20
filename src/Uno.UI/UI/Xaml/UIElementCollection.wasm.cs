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
		private readonly UIElement _view;

		public UIElementCollection(UIElement view) : base(view)
		{
			_view = view;
		}

		protected override void AddCore(View item)
		{
			_view.AddChild(item);
		}

		protected override IEnumerable<View> ClearCore()
		{
			var deleted = _view._children.ToArray();
			_view.ClearChildren();

			return deleted;
		}

		protected override bool ContainsCore(View item)
		{
			return _view._children.Contains(item);
		}

		protected override void CopyToCore(View[] array, int arrayIndex)
			=> _view._children.ToArray().CopyTo(array, arrayIndex);


		protected override int CountCore() => _view._children.Count;

		protected override View GetAtIndexCore(int index) => _view._children[index];

		protected override List<View>.Enumerator GetEnumeratorCore() => _view._children.GetEnumerator();

		protected override int IndexOfCore(View item) => _view._children.IndexOf(item);

		protected override void InsertCore(int index, View item)
		{
			_view.AddChild(item, index);
		}

		protected override void MoveCore(uint oldIndex, uint newIndex)
		{
			_view.MoveViewTo((int)oldIndex, (int)newIndex);
		}

		protected override View RemoveAtCore(int index)
		{
			var item = _view._children.ElementAtOrDefault(index);
			_view.RemoveChild(item);
			return item;
		}

		protected override bool RemoveCore(View item) => _view.RemoveChild(item);

		protected override View SetAtIndexCore(int index, View value) => throw new NotImplementedException();
	}
}
