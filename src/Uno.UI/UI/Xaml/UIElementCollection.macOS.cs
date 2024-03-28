using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;
using Foundation;
using Uno.UI.Controls;
using AppKit;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection : IList<UIElement>, IEnumerable<UIElement>
	{
		private readonly BindableNSView _owner;

		public UIElementCollection(BindableNSView owner)
		{
			_owner = owner;
		}

		private int IndexOfCore(UIElement item)
		{
			return _owner.ChildrenShadow.IndexOf(item);
		}

		private void InsertCore(int index, UIElement item)
		{
			if (index == _owner.Subviews.Length)
			{
				_owner.AddSubview(item);
			}
			else
			{
				_owner.AddSubview(item, NSWindowOrderingMode.Above, _owner.Subviews[index]);
			}
		}

		private UIElement RemoveAtCore(int index)
		{
			var view = _owner.ChildrenShadow[index];

			view.RemoveFromSuperview();

			return view as UIElement;
		}

		private UIElement GetAtIndexCore(int index)
		{
			return _owner.ChildrenShadow[index] as UIElement;
		}

		private UIElement SetAtIndexCore(int index, UIElement value)
		{
			var view = _owner.ChildrenShadow[index];

			// Set the view directly in the original array
			_owner.Subviews[index] = value;

			return view as UIElement;
		}

		private void AddCore(UIElement item)
		{
			_owner.AddSubview(item);
		}

		private IEnumerable<NSView> ClearCore()
		{
			var views = _owner.ChildrenShadow.ToList();
			views.ForEach(v => v.RemoveFromSuperview());

			return views;
		}

		private bool ContainsCore(UIElement item)
		{
			return _owner.ChildrenShadow.Contains(item);
		}

		private void CopyToCore(UIElement[] array, int arrayIndex)
		{
			_owner.ChildrenShadow.ToArray().CopyTo(array, arrayIndex);
		}

		private bool RemoveCore(UIElement item)
		{
			item.RemoveFromSuperview();

			return true;
		}

		private int CountCore()
		{
			return _owner.ChildrenShadow.Count;
		}

		private void MoveCore(uint oldIndex, uint newIndex)
		{
			_owner.MoveViewTo((int)oldIndex, (int)newIndex);
		}
	}
}
