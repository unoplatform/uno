using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection
	{
		private readonly UIElement _owner;

		internal UIElementCollection(UIElement view)
		{
			_owner = view;
		}

		protected void AddCore(UIElement item)
		{
			_owner.AddChild(item);
		}

		protected IEnumerable<UIElement> ClearCore()
		{
			var deleted = _owner._children.ToArray();
			_owner.ClearChildren();

			return deleted;
		}

		protected bool ContainsCore(UIElement item)
		{
			return _owner._children.Contains(item);
		}

		protected void CopyToCore(UIElement[] array, int arrayIndex)
			=> _owner._children.ToArray().CopyTo(array, arrayIndex);


		protected int CountCore() => _owner._children.Count;

		protected UIElement GetAtIndexCore(int index) => _owner._children[index];

		protected List<UIElement>.Enumerator GetEnumerator() => _owner._children.GetEnumerator();
		IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => GetEnumerator();

		protected int IndexOfCore(UIElement item) => _owner._children.IndexOf(item);

		protected void InsertCore(int index, UIElement item)
		{
			_owner.AddChild(item, index);
		}

		protected void MoveCore(uint oldIndex, uint newIndex)
		{
			// _owner.MoveChildTo((int)oldIndex, (int)newIndex);
		}

		protected UIElement RemoveAtCore(int index)
		{
			var item = _owner._children.ElementAtOrDefault(index);
			_owner.RemoveChild(item);
			return item;
		}

		protected bool RemoveCore(UIElement item) => _owner.RemoveChild(item) != null;

		protected UIElement SetAtIndexCore(int index, UIElement value) => throw new NotImplementedException();
	}
}
