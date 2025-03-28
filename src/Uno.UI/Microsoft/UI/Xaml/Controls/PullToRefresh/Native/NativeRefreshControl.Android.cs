using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.SwipeRefreshLayout.Widget;
using Uno.UI.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Rect = Windows.Foundation.Rect;
using ScrollView = Android.Widget.ScrollView;
using UnoScrollViewer = Windows.UI.Xaml.Controls.ScrollViewer;

namespace Uno.UI.Xaml.Controls;

public partial class NativeRefreshControl : SwipeRefreshLayout, IShadowChildrenProvider, DependencyObject, ILayouterElement
{
	// Distance in pixels a touch can wander before we think the user is scrolling
	// https://developer.android.com/reference/android/view/ViewConfiguration.html#getScaledTouchSlop()
	private static readonly float _touchSlop = ViewConfiguration.Get(ContextHelper.Current).ScaledTouchSlop;

	private PointF _gestureStart;
	private bool _isSwiping = false;
	private bool _ignoreGesture = false;
	private Android.Views.View _content;
	private ViewGroup _descendantScrollable;
	private bool _baseOnInterceptTouchResult;

	public NativeRefreshControl() : base(ContextHelper.Current)
	{
		_layouter = new NativeRefreshControlLayouter(this);
	}

	internal Android.Views.View Content
	{
		get => _content;
		set
		{
			if (_content != null)
			{
				RemoveView(_content);
			}
			_content = value;
			if (_content != null)
			{
				AddView(_content);
			}
		}
	}

	public override bool CanChildScrollUp()
	{
		_descendantScrollable = _descendantScrollable ?? GetDescendantScrollable();

		if (_descendantScrollable != null)
		{
			if (_descendantScrollable is UnoScrollViewer unoScrollViewer)
			{
				return unoScrollViewer.VerticalOffset > 0;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				var canScrollUp = ViewCompat.CanScrollVertically(_descendantScrollable, -1);
#pragma warning restore CS0618 // Type or member is obsolete
				return canScrollUp;
			}
		}
		return base.CanChildScrollUp();
	}

	public override bool OnInterceptTouchEvent(MotionEvent e)
	{
		_baseOnInterceptTouchResult = base.OnInterceptTouchEvent(e);
		switch (e.Action)
		{
			case MotionEventActions.Down:
				_isSwiping = false;
				_ignoreGesture = false;
				_gestureStart = new PointF(e.GetX(), e.GetY());
				break;
			case MotionEventActions.Move:
				return ShouldInterceptMove(e);
			default:
				_isSwiping = false;
				_ignoreGesture = false;
				break;
		}

		return _baseOnInterceptTouchResult;
	}

	private bool ShouldInterceptMove(MotionEvent e)
	{
		//If the Swipe already started swiping we let it handle the touch until it is completed.
		if (_isSwiping)
		{
			return true;
		}
		else
		{
			var xDistance = Math.Abs(e.GetX() - _gestureStart.X);
			var yDistance = Math.Abs(e.GetY() - _gestureStart.Y);
			var cumulativeDistance = new Vector2(xDistance, yDistance);

			// Here we validate that the touch is not horizontal. 
			// This is to ensure that it will not override horizontal
			// touches for children such as flipviews or horizontal listviews.
			if (_ignoreGesture || cumulativeDistance.X > _touchSlop)
			{
				_ignoreGesture = true;
				return false;
			}

			if (cumulativeDistance.Y > _touchSlop && // touchSlop is the minimal distance for us to determine it is a scroll
				cumulativeDistance.Y > cumulativeDistance.X && // Check the scroll is vertical
				e.GetY() - _gestureStart.Y > 0 && // Check if the scroll is positive so that we don't intercept the scrolling of the AVP/List
				!CanChildScrollUp()) // Validate if the child can scroll up so that we only allow swipe refresh when the content is scrolled.
			{
				_isSwiping = true;

				return true;
			}
			else
			{
				return false;
			}
		}
	}

	public override bool OnTouchEvent(MotionEvent e)
	{
		if (e.Action == MotionEventActions.Up && !_baseOnInterceptTouchResult)
		{
			// On MotionEventActions.Up, SwipeRefreshLayout.OnTouchEvent checks the current y against mInitialMotionY and sets 
			// refreshing accordingly. However, if OnInterceptTouchEvent hasn't returned true (because user hasn't dragged far 
			// enough to trigger a drag) then mInitialMotionY hasn't been set properly, giving rise to false-positive refreshes. 
			// Hence we don't call base in this case to prevent the false positive.
			return false;
		}

		var baseOnTouchResult = base.OnTouchEvent(e);
		// If we are handling touch and OnInterceptTouchEvent() did not return true, it means there are no touch handlers in the
		// children. (This can happen if, eg, the content is not scrollable.) Give OnInterceptTouchEvent another chance so that
		// SwipeRefreshLayout can set its isDragged state correctly, otherwise indicator won't appear.
		if (baseOnTouchResult && !_baseOnInterceptTouchResult)
		{
			OnInterceptTouchEvent(e);
		}
		return baseOnTouchResult;
	}

	private readonly List<View> _emptyList = new();

	List<View> IShadowChildrenProvider.ChildrenShadow => Content != null ? new List<View>(1) { Content as View } : _emptyList;

	private ILayouter _layouter;

	ILayouter ILayouterElement.Layouter => _layouter;
	Size ILayouterElement.LastAvailableSize => LayoutInformation.GetAvailableSize(this);
	bool ILayouterElement.IsMeasureDirty => true;
	bool ILayouterElement.IsFirstMeasureDoneAndManagedElement => false;
	bool ILayouterElement.StretchAffectsMeasure => true;
	bool ILayouterElement.IsMeasureDirtyPathDisabled => true;

	public override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	{
		base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		((ILayouterElement)this).OnMeasureInternal(widthMeasureSpec, heightMeasureSpec);
	}

	void ILayouterElement.SetMeasuredDimensionInternal(int width, int height)
	{
		SetMeasuredDimension(width, height);
	}

	partial void OnLayoutPartial(bool changed, int left, int top, int right, int bottom)
	{
		var newSize = new Rect(0, 0, right - left, bottom - top).PhysicalToLogicalPixels();

		// WARNING: The layouter must be called every time here,
		// even if the size has not changed. Failing to call the layouter
		// may leave the default ScrollViewer implementation place 
		// the child at an invalid location when the visibility changes.

		_layouter.Arrange(newSize);
	}

	private class NativeRefreshControlLayouter : Layouter
	{
		public NativeRefreshControlLayouter(NativeRefreshControl view) : base(view)
		{
		}

		private NativeRefreshControl RefreshControl => Panel as NativeRefreshControl;

		protected override void MeasureChild(View child, int widthSpec, int heightSpec)
		{
			var childMargin = (child as FrameworkElement)?.Margin ?? Thickness.Empty;

			RefreshControl.Content?.Measure(widthSpec, heightSpec);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var child = RefreshControl.Content;

			var desiredChildSize = default(Size);
			if (child != null)
			{
				var scrollSpace = availableSize;

				desiredChildSize = MeasureChild(child, scrollSpace);

				// Give opportunity to the the content to define the viewport size itself
				(child as ICustomScrollInfo)?.ApplyViewport(ref desiredChildSize);
			}

			return desiredChildSize;
		}

		protected override Size ArrangeOverride(Size slotSize)
		{
			var child = RefreshControl.Content;

			if (child != null)
			{
				ArrangeChild(child, new Rect(
					0,
					0,
					slotSize.Width,
					slotSize.Height
				));

				// Give opportunity to the the content to define the viewport size itself
				(child as ICustomScrollInfo)?.ApplyViewport(ref slotSize);

			}

			return slotSize;
		}

		protected override string Name => Panel.Name;
	}

	private ViewGroup GetDescendantScrollable()
	{
		return getScrollablesInChildren(this).FirstOrDefault();

		IEnumerable<ViewGroup> getScrollablesInChildren(ViewGroup parent)
		{
			for (int i = 0; i < parent.ChildCount; i++)
			{
				var child = parent.GetChildAt(i);

				if (child is ViewGroup v)
				{
					// Get the IncludeInSwipeRefresh attached property on the FrameworkElement
					// (False specifies that the element will not be discovered as a descendant scroller)
					var element = (child as FrameworkElement);

					if (v is ScrollView || v is AbsListView || v is RecyclerView || v is UnoScrollViewer)
					{
						yield return v;
					}
					foreach (var vInner in getScrollablesInChildren(v))
					{
						yield return vInner;
					}
				}
			}
		}
	}
}
