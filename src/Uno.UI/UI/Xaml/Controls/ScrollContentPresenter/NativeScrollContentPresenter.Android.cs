using Android.Views;
using Android.Widget;
using Uno.Extensions;
using Uno.Foundation.Logging;
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
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	partial class NativeScrollContentPresenter : UnoTwoDScrollView, IShadowChildrenProvider, DependencyObject, ILayouterElement
	{
		private static readonly List<View> _emptyList = new List<View>(0);

		private ScrollViewer ScrollOwner => _scrollViewer.TryGetTarget(out var s) ? s : (Parent as FrameworkElement)?.TemplatedParent as ScrollViewer;

		private ScrollBarVisibility _verticalScrollBarVisibility;
		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get => _verticalScrollBarVisibility;
			set
			{
				_verticalScrollBarVisibility = value;
				UpdateScrollSettings();
			}
		}

		private ScrollBarVisibility _horizontalScrollBarVisibility;
		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => _horizontalScrollBarVisibility;
			set
			{
				_horizontalScrollBarVisibility = value;
				UpdateScrollSettings();
			}
		}

		private ILayouter _layouter;
		private readonly WeakReference<ScrollViewer> _scrollViewer;

		public NativeScrollContentPresenter(ScrollViewer scroller) : this()
		{
			_scrollViewer = new WeakReference<ScrollViewer>(scroller);
		}

		public NativeScrollContentPresenter()
			: base(ContextHelper.Current)
		{
			InitializeScrollbars();

			SetForegroundGravity(GravityFlags.Fill);

			SetClipToPadding(false);
			SetClipChildren(false);
			ScrollBarStyle = ScrollbarStyles.OutsideOverlay; // prevents padding from affecting scrollbar position

			_layouter = new ScrollViewerLayouter(this);
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

			if (FeatureConfiguration.ScrollViewer.AndroidScrollbarFadeDelay != null)
			{
				ScrollBarDefaultDelayBeforeFade = (int)FeatureConfiguration.ScrollViewer.AndroidScrollbarFadeDelay.Value.TotalMilliseconds;
			}
		}

		List<View> IShadowChildrenProvider.ChildrenShadow => Content != null ? new List<View>(1) { Content as View } : _emptyList;

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

		ILayouter ILayouterElement.Layouter => _layouter;
		Size ILayouterElement.LastAvailableSize => LayoutInformation.GetAvailableSize(this);
		bool ILayouterElement.IsMeasureDirty => true;
		bool ILayouterElement.IsFirstMeasureDoneAndManagedElement => false;
		bool ILayouterElement.StretchAffectsMeasure => false;
		bool ILayouterElement.IsMeasureDirtyPathDisabled => true;

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
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

			// base.OnLayout is not invoked in the mixin to allow for the clipping algorithms
			base.OnLayout(changed, left, top, right, bottom);
		}

		private void UpdateScrollSettings()
		{
			var verticalScrollVisible = VerticalScrollBarVisibility == ScrollBarVisibility.Auto || VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
			var verticalScrollEnabled = VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
			var horizontalScrollVisible = HorizontalScrollBarVisibility == ScrollBarVisibility.Auto || HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
			var horizontalScrollEnabled = HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;

			VerticalScrollBarEnabled = verticalScrollVisible;
			HorizontalScrollBarEnabled = horizontalScrollVisible;

			// TODO: for now there's no way to only disable scrolling in one direction
			IsScrollingEnabled = verticalScrollEnabled || horizontalScrollEnabled;
		}

		private class ScrollViewerLayouter : Layouter
		{
			public ScrollViewerLayouter(NativeScrollContentPresenter view) : base(view)
			{
			}

			private NativeScrollContentPresenter ScrollContentPresenter => Panel as NativeScrollContentPresenter;

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

					// Give opportunity to the the content to define the viewport size itself
					(child as ICustomScrollInfo)?.ApplyViewport(ref desiredChildSize);
				}

				return desiredChildSize;
			}

			protected override Size ArrangeOverride(Size slotSize)
			{
				var child = this.GetChildren().FirstOrDefault();

				if (child != null)
				{
					var desiredChildSize = LayoutInformation.GetDesiredSize(child);

					var occludedPadding = ScrollContentPresenter._padding;
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

					ScrollContentPresenter.ScrollOwner?.TryApplyPendingScrollTo();

					// Give opportunity to the the content to define the viewport size itself
					(child as ICustomScrollInfo)?.ApplyViewport(ref slotSize);

				}

				return slotSize;
			}

			protected override string Name => Panel.Name;
		}

		#region Managed to native
		private Thickness _padding;
		Thickness INativeScrollContentPresenter.Padding
		{
			get => _padding;
			set
			{
				_padding = value;
				UpdatePadding();
			}
		}

		void INativeScrollContentPresenter.SmoothScrollBy(int physicalDeltaX, int physicalDeltaY)
			=> SmoothScrollBy(physicalDeltaX, physicalDeltaY);

		bool INativeScrollContentPresenter.Set(
			double? horizontalOffset,
			double? verticalOffset,
			float? zoomFactor,
			bool disableAnimation,
			bool isIntermediate)
			=> throw new NotImplementedException();
		#endregion

		#region Native to managed
		protected override void OnScrollChanged(int scrollX, int scrollY, bool isIntermediate)
		{
			// Does nothing, so avoid useless interop!
			// base.OnScrollChanged(scrollX, scrollY, isIntermediate);

			ScrollOwner?.Presenter?.OnNativeScroll(
				ViewHelper.PhysicalToLogicalPixels(scrollX),
				ViewHelper.PhysicalToLogicalPixels(scrollY),
				isIntermediate
			);
		}

		protected override void OnZoomScaleChanged(float p0, float p1)
		{
			ScrollOwner?.Presenter?.OnNativeZoom(p1);
		}
		#endregion

		private Thickness _childMargin;
		private void SetChildMargin(Thickness childMargin)
		{
			// We're using the ScrollView's padding as the child's margin
			// because the native ScrollView determines the scroll area 
			// based on the measured size of its children, which doesn't account for margins.
			_childMargin = childMargin;
			UpdatePadding();
		}

		private void UpdatePadding()
		{
			SetPadding(
				ViewHelper.LogicalToPhysicalPixels(_padding.Left + _childMargin.Left),
				ViewHelper.LogicalToPhysicalPixels(_padding.Top + _childMargin.Top),
				ViewHelper.LogicalToPhysicalPixels(_padding.Right + _childMargin.Right),
				ViewHelper.LogicalToPhysicalPixels(_padding.Bottom + _childMargin.Bottom)
			);
		}
	}
}
