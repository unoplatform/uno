using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Android.Views;
using System;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Uno.UI.Controls;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection : IList<UIElement>, IEnumerable<UIElement>
	{
		private readonly BindableView _owner;

		public UIElementCollection(BindableView owner)
		{
			_owner = owner;
		}

		private int IndexOfCore(UIElement item)
		{
			return _owner.GetChildren().IndexOf(item);
		}

		private void InsertCore(int index, UIElement item)
		{
			_owner.AddView(item, index);
		}

		private UIElement RemoveAtCore(int index)
		{
			var view = _owner.GetChildAt(index);
			_owner.RemoveViewAt(index);

			return view as UIElement;
		}

		private UIElement GetAtIndexCore(int index)
		{
			return _owner.GetChildAt(index) as UIElement;
		}

		private UIElement SetAtIndexCore(int index, UIElement value)
		{
			var view = _owner.GetChildAt(index);

			_owner.RemoveViewAt(index);
			_owner.AddView(value, index);

			return view as UIElement;
		}

		private void AddCore(UIElement item)
		{
			_owner.AddView(item);
		}

		private IEnumerable<View> ClearCore()
		{
			var views = _owner.GetChildren().ToArray();
			_owner.RemoveAllViews();

			return views;
		}

		private bool ContainsCore(UIElement item)
		{
			return _owner.GetChildren().Contains(item);
		}

		private void CopyToCore(UIElement[] array, int arrayIndex)
		{
			_owner.GetChildren().ToArray().CopyTo(array, arrayIndex);
		}

		private bool RemoveCore(UIElement item)
		{
			if (item != null)
			{
				_owner.RemoveView(item);
				return true;
			}
			else
			{
				return false;
			}
		}

		private int CountCore()
		{
			return _owner.ChildCount;
		}

		private void MoveCore(uint oldIndex, uint newIndex)
		{
			_owner.MoveViewTo((int)oldIndex, (int)newIndex);
		}
	}
}
