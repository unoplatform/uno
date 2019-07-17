#if XAMARIN_ANDROID
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
	public class UIElementCollection : BatchCollection<View>, IList<View>, IEnumerable<View>
    {
        private readonly BindableView _owner;

        public UIElementCollection(BindableView owner) : base(owner)
        {
            _owner = owner;
        }

		protected override int IndexOfCore(View item)
		{
			return _owner.GetChildren().IndexOf(item);
		}

		protected override void InsertCore(int index, View item)
		{
			_owner.AddView(item, index);
		}

		protected override View RemoveAtCore(int index)
		{
			var view = _owner.GetChildAt(index);
			_owner.RemoveViewAt(index);

			return view;
		}

		protected override View GetAtIndexCore(int index)
		{
			return _owner.GetChildAt(index);
		}

		protected override View SetAtIndexCore(int index, View value)
		{
			var view = _owner.GetChildAt(index);

			_owner.RemoveViewAt(index);
			_owner.AddView(value, index);

			return view;
		}

		protected override void AddCore(View item)
		{
			_owner.AddView(item);
		}

		protected override IEnumerable<View> ClearCore()
		{
			var views = _owner.GetChildren().ToArray();
			_owner.RemoveAllViews();

			return views;
		}

		protected override bool ContainsCore(View item)
		{
			return _owner.GetChildren().Contains(item);
		}

		protected override void CopyToCore(View[] array, int arrayIndex)
		{
			_owner.GetChildren().ToArray().CopyTo(array, arrayIndex);
		}

		protected override bool RemoveCore(View item)
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

		protected override IEnumerator<View> GetEnumeratorCore()
			=> _owner.GetChildrenEnumerator();

		// This method is a explicit replace of GetEnumerator in BatchCollection<T> to
		// enable allocation-less enumeration. It is present at this level to avoid
		// a binary breaking change.
		public new IEnumerator<View> GetEnumerator() => GetEnumeratorCore();
	}
}
#endif
