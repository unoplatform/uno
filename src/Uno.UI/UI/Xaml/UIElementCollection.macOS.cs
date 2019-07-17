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
	public partial class UIElementCollection :  BatchCollection<NSView>, IList<NSView>, IEnumerable<NSView>
	{
        private readonly BindableNSView _owner;

        public UIElementCollection(BindableNSView owner) : base(owner)
		{
			_owner = owner;
		}

		protected override int IndexOfCore(NSView item)
		{
			return _owner.ChildrenShadow.IndexOf(item);
		}

		protected override void InsertCore(int index, NSView item)
		{
			if (_owner.Subviews.Length != 0)
			{
				_owner.AddSubview(item, NSWindowOrderingMode.Above, _owner.Subviews[index]);
			}
			else
			{
				_owner.AddSubview(item);
			}
		}

		protected override NSView RemoveAtCore(int index)
		{
			var view = _owner.ChildrenShadow[index];

			view.RemoveFromSuperview();

			return view;
		}

		protected override NSView GetAtIndexCore(int index)
		{
			return _owner.ChildrenShadow[index];
		}

		protected override NSView SetAtIndexCore(int index, NSView value)
		{
			var view = _owner.ChildrenShadow[index];

			// Set the view directly in the original array
			_owner.Subviews[index] = value;

			return view;
		}

		protected override void AddCore(NSView item)
		{
			_owner.AddSubview(item);
		}

		protected override IEnumerable<NSView> ClearCore()
		{
			var views = _owner.ChildrenShadow.ToList();
			views.ForEach(v => v.RemoveFromSuperview());

			return views;
		}

		protected override bool ContainsCore(NSView item)
		{
			return _owner.ChildrenShadow.Contains(item);
		}

		protected override void CopyToCore(NSView[] array, int arrayIndex)
		{
			_owner.ChildrenShadow.ToArray().CopyTo(array, arrayIndex);
		}

		protected override bool RemoveCore(NSView item)
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

		protected override List<NSView>.Enumerator GetEnumeratorCore()
			=> _owner.GetChildrenEnumerator();

		// This method is a explicit replace of GetEnumerator in BatchCollection<T> to
		// enable allocation-less enumeration. It is present at this level to avoid
		// a binary breaking change.
		public new List<NSView>.Enumerator GetEnumerator() => GetEnumeratorCore();
	}
}
