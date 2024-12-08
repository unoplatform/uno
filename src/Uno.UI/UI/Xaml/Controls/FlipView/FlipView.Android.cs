using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Uno.UI.Controls;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using AndroidX.ViewPager.Widget;
using System.Collections.Specialized;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipView : Selector
	{
		private NativePagedView PagedView { get { return InternalItemsPanelRoot as NativePagedView; } }

		partial void InitializePartial()
		{
			SetClipChildren(true);
		}

		private protected override void UpdateItems(NotifyCollectionChangedEventArgs args)
		{

			if (PagedView != null && PagedView.Adapter == null)
			{
				PagedView.Adapter = new FlipViewAdapter()
				{
					Owner = this
				};
				PagedView.AddOnPageChangeListener(new FlipViewPageChangeListener { Owner = this });
				//Set CurrentItem in case SelectedIndex has changed prior to FlipView becoming visible
				PagedView.CurrentItem = SelectedIndex;
			}

			PagedView?.Adapter.NotifyDataSetChanged();

			base.UpdateItems(args);

			RequestLayout();
		}

		private void OnSelectedIndexChangedPartial(int oldValue, int newValue, bool animateChange)
		{
			if (PagedView == null || PagedView.CurrentItem == newValue)
			{
				return;
			}

			PagedView.SetCurrentItem(newValue, smoothScroll: animateChange);
		}

		private class FlipViewPageChangeListener : ViewPager.SimpleOnPageChangeListener
		{
			private WeakReference<FlipView> _ownerReference;
			/// <summary>
			/// The FlipView which uses this listener. This property is backed by a weak reference.
			/// </summary>
			internal FlipView Owner
			{
				get { return _ownerReference?.GetTarget(); }
				set { _ownerReference = new WeakReference<FlipView>(value); }
			}
			public override void OnPageSelected(int position)
			{
				var owner = Owner;
				if (owner.SelectedIndex != position)
				{
					owner.SelectedIndex = position;
				}
			}
		}
	}
}
