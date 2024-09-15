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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Rect = Windows.Foundation.Rect;
using ScrollView = Android.Widget.ScrollView;
using UnoScrollViewer = Microsoft.UI.Xaml.Controls.ScrollViewer;

namespace Uno.UI.Xaml.Controls;

public partial class NativeRefreshControl : SwipeRefreshLayout, IShadowChildrenProvider, DependencyObject
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

	public override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	{
		base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
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
