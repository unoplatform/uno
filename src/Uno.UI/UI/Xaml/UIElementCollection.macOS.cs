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
	public partial class UIElementCollection : BatchCollection<UIElement>, IList<UIElement>, IEnumerable<UIElement>
	{
		private readonly BindableNSView _owner;

		public UIElementCollection(BindableNSView owner) : base(owner)
		{
			_owner = owner;
		}

		protected override int IndexOfCore(UIElement item)
		{
			return _owner.ChildrenShadow.IndexOf(item);
		}

		protected override void InsertCore(int index, UIElement item)
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

		protected override UIElement RemoveAtCore(int index)
		{
			var view = _owner.ChildrenShadow[index];

			view.RemoveFromSuperview();

			return view as UIElement;
		}

		protected override UIElement GetAtIndexCore(int index)
		{
			return _owner.ChildrenShadow[index] as UIElement;
		}

		protected override UIElement SetAtIndexCore(int index, UIElement value)
		{
			var view = _owner.ChildrenShadow[index];

			// Set the view directly in the original array
			_owner.Subviews[index] = value;

			return view as UIElement;
		}

		protected override void AddCore(UIElement item)
		{
			_owner.AddSubview(item);
		}

		protected override IEnumerable<UIElement> ClearCore()
		{
			var views = _owner.ChildrenShadow.ToList();
			views.ForEach(v => v.RemoveFromSuperview());

			return views.Cast<UIElement>(); // IFE TODO
		}

		protected override bool ContainsCore(UIElement item)
		{
			return _owner.ChildrenShadow.Contains(item);
		}

		protected override void CopyToCore(UIElement[] array, int arrayIndex)
		{
			_owner.ChildrenShadow.ToArray().CopyTo(array, arrayIndex);
		}

		protected override bool RemoveCore(UIElement item)
		{
			item.RemoveFromSuperview();

			return true;
		}

		protected override int CountCore()
		{
			return _owner.ChildrenShadow.Count;
		}

		protected override void MoveCore(uint oldIndex, uint newIndex)
		{
			_owner.MoveViewTo((int)oldIndex, (int)newIndex);
		}
	}
}
