using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Android.Views;
using System;
using Uno.UI.Controls;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection : BatchCollection<UIElement>, IList<UIElement>, IEnumerable<UIElement>
	{
		private readonly BindableView _owner;

		public UIElementCollection(BindableView owner) : base(owner)
		{
			_owner = owner;
		}

		protected override int IndexOfCore(UIElement item)
		{
			return _owner.GetChildren().IndexOf(item);
		}

		protected override void InsertCore(int index, UIElement item)
		{
			_owner.AddView(item, index);
		}

		protected override UIElement RemoveAtCore(int index)
		{
			var view = _owner.GetChildAt(index);
			_owner.RemoveViewAt(index);

			return view as UIElement;
		}

		protected override UIElement GetAtIndexCore(int index)
		{
			return _owner.GetChildAt(index) as UIElement;
		}

		protected override UIElement SetAtIndexCore(int index, UIElement value)
		{
			var view = _owner.GetChildAt(index);

			_owner.RemoveViewAt(index);
			_owner.AddView(value, index);

			return view as UIElement;
		}

		protected override void AddCore(UIElement item)
		{
			_owner.AddView(item);
		}

		protected override IEnumerable<UIElement> ClearCore()
		{
			var views = _owner.GetChildren().ToArray();
			_owner.RemoveAllViews();

			return views.Cast<UIElement>(); // IFE TODO
		}

		protected override bool ContainsCore(UIElement item)
		{
			return _owner.GetChildren().Contains(item);
		}

		protected override void CopyToCore(UIElement[] array, int arrayIndex)
		{
			_owner.GetChildren().ToArray().CopyTo(array, arrayIndex);
		}

		protected override bool RemoveCore(UIElement item)
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

		protected override int CountCore()
		{
			return _owner.ChildCount;
		}

		protected override void MoveCore(uint oldIndex, uint newIndex)
		{
			_owner.MoveViewTo((int)oldIndex, (int)newIndex);
		}
	}


}
