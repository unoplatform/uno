using System;
using System.Collections.Generic;
using System.Text;
using Android.Support.V7.Widget;
using Android.Views;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase : UnoRecyclerView, ILayoutConstraints
	{
		/// <summary>
		/// Is the RecyclerView currently undergoing animated scrolling, either user-initiated or programmatic.
		/// </summary>
		private bool _isInAnimatedScroll;

		internal ScrollingViewCache ViewCache { get; }

		public NativeListViewBase() : base(ContextHelper.Current)
		{
			InitializeScrollbars();
			VerticalScrollBarEnabled = true;
			HorizontalScrollBarEnabled = true;

			ViewCache = new ScrollingViewCache(this);
			SetViewCacheExtension(ViewCache);

			InitializeSnapHelper();

			MotionEventSplittingEnabled = false;
		}

		partial void InitializeSnapHelper();

		private void InitializeScrollbars()
		{
			// Force scrollbars to initialize since we're not inflating from xml
			if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.Kitkat)
			{
				var styledAttributes = Context.Theme.ObtainStyledAttributes(Resource.Styleable.View);
				InitializeScrollbars(styledAttributes);
				styledAttributes.Recycle();
			}
			else
			{
				InitializeScrollbars(null);
			}
		}

		internal NativeListViewBaseAdapter CurrentAdapter
		{
			get { return GetAdapter() as NativeListViewBaseAdapter; }
			set { SetAdapter(value); }
		}
		internal VirtualizingPanelLayout NativeLayout
		{
			get { return GetLayoutManager() as VirtualizingPanelLayout; }
			set
			{
				SetLayoutManager(value);
				PropagatePadding();
			}
		}

		private Thickness _padding;
		public Thickness Padding
		{
			get
			{
				return _padding;
			}

			set
			{
				_padding = value;
				PropagatePadding();
			}
		}

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get
			{
				return HorizontalScrollBarEnabled ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}

			set
			{
				switch (value)
				{
					case ScrollBarVisibility.Disabled:
					case ScrollBarVisibility.Hidden:
						HorizontalScrollBarEnabled = false;
						break;
					case ScrollBarVisibility.Auto:
					case ScrollBarVisibility.Visible:
					default:
						HorizontalScrollBarEnabled = true;
						break;
				}
			}
		}

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get
			{
				return VerticalScrollBarEnabled ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}

			set
			{
				switch (value)
				{
					case ScrollBarVisibility.Disabled:
					case ScrollBarVisibility.Hidden:
						VerticalScrollBarEnabled = false;
						break;
					case ScrollBarVisibility.Auto:
					case ScrollBarVisibility.Visible:
					default:
						VerticalScrollBarEnabled = true;
						break;
				}
			}
		}

		public void ScrollIntoView(int displayPosition)
		{
			ScrollToPosition(displayPosition);
		}

		public void ScrollIntoView(int displayPosition, ScrollIntoViewAlignment alignment)
		{
			StopScroll();

			if (NativeLayout != null)
			{
				NativeLayout.ScrollToPosition(displayPosition, alignment);

				AwakenScrollBars();
			}
		}

		public override void ScrollTo(int x, int y)
		{
			ScrollBy(x - NativeLayout.HorizontalOffset, y - NativeLayout.VerticalOffset);
		}

		public void SmoothScrollTo(int x, int y)
		{
			SmoothScrollBy(x - NativeLayout.HorizontalOffset, y - NativeLayout.VerticalOffset);
		}

		public override void OnScrolled(int dx, int dy)
		{
			InvokeOnScroll();
		}

		private void InvokeOnScroll()
		{
			XamlParent?.ScrollViewer?.OnScrollInternal(
				ViewHelper.PhysicalToLogicalPixels(NativeLayout.HorizontalOffset),
				ViewHelper.PhysicalToLogicalPixels(NativeLayout.VerticalOffset),
				isIntermediate: _isInAnimatedScroll
				);
		}

		public override void OnScrollStateChanged(int state)
		{
			switch (state)
			{
				case ScrollStateIdle:
					_isInAnimatedScroll = false;
					InvokeOnScroll();
					break;
				case ScrollStateDragging:
				case ScrollStateSettling:
					_isInAnimatedScroll = true;
					break;
			}
		}

		// We override these two methods because the base implementation depends on ComputeScrollOfset, which is not reliable when, eg, items 
		// are inserted/removed out of view.
		public override bool CanScrollHorizontally(int direction)
		{
			return NativeLayout.CanCurrentlyScrollHorizontally(direction);
		}

		public override bool CanScrollVertically(int direction)
		{
			return NativeLayout.CanCurrentlyScrollVertically(direction);
		}

		public void Refresh()
		{
			CurrentAdapter?.NotifyDataSetChanged();

			var isScrollResetting = NativeLayout != null && NativeLayout.ContentOffset != 0;
			NativeLayout?.Refresh();

			if (isScrollResetting)
			{
				// Raise scroll events since offset has been reset to 0
				InvokeOnScroll();
			}
		}

		private void PropagatePadding()
		{
			var asVirtualizingPanelLayout = NativeLayout as VirtualizingPanelLayout;
			if (asVirtualizingPanelLayout != null)
			{
				asVirtualizingPanelLayout.Padding = this.Padding;
			}
		}

		bool ILayoutConstraints.IsWidthConstrained(View requester)
		{
			if (requester != null && NativeLayout?.ScrollOrientation == Orientation.Horizontal)
			{
				//If scroll is horizontal, width change requires relayout of siblings
				return false;
			}

			//Otherwise use standard rules
			return this.IsWidthConstrainedSimple() ?? (base.Parent as ILayoutConstraints)?.IsWidthConstrained(this) ?? false;
		}

		bool ILayoutConstraints.IsHeightConstrained(View requester)
		{
			if (requester != null && NativeLayout?.ScrollOrientation == Orientation.Vertical)
			{
				//If scroll is vertical, height change requires relayout of siblings
				return false;
			}

			return this.IsHeightConstrainedSimple() ?? (base.Parent as ILayoutConstraints)?.IsHeightConstrained(this) ?? false;
		}
	}
}
