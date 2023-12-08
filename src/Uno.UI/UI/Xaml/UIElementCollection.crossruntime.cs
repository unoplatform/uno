using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;
using View = Windows.UI.Xaml.UIElement;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection
	{
		private readonly UIElement _owner;

		internal UIElementCollection(UIElement view)
		{
			_owner = view;
		}

		private void AddCore(View item)
		{
			_owner.AddChild(item);
		}

		// UNO TODO: **IMPORTANT BEFORE MERGE** Should this call "Leave" on all children?
		private IEnumerable<View> ClearCore()
		{
			var deleted = _owner._children.ToArray();
			_owner.ClearChildren();

			return deleted;
		}

		private bool ContainsCore(View item)
		{
			return _owner._children.Contains(item);
		}

		private void CopyToCore(View[] array, int arrayIndex)
			=> _owner._children.ToArray().CopyTo(array, arrayIndex);


		private int CountCore() => _owner._children.Count;

		private View GetAtIndexCore(int index) => _owner._children[index];

		public List<View>.Enumerator GetEnumerator() => (List<View>.Enumerator)_owner._children.GetEnumerator();

		IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => GetEnumerator();

		private int IndexOfCore(View item) => _owner._children.IndexOf(item);

		// UNO TODO: **IMPORTANT BEFORE MERGE** Should this call "Enter"?
		private void InsertCore(int index, View item)
		{
			_owner.AddChild(item, index);
		}

		private void MoveCore(uint oldIndex, uint newIndex)
		{
			_owner.MoveChildTo((int)oldIndex, (int)newIndex);
		}

		// UNO TODO: **IMPORTANT BEFORE MERGE** Should this call "Leave"?
		private View RemoveAtCore(int index)
		{
			var item = _owner._children.ElementAtOrDefault(index);
			_owner.RemoveChild(item);
			return item;
		}

		// UNO TODO: **IMPORTANT BEFORE MERGE** Should this call "Leave"?
		private bool RemoveCore(View item)
			=> _owner.RemoveChild(item);

		// UNO TODO: **IMPORTANT BEFORE MERGE** Should this call "Leave" on the old child, and "Enter" on the new one?
		private View SetAtIndexCore(int index, View value)
			=> _owner.ReplaceChild(index, value);
	}
}
