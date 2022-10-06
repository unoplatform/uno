using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
using ScrollView = Android.Widget.ScrollView;
using UnoScrollViewer = Windows.UI.Xaml.Controls.ScrollViewer;

namespace Uno.UI.Xaml.Controls;

public partial class NativeRefreshControl : SwipeRefreshLayout, IShadowChildrenProvider, DependencyObject, ILayouterElement
{
	// Distance in pixels a touch can wander before we think the user is scrolling
	// https://developer.android.com/reference/android/view/ViewConfiguration.html#getScaledTouchSlop()
	private static readonly float _touchSlop = ViewConfiguration.Get(ContextHelper.Current).ScaledTouchSlop;
	private Android.Views.View _content;
	private ViewGroup _descendantScrollable;

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

	private readonly List<View> _emptyList = new();

	List<View> IShadowChildrenProvider.ChildrenShadow => Content != null ? new List<View>(1) { Content as View } : _emptyList;

	private ILayouter _layouter;

	ILayouter ILayouterElement.Layouter => _layouter;
	Size ILayouterElement.LastAvailableSize => LayoutInformation.GetAvailableSize(this);
	bool ILayouterElement.IsMeasureDirty => true;
	bool ILayouterElement.IsFirstMeasureDoneAndManagedElement => false;
	bool ILayouterElement.StretchAffectsMeasure => false;
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
