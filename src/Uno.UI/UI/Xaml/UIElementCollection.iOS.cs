#if XAMARIN_IOS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;
using Foundation;
using UIKit;
using Uno.UI.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class UIElementCollection :  BatchCollection<UIView>, IList<UIView>, IEnumerable<UIView>
	{
        private readonly BindableUIView _owner;

        public UIElementCollection(BindableUIView owner) : base(owner)
		{
			_owner = owner;
		}

		protected override int IndexOfCore(UIView item)
		{
			return _owner.ChildrenShadow.IndexOf(item);
		}

		protected override void InsertCore(int index, UIView item)
		{
			_owner.InsertSubview(item, index);
		}

		protected override UIView RemoveAtCore(int index)
		{
			var view = _owner.ChildrenShadow[index];

			view.RemoveFromSuperview();

			return view;
		}

		protected override UIView GetAtIndexCore(int index)
		{
			return _owner.ChildrenShadow[index];
		}

		protected override UIView SetAtIndexCore(int index, UIView value)
		{
			var view = _owner.ChildrenShadow[index];

			// Set the view directly in the original array
			_owner.Subviews[index] = value;

			return view;
		}

		protected override void AddCore(UIView item)
		{
			_owner.AddSubview(item);
		}

		protected override IEnumerable<UIView> ClearCore()
		{
			var views = _owner.ChildrenShadow.ToList();
			views.ForEach(v => v.RemoveFromSuperview());

			return views;
		}

		protected override bool ContainsCore(UIView item)
		{
			return _owner.ChildrenShadow.Contains(item);
		}

		protected override void CopyToCore(UIView[] array, int arrayIndex)
		{
			_owner.ChildrenShadow.ToArray().CopyTo(array, arrayIndex);
		}

		protected override bool RemoveCore(UIView item)
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

		protected override IEnumerator<UIView> GetEnumeratorCore()
			=> _owner.GetChildrenEnumerator();

		// This method is a explicit replace of GetEnumerator in BatchCollection<T> to
		// enable allocation-less enumeration. It is present at this level to avoid
		// a binary breaking change.
		public new IEnumerator<UIView> GetEnumerator() => GetEnumeratorCore();
	}
}
#endif
