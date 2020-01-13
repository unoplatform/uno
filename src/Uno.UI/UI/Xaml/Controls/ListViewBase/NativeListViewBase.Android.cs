using System;
using System.Collections.Generic;
using System.Text;
using Android.Support.V7.Widget;
using Android.Views;
using Uno.Extensions;
using Uno.UI;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase : UnoRecyclerView, ILayoutConstraints
	{
		/// <summary>
		/// Is the RecyclerView currently undergoing animated scrolling, either user-initiated or programmatic.
		/// </summary>
		private bool _isInAnimatedScroll;

		bool _shouldRecalibrateFlingVelocity;
		float? _previousX, _previousY;
		float? _deltaX, _deltaY;

		internal BufferViewCache ViewCache { get; }

		internal IEnumerable<SelectorItem> CachedItemViews => ViewCache.CachedItemViews;

		public override OverScrollMode OverScrollMode
		{
			get
			{
				// This duplicates the logic in Android.Views.View.overScrollBy(), which for some reason RecyclerView doesn't use, but
				// only checks getOverScrollMode(). This ensures edge effects aren't shown if content is too small to scroll.
				if (NativeLayout == null)
				{
					return base.OverScrollMode;
				}
				else if (NativeLayout.ScrollOrientation == Orientation.Vertical)
				{
					return ComputeVerticalScrollRange() > ComputeVerticalScrollExtent() ?
						base.OverScrollMode :
						OverScrollMode.Never;
				}
				else
				{
					return ComputeHorizontalScrollRange() > ComputeHorizontalScrollExtent() ?
						base.OverScrollMode :
						OverScrollMode.Never;
				}
			}
		}

		public NativeListViewBase() : base(ContextHelper.Current)
		{
			InitializeScrollbars();
			VerticalScrollBarEnabled = true;
			HorizontalScrollBarEnabled = true;

			if (FeatureConfiguration.NativeListViewBase.RemoveItemAnimator)
			{
				SetItemAnimator(null);
			}

			ViewCache = new BufferViewCache(this);
			SetViewCacheExtension(ViewCache);

			InitializeSnapHelper();

			_shouldRecalibrateFlingVelocity = (int)Android.OS.Build.VERSION.SdkInt >= 28; // Android.OS.BuildVersionCodes.P

			// // This is required for animations not to be cut off by transformed ancestor views. (#1333)
			SetClipChildren(false);
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

				if(FeatureConfiguration.ScrollViewer.AndroidScrollbarFadeDelay != null)
				{
					ScrollBarDefaultDelayBeforeFade = (int)FeatureConfiguration.ScrollViewer.AndroidScrollbarFadeDelay.Value.TotalMilliseconds;
				}
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

		protected override void AttachViewToParent(View child, int index, ViewGroup.LayoutParams layoutParams)
		{
			var vh = GetChildViewHolder(child);
			if (vh != null)
			{
				vh.IsDetached = false;
			}
			base.AttachViewToParent(child, index, layoutParams);
		}

		protected override void DetachViewFromParent(int index)
		{
			var view = GetChildAt(index);
			if (view != null)
			{
				var vh = GetChildViewHolder(view);
				if (vh != null)
				{
					vh.IsDetached = true;
				}
			}
			base.DetachViewFromParent(index);
		}

		protected override void RemoveDetachedView(View child, bool animate)
		{
			var vh = GetChildViewHolder(child);
			if (vh != null)
			{
				vh.IsDetached = false;
			}
#if DEBUG
			if (!vh.IsDetachedPrivate)
			{
				// Preempt the unmanaged exception 'Java.Lang.IllegalArgumentException: Called removeDetachedView with a view which is not flagged as tmp detached.' for easier debugging.
				throw new InvalidOperationException($"View {child} is not flagged tmp detached.");
			}
#endif
			base.RemoveDetachedView(child, animate);
		}

		partial void OnUnloadedPartial()
		{
			ViewCache?.OnUnloaded();
		}

		public void Refresh()
		{
			CurrentAdapter?.Refresh();

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

		internal new UnoViewHolder GetChildViewHolder(View view) => base.GetChildViewHolder(view) as UnoViewHolder;

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

		public override bool Fling(int velocityX, int velocityY)
		{
			if (_shouldRecalibrateFlingVelocity)
			{
				// Workaround for inverted ListView receiving fling velocity in the opposite direction
				// See: https://issuetracker.google.com/u/1/issues/112385925
				velocityX = Recalibrate(velocityX, _deltaX);
				velocityY = Recalibrate(velocityY, _deltaY);
			}

			return base.Fling(velocityX, velocityY);

			int Recalibrate(int value, float? hint)
			{
				// note: the opposite sign should be used to recalibrate the velocity
				return hint.HasValue && hint != 0
					? Math.Abs(value) * -Math.Sign(hint.Value)
					: value;
			}
		}

		internal void TrackMotionDirections(MotionEvent e)
		{
			if (!_shouldRecalibrateFlingVelocity)
			{
				return;
			}

			switch (e.Action)
			{
				case MotionEventActions.Down:
					_deltaX = _deltaY = null;
					SaveCurrentCoordinates();
					break;

				case MotionEventActions.Move:
					ComputeMotionDirections();
					SaveCurrentCoordinates();
					break;

				case MotionEventActions.Up:
					// Note that ACTION_UP and ACTION_POINTER_UP always report the last known position
					// of the pointers that went up. -- VelocityTracker.cpp
					// Ignoring this one, since trying to compute directions here will always return 0s.
					break;

				case MotionEventActions.Cancel:
					_deltaX = _deltaY = null;
					break;
			}

			void SaveCurrentCoordinates()
			{
				_previousX = e.GetX();
				_previousY = e.GetY();
			}
			void ComputeMotionDirections()
			{
				_deltaX = e.GetX() - (e.HistorySize > 0 ? e.GetHistoricalX(e.HistorySize - 1) : _previousX);
				_deltaY = e.GetY() - (e.HistorySize > 0 ? e.GetHistoricalY(e.HistorySize - 1) : _previousY);
			}
		}
	}
}
