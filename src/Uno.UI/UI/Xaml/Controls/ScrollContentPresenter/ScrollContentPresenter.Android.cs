using Android.Views;
using Android.Widget;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using Uno.UI.Controls;
using Uno.UI;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : UnoTwoDScrollView, IShadowChildrenProvider, DependencyObject
	{
		private static readonly List<View> _emptyList = new List<View>(0);

		private ScrollBarVisibility _verticalScrollBarVisibility;
		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return _verticalScrollBarVisibility; }
			set
			{
				_verticalScrollBarVisibility = value;
				UpdateScrollSettings();
			}
		}

		public ScrollBarVisibility _horizontalScrollBarVisibility;
		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return _horizontalScrollBarVisibility; }
			set
			{
				_horizontalScrollBarVisibility = value;
				UpdateScrollSettings();
			}
		}

		public ScrollMode _horizontalScrollMode;
		public ScrollMode HorizontalScrollMode
		{
			get { return _horizontalScrollMode; }
			set
			{
				_horizontalScrollMode = value;
				UpdateScrollSettings();
			}
		}

		private ScrollMode _verticalScrollMode;
		public ScrollMode VerticalScrollMode
		{
			get { return _verticalScrollMode; }
			set
			{
				_verticalScrollMode = value;
				UpdateScrollSettings();
			}
		}

		private ILayouter _layouter;

		public ScrollContentPresenter()
			: base(ContextHelper.Current)
		{
			InitializeBinder();
			InitializeScrollContentPresenter();
			InitializeScrollbars();

			SetForegroundGravity(GravityFlags.Fill);

			SetClipToPadding(false);
			ScrollBarStyle = ScrollbarStyles.OutsideOverlay; // prevents padding from affecting scrollbar position

			_layouter = new ScrollViewerLayouter(this);

			MotionEventSplittingEnabled = false;
		}

		private void InitializeScrollbars()
		{
			// Force scrollbars to initialize since we're not inflating from xml
			if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.Kitkat)
			{
				var styledAttributes = Context.Theme.ObtainStyledAttributes(Uno.UI.Resource.Styleable.View);
				InitializeScrollbars(styledAttributes);
				styledAttributes.Recycle();
			}
			else
			{
				InitializeScrollbars(null);
			}
		}

		List<View> IShadowChildrenProvider.ChildrenShadow => Content != null ? new List<View>(1) { Content } : _emptyList;

		partial void OnContentChanged(View previousView, View newView)
		{
			if (previousView != null)
			{
				RemoveView(previousView);
			}

			if (newView != null)
			{
				AddView(newView);
			}
		}

		//TODO generated code
		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var availableSize = ViewHelper.LogicalSizeFromSpec(widthMeasureSpec, heightMeasureSpec);

			if (!double.IsNaN(Width) || !double.IsNaN(Height))
			{
				availableSize = new Size(
					double.IsNaN(Width) ? availableSize.Width : Width,
					double.IsNaN(Height) ? availableSize.Height : Height
				);
			}

			var measuredSize = _layouter.Measure(availableSize);

			measuredSize = measuredSize.LogicalToPhysicalPixels();

			// Report our final dimensions.
			SetMeasuredDimension(
				(int)measuredSize.Width,
				(int)measuredSize.Height
			);

			IFrameworkElementHelper.OnMeasureOverride(this);
		}

		//TODO generated code
		partial void OnLayoutPartial(bool changed, int left, int top, int right, int bottom)
		{
			var newSize = new Rect(0, 0, right - left, bottom - top).PhysicalToLogicalPixels();

			// WARNING: The layouter must be called every time here,
			// even if the size has not changed. Failing to call the layouter
			// may leave the default ScrollViewer implementation place 
			// the child at an invalid location when the visibility changes.

			_layouter.Arrange(newSize);
		}

		private void UpdateScrollSettings()
		{
			var verticalScrollVisible = VerticalScrollBarVisibility == ScrollBarVisibility.Auto || VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
			var verticalScrollEnabled = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled && VerticalScrollMode != ScrollMode.Disabled;
			var horizontalScrollVisible = HorizontalScrollBarVisibility == ScrollBarVisibility.Auto || HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
			var horizontalScrollEnabled = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled && HorizontalScrollMode != ScrollMode.Disabled;

			VerticalScrollBarEnabled = verticalScrollVisible;
			HorizontalScrollBarEnabled = horizontalScrollVisible;

			// TODO: for now there's no way to only disable scrolling in one direction
			IsScrollingEnabled = verticalScrollEnabled || horizontalScrollEnabled;
		}

		private class ScrollViewerLayouter : Layouter
		{
			public ScrollViewerLayouter(ScrollContentPresenter view) : base(view)
			{
			}

			private ScrollContentPresenter ScrollContentPresenter => Panel as ScrollContentPresenter;

			protected override void MeasureChild(View child, int widthSpec, int heightSpec)
			{
				var childMargin = (child as FrameworkElement)?.Margin ?? Thickness.Empty;
				ScrollContentPresenter.SetChildMargin(childMargin);

				this.GetChildren().FirstOrDefault()?.Measure(widthSpec, heightSpec);
			}

			protected override Size MeasureOverride(Size availableSize)
			{
				var child = this.GetChildren().FirstOrDefault();

				var desiredChildSize = default(Size);
				if (child != null)
				{
					var scrollSpace = availableSize;
					if (ScrollContentPresenter.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
					{
						scrollSpace.Height = double.PositiveInfinity;
					}
					if (ScrollContentPresenter.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
					{
						scrollSpace.Width = double.PositiveInfinity;
					}

					desiredChildSize = MeasureChild(child, scrollSpace);
				}

				return desiredChildSize;
			}

			protected override Size ArrangeOverride(Size slotSize)
			{
				var child = this.GetChildren().FirstOrDefault();

				if (child != null)
				{
					var desiredChildSize = DesiredChildSize(child);

					var occludedPadding = (Panel as ScrollContentPresenter)._occludedRectPadding;
					slotSize.Width -= occludedPadding.Left + occludedPadding.Right;
					slotSize.Height -= occludedPadding.Top + occludedPadding.Bottom;

					var width = Math.Max(slotSize.Width, desiredChildSize.Width);
					var height = Math.Max(slotSize.Height, desiredChildSize.Height);

					ArrangeChild(child, new Rect(
						0,
						0,
						width,
						height
					));
				}

				return slotSize;
			}

			protected override string Name => Panel.Name;
		}

		protected override void OnScrollChanged(int l, int t, int oldl, int oldt, bool isIntermediate)
		{
			// Does nothing, so avoid useless interop!
			// base.OnScrollChanged(l, t, oldl, oldt, isIntermediate);

			(TemplatedParent as ScrollViewer)?.OnScrollInternal(
				ViewHelper.PhysicalToLogicalPixels(l),
				ViewHelper.PhysicalToLogicalPixels(t),
				isIntermediate
			);
		}

		protected override void OnZoomScaleChanged(float p0, float p1)
		{
			(TemplatedParent as ScrollViewer)?.OnZoomInternal(p1);
		}

		public Rect MakeVisible(UIElement visual, Rect rectangle)
		{
			if (visual is FrameworkElement fe)
			{
				var scrollRect = new Rect(
					_occludedRectPadding.Left,
					_occludedRectPadding.Top,
					ActualWidth - _occludedRectPadding.Right,
					ActualHeight - _occludedRectPadding.Bottom
				);

				var visualPoint = UIElement.TransformToVisual(visual, this).TransformPoint(new Point());
				var visualRect = new Rect(visualPoint, new Size(fe.ActualWidth, fe.ActualHeight));

				var deltaX = Math.Min(visualRect.Left - scrollRect.Left, Math.Max(0, visualRect.Right - scrollRect.Right));
				var deltaY = Math.Min(visualRect.Top - scrollRect.Top, Math.Max(0, visualRect.Bottom - scrollRect.Bottom));

				SmoothScrollBy(
					ViewHelper.LogicalToPhysicalPixels(deltaX),
					ViewHelper.LogicalToPhysicalPixels(deltaY)
				);
			}

			return rectangle;
		}

		IDisposable IScrollContentPresenter.Pad(Rect occludedRect)
		{
			var viewPortPoint = UIElement.TransformToVisual(this, null).TransformPoint(new Point());
			var viewPortSize = new Size(ActualWidth, ActualHeight);
			var viewPortRect = new Rect(viewPortPoint, viewPortSize);
			var intersection = viewPortRect;
			intersection.Intersect(occludedRect);
			if (intersection.IsEmpty)
			{
				SetOccludedRectPadding(new Thickness());
			}
			else
			{
				SetOccludedRectPadding(new Thickness(0, 0, 0, intersection.Height));
			}

			return Disposable.Create(() => SetOccludedRectPadding(new Thickness()));
		}

		private Thickness _childMargin;
		private void SetChildMargin(Thickness childMargin)
		{
			// We're using the ScrollView's padding as the child's margin
			// because the native ScrollView determines the scroll area 
			// based on the measured size of its children, which doesn't account for margins.
			_childMargin = childMargin;
			UpdatePadding();
		}

		private Thickness _occludedRectPadding;

		private void SetOccludedRectPadding(Thickness occludedRectPadding)
		{
			_occludedRectPadding = occludedRectPadding;
			UpdatePadding();
		}

		private void UpdatePadding()
		{
			SetPadding(
				ViewHelper.LogicalToPhysicalPixels(_occludedRectPadding.Left + _childMargin.Left),
				ViewHelper.LogicalToPhysicalPixels(_occludedRectPadding.Top + _childMargin.Top),
				ViewHelper.LogicalToPhysicalPixels(_occludedRectPadding.Right + _childMargin.Right),
				ViewHelper.LogicalToPhysicalPixels(_occludedRectPadding.Bottom + _childMargin.Bottom)
			);
		}
	}
}
