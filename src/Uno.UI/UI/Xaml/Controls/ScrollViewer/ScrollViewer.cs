#if NET461
#pragma warning disable CS0067
#endif

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Devices.Input;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using Uno;
using Uno.Extensions;
using Microsoft.Extensions.Logging;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using View = AppKit.NSView;
using AppKit;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using _ScrollContentPresenter = Windows.UI.Xaml.Controls.ScrollContentPresenter;
#else
using _ScrollContentPresenter = Windows.UI.Xaml.Controls.IScrollContentPresenter;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollViewer : ContentControl, IFrameworkTemplatePoolAware
	{
		private static class Parts
		{
			public static class Uwp
			{
				public const string ScrollContentPresenter = "ScrollContentPresenter";
				public const string VerticalScrollBar = "VerticalScrollBar";
				public const string HorizontalScrollBar = "HorizontalScrollBar";
			}

			public static class WinUI3
			{
				public const string Scroller = "PART_Scroller";
				public const string VerticalScrollBar = "PART_VerticalScrollBar";
				public const string HorizontalScrollBar = "PART_HorizontalScrollBar";
			}
		}

		private static class VisualStates
		{
			public static class ScrollingIndicator
			{
				public const string None = "NoIndicator";
				public const string Touch = "TouchIndicator";
				public const string Mouse = "MouseIndicator";
				// public const string MouseFull = "MouseIndicatorFull"; // No supported yet
			}

			public static class ScrollBarsSeparator
			{
				public const string Collapsed = "ScrollBarSeparatorCollapsed";
				public const string Expanded = "ScrollBarSeparatorExpanded";
				public const string ExpandedWithoutAnimation = "ScrollBarSeparatorExpandedWithoutAnimation";
				public const string CollapsedWithoutAnimation = "ScrollBarSeparatorCollapsedWithoutAnimation";

				// On WinUI3 visuals states are prefixed with "ScrolBar***s***" (with a trailing 's')
				//public const string Collapsed = "ScrollBarsSeparatorCollapsed";
				//public const string CollapsedDisabled = "ScrollBarsSeparatorCollapsedDisabled"; // Not supported yet
				//public const string Expanded = "ScrollBarsSeparatorExpanded";
				//public const string DisplayedWithoutAnimation = "ScrollBarsSeparatorDisplayedWithoutAnimation"; // Not supported yet
				//public const string ExpandedWithoutAnimation = "ScrollBarsSeparatorExpandedWithoutAnimation";
				//public const string CollapsedWithoutAnimation = "ScrollBarsSeparatorCollapsedWithoutAnimation";
			}
		}

		private static bool IsAnimationEnabled => Uno.UI.Helpers.WinUI.SharedHelpers.IsAnimationsEnabled();

		/// <summary>
		/// Occurs when manipulations such as scrolling and zooming have caused the view to change.
		/// </summary>
		public event EventHandler<ScrollViewerViewChangedEventArgs>? ViewChanged;

		static ScrollViewer()
		{
#if !NET461
			HorizontalContentAlignmentProperty.OverrideMetadata(
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(HorizontalAlignment.Stretch)
			);

			VerticalContentAlignmentProperty.OverrideMetadata(
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(VerticalAlignment.Top)
			);
#endif
		}

		public ScrollViewer()
		{
			DefaultStyleKey = typeof(ScrollViewer);

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// On Skia, the Scrolling is managed by the ScrollContentPresenter (as UWP), which is flagged as IsScrollPort.
			// Note: We should still add support for the zoom factor ... which is not yet supported on Skia.
			// Note 2: This as direct consequences in UIElement.GetTransform and VisualTreeHelper.SearchDownForTopMostElementAt
			UIElement.RegisterAsScrollPort(this);
#endif

			UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewer.GetUpdatesMode(this);
			InitializePartial();

			Loaded += AttachScrollBars;
			Unloaded += DetachScrollBars;
			Unloaded += ResetScrollIndicator;
		}

		partial void InitializePartial();

		#region -- Common DP callbacks --
		private static PropertyChangedCallback OnHorizontalScrollabilityPropertyChanged = (obj, _)
			=> (obj as ScrollViewer)?.UpdateComputedHorizontalScrollability(invalidate: true);
		private static PropertyChangedCallback OnVerticalScrollabilityPropertyChanged = (obj, _)
			=> (obj as ScrollViewer)?.UpdateComputedVerticalScrollability(invalidate: true);
		#endregion

		#region HorizontalScrollBarVisibility (Attached DP - inherited)
		public static ScrollBarVisibility GetHorizontalScrollBarVisibility(DependencyObject obj)
			=> (ScrollBarVisibility)obj.GetValue(HorizontalScrollBarVisibilityProperty);

		public static void SetHorizontalScrollBarVisibility(DependencyObject obj, ScrollBarVisibility value)
			=> obj.SetValue(HorizontalScrollBarVisibilityProperty, value);

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => (ScrollBarVisibility)this.GetValue(HorizontalScrollBarVisibilityProperty);
			set => this.SetValue(HorizontalScrollBarVisibilityProperty, value);
		}

		public static DependencyProperty HorizontalScrollBarVisibilityProperty { get; } =
			DependencyProperty.RegisterAttached(
				"HorizontalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollBarVisibility.Disabled,
					propertyChangedCallback: OnHorizontalScrollabilityPropertyChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
				)
			);
		#endregion

		#region VerticalScrollBarVisibility (Attached DP - inherited)
		public static ScrollBarVisibility GetVerticalScrollBarVisibility(DependencyObject obj)
			=> (ScrollBarVisibility)obj.GetValue(VerticalScrollBarVisibilityProperty);

		public static void SetVerticalScrollBarVisibility(DependencyObject obj, ScrollBarVisibility value)
			=> obj.SetValue(VerticalScrollBarVisibilityProperty, value);

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get => (ScrollBarVisibility)this.GetValue(VerticalScrollBarVisibilityProperty);
			set => this.SetValue(VerticalScrollBarVisibilityProperty, value);
		}

		public static DependencyProperty VerticalScrollBarVisibilityProperty { get; } =
			DependencyProperty.RegisterAttached(
				"VerticalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollBarVisibility.Auto,
					propertyChangedCallback: OnVerticalScrollabilityPropertyChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
				)
			);
		#endregion

		#region HorizontalScrollMode (Attached DP - inherited)
		public static ScrollMode GetHorizontalScrollMode(DependencyObject obj)
			=> (ScrollMode)obj.GetValue(HorizontalScrollModeProperty);

		public static void SetHorizontalScrollMode(DependencyObject obj, ScrollMode value)
			=> obj.SetValue(HorizontalScrollModeProperty, value);

		public ScrollMode HorizontalScrollMode
		{
			get => (ScrollMode)this.GetValue(HorizontalScrollModeProperty);
			set => this.SetValue(HorizontalScrollModeProperty, value);
		}

		public static DependencyProperty HorizontalScrollModeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"HorizontalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnHorizontalScrollabilityPropertyChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
				)
			);
		#endregion

		#region VerticalScrollMode (Attached DP - inherited)

		public static ScrollMode GetVerticalScrollMode(DependencyObject obj)
			=> (ScrollMode)obj.GetValue(VerticalScrollModeProperty);

		public static void SetVerticalScrollMode(DependencyObject obj, ScrollMode value)
			=> obj.SetValue(VerticalScrollModeProperty, value);

		public ScrollMode VerticalScrollMode
		{
			get => (ScrollMode)this.GetValue(VerticalScrollModeProperty);
			set => this.SetValue(VerticalScrollModeProperty, value);
		}

		// Using a DependencyProperty as the backing store for VerticalScrollMode.  This enables animation, styling, binding, etc...
		public static DependencyProperty VerticalScrollModeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"VerticalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnVerticalScrollabilityPropertyChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
				)
			);
		#endregion

		#region BringIntoViewOnFocusChange (Attached DP - inherited)
#if __IOS__
		[global::Uno.NotImplemented]
#endif
		public static bool GetBringIntoViewOnFocusChange(global::Windows.UI.Xaml.DependencyObject element)
			=> (bool)element.GetValue(BringIntoViewOnFocusChangeProperty);

#if __IOS__
		[global::Uno.NotImplemented]
#endif
		public static void SetBringIntoViewOnFocusChange(global::Windows.UI.Xaml.DependencyObject element, bool bringIntoViewOnFocusChange)
			=> element.SetValue(BringIntoViewOnFocusChangeProperty, bringIntoViewOnFocusChange);

#if __IOS__
		[global::Uno.NotImplemented]
#endif
		public bool BringIntoViewOnFocusChange
		{
			get => (bool)GetValue(BringIntoViewOnFocusChangeProperty);
			set => SetValue(BringIntoViewOnFocusChangeProperty, value);
		}

		public static DependencyProperty BringIntoViewOnFocusChangeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"BringIntoViewOnFocusChange",
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					true,
					propertyChangedCallback: OnBringIntoViewOnFocusChangeChanged,
					options: FrameworkPropertyMetadataOptions.Inherits));

		private static void OnBringIntoViewOnFocusChangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = dependencyObject as ScrollViewer;

			view?.OnBringIntoViewOnFocusChangeChangedPartial((bool)args.NewValue);
		}

		partial void OnBringIntoViewOnFocusChangeChangedPartial(bool newValue);
		#endregion

		#region ZoomMode (Attached DP - inherited)
		public static ZoomMode GetZoomMode(DependencyObject element)
			=> (ZoomMode)element.GetValue(ZoomModeProperty);

		public static void SetZoomMode(DependencyObject element, ZoomMode zoomMode)
			=> element.SetValue(ZoomModeProperty, zoomMode);

		public ZoomMode ZoomMode
		{
			get => (ZoomMode)GetValue(ZoomModeProperty);
			set => SetValue(ZoomModeProperty, value);
		}

		public static DependencyProperty ZoomModeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"ZoomMode",
				typeof(ZoomMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ZoomMode.Disabled,
					propertyChangedCallback: (o, e) => ((ScrollViewer)o).OnZoomModeChanged((ZoomMode)e.NewValue),
					options: FrameworkPropertyMetadataOptions.Inherits
				)
			);

		private void OnZoomModeChanged(ZoomMode zoomMode)
		{
			OnZoomModeChangedPartial(zoomMode);
		}

		partial void OnZoomModeChangedPartial(ZoomMode zoomMode);
		#endregion

		#region MinZoomFactor (DP)
		public float MinZoomFactor
		{
			get => (float)GetValue(MinZoomFactorProperty);
			set => SetValue(MinZoomFactorProperty, value);
		}

		public static DependencyProperty MinZoomFactorProperty { get; } =
			DependencyProperty.Register("MinZoomFactor", typeof(float), typeof(ScrollViewer), new FrameworkPropertyMetadata(0.1f, (o, e) => ((ScrollViewer)o).OnMinZoomFactorChanged(e)));

		private void OnMinZoomFactorChanged(DependencyPropertyChangedEventArgs args)
		{
			_presenter?.OnMinZoomFactorChanged((float)args.NewValue);
		}
		#endregion

		#region MaxZoomFactor (DP)
		public float MaxZoomFactor
		{
			get => (float)GetValue(MaxZoomFactorProperty);
			set => SetValue(MaxZoomFactorProperty, value);
		}

		public static DependencyProperty MaxZoomFactorProperty { get; } =
			DependencyProperty.Register("MaxZoomFactor", typeof(float), typeof(ScrollViewer), new FrameworkPropertyMetadata(10f, (o, e) => ((ScrollViewer)o).OnMaxZoomFactorChanged(e)));

		private void OnMaxZoomFactorChanged(DependencyPropertyChangedEventArgs args)
		{
			_presenter?.OnMaxZoomFactorChanged((float)args.NewValue);
		}
		#endregion

		#region ZoomFactor (DP - readonly)
		public float ZoomFactor
		{
			get => (float)GetValue(ZoomFactorProperty);
			private set { SetValue(ZoomFactorProperty, value); }
		}

		public static DependencyProperty ZoomFactorProperty { get; } =
			DependencyProperty.Register("ZoomFactor", typeof(float), typeof(ScrollViewer), new FrameworkPropertyMetadata(1f));
		#endregion

		#region HorizontalSnapPointsType (DP)
		public SnapPointsType HorizontalSnapPointsType
		{
			get => (SnapPointsType)this.GetValue(HorizontalSnapPointsTypeProperty);
			set => this.SetValue(HorizontalSnapPointsTypeProperty, value);
		}

		public static DependencyProperty HorizontalSnapPointsTypeProperty { get; } =
			DependencyProperty.Register(
				"HorizontalSnapPointsType",
				typeof(SnapPointsType),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(SnapPointsType)));
		#endregion

		#region HorizontalSnapPointsAlignment (DP)
		public SnapPointsAlignment HorizontalSnapPointsAlignment
		{
			get => (SnapPointsAlignment)this.GetValue(HorizontalSnapPointsAlignmentProperty);
			set => this.SetValue(HorizontalSnapPointsAlignmentProperty, value);
		}

		public static DependencyProperty HorizontalSnapPointsAlignmentProperty { get; } =
			DependencyProperty.Register(
				"HorizontalSnapPointsAlignment",
				typeof(SnapPointsAlignment),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(SnapPointsAlignment)));
		#endregion

		#region VerticalSnapPointsType (DP)
		public SnapPointsType VerticalSnapPointsType
		{
			get => (SnapPointsType)this.GetValue(VerticalSnapPointsTypeProperty);
			set => this.SetValue(VerticalSnapPointsTypeProperty, value);
		}

		public static DependencyProperty VerticalSnapPointsTypeProperty { get; } =
			DependencyProperty.Register(
				"VerticalSnapPointsType",
				typeof(SnapPointsType),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(SnapPointsType)));
		#endregion

		#region VerticalSnapPointsAlignment (DP)
		public SnapPointsAlignment VerticalSnapPointsAlignment
		{
			get => (SnapPointsAlignment)this.GetValue(VerticalSnapPointsAlignmentProperty);
			set => this.SetValue(VerticalSnapPointsAlignmentProperty, value);
		}

		public static DependencyProperty VerticalSnapPointsAlignmentProperty { get; } =
			DependencyProperty.Register(
				"VerticalSnapPointsAlignment",
				typeof(SnapPointsAlignment),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(SnapPointsAlignment)));
		#endregion

		#region ExtentHeight (DP - readonly)
		public double ExtentHeight
		{
			get => (double)GetValue(ExtentHeightProperty);
			private set => SetValue(ExtentHeightProperty, value);
		}

		public static DependencyProperty ExtentHeightProperty { get; } =
			DependencyProperty.Register(
				"ExtentHeight",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));
		#endregion

		#region ExtentWidth (DP - readonly)
		public double ExtentWidth
		{
			get => (double)GetValue(ExtentWidthProperty);
			private set => SetValue(ExtentWidthProperty, value);
		}

		public static DependencyProperty ExtentWidthProperty { get; } =
			DependencyProperty.Register(
				"ExtentWidth",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));
		#endregion

		#region ViewportHeight (DP - readonly)
		public double ViewportHeight
		{
			get => (double)GetValue(ViewportHeightProperty);
			private set => SetValue(ViewportHeightProperty, value);
		}

		public static DependencyProperty ViewportHeightProperty { get; } =
			DependencyProperty.Register(
				"ViewportHeight",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));
		#endregion

		#region ViewportWidth (DP - readonly)
		public double ViewportWidth
		{
			get => (double)GetValue(ViewportWidthProperty);
			private set => SetValue(ViewportWidthProperty, value);
		}

		public static DependencyProperty ViewportWidthProperty { get; } =
			DependencyProperty.Register(
				"ViewportWidth",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));
		#endregion

		#region ComputedHorizontalScrollBarVisibility (DP - readonly)
		public static DependencyProperty ComputedHorizontalScrollBarVisibilityProperty { get; } =
			DependencyProperty.Register(
				"ComputedHorizontalScrollBarVisibility",
				typeof(Visibility),
				typeof(ScrollViewer),
				new PropertyMetadata(Visibility.Collapsed)); // This has to be collapsed by default to allow deferred loading of the template

		public Visibility ComputedHorizontalScrollBarVisibility
		{
			get => (Visibility)GetValue(ComputedHorizontalScrollBarVisibilityProperty);
			private set => SetValue(ComputedHorizontalScrollBarVisibilityProperty, value);
		}
		#endregion

		#region ComputedVerticalScrollBarVisibilityProperty (DP - readonly)
		public static DependencyProperty ComputedVerticalScrollBarVisibilityProperty { get; } =
			DependencyProperty.Register(
				"ComputedVerticalScrollBarVisibility",
				typeof(Visibility),
				typeof(ScrollViewer),
				new PropertyMetadata(Visibility.Collapsed)); // This has to be collapsed by default to allow deferred loading of the template

		public Visibility ComputedVerticalScrollBarVisibility
		{
			get => (Visibility)GetValue(ComputedVerticalScrollBarVisibilityProperty);
			private set => SetValue(ComputedVerticalScrollBarVisibilityProperty, value);
		}
		#endregion

		#region ScrollableHeight (DP - readonly)
		public double ScrollableHeight
		{
			get => (double)GetValue(ScrollableHeightProperty);
			private set => SetValue(ScrollableHeightProperty, value);
		}

		public static DependencyProperty ScrollableHeightProperty { get; } =
			DependencyProperty.Register(
				"ScrollableHeight",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));
		#endregion

		#region ScrollableWidth (DP - readonly)
		public double ScrollableWidth
		{
			get => (double)GetValue(ScrollableWidthProperty);
			private set => SetValue(ScrollableWidthProperty, value);
		}

		public static DependencyProperty ScrollableWidthProperty { get; } =
			DependencyProperty.Register(
				"ScrollableWidth",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));
		#endregion

		#region VerticalOffset (DP - readonly)
		public double VerticalOffset
		{
			get => (double)GetValue(VerticalOffsetProperty);
			private set => SetValue(VerticalOffsetProperty, value);
		}

		public static DependencyProperty VerticalOffsetProperty =
			DependencyProperty.Register(
				"VerticalOffset",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					defaultValue: (double)0,
					propertyChangedCallback: null
				)
			);

		#endregion

		#region HorizontalOffset (DP - readonly)
		public double HorizontalOffset
		{
			get => (double)GetValue(HorizontalOffsetProperty);
			private set => SetValue(HorizontalOffsetProperty, value);
		}

		public static DependencyProperty HorizontalOffsetProperty =
			DependencyProperty.Register(
				"HorizontalOffset",
				typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					defaultValue: (double)0,
					propertyChangedCallback: null
				)
			);
		#endregion

		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

#pragma warning disable 649 // unused member for Unit tests
		private _ScrollContentPresenter? _presenter;
#pragma warning restore 649 // unused member for Unit tests

		/// <summary>
		/// Gets the size of the Viewport used in the **CURRENT** (cf. remarks) or last measure
		/// </summary>
		/// <remarks>Unlike the LayoutInformation.GetAvailableSize(), this property is set **BEFORE** measuring the children of the ScrollViewer</remarks>
		internal Size ViewportMeasureSize { get; private set; }

		/// <summary>
		/// Gets the size of the Viewport used in the **CURRENT** (cf. remarks) or last arrange
		/// </summary>
		/// <remarks>Unlike the LayoutInformation.GetLayoutSlot(), this property is set **BEFORE** arranging the children of the ScrollViewer</remarks>
		internal Size ViewportArrangeSize { get; private set; }

		// Note for implementers: Search for SharedHelpers.IsRS5OrHigher() in ItemsRepeaterScrollHost.cs
		// => This should be re-enabled AND this class also gives the base implementation for the anchoring
		[global::Uno.NotImplemented]
		public UIElement? CurrentAnchor => null;

		/// <summary>
		/// Cached value of <see cref="Uno.UI.Xaml.Controls.ScrollViewer.UpdatesModeProperty"/>,
		/// in order to not access the DP on each scroll (perf considerations)
		/// </summary>
		internal Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode UpdatesMode { get; set; }

		/// <summary>
		/// If this flag is enabled, the ScrollViewer will report offsets less than 0 and greater than <see cref="ScrollableHeight"/> when
		/// 'overscrolling' on iOS. By default this is false, matching Windows behaviour.
		/// </summary>
		[UnoOnly]
		public bool ShouldReportNegativeOffsets { get; set; } = false;

		/// <summary>
		/// Determines if the vertical scrolling is allowed or not.
		/// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		/// </summary>
		internal bool ComputedIsHorizontalScrollEnabled { get; private set; } = false;

		/// <summary>
		/// Determines if the vertical scrolling is allowed or not.
		/// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		/// </summary>
		internal bool ComputedIsVerticalScrollEnabled { get; private set; } = false;

		protected override Size MeasureOverride(Size availableSize)
		{
			ViewportMeasureSize = availableSize;

			return base.MeasureOverride(availableSize);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ViewportArrangeSize = finalSize;

			var arrangeSize = base.ArrangeOverride(finalSize);
			TrimOverscroll(Orientation.Horizontal);
			TrimOverscroll(Orientation.Vertical);
			return arrangeSize;
		}

		partial void TrimOverscroll(Orientation orientation);

		internal override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();

			UpdateDimensionProperties();
			UpdateZoomedContentAlignment();
		}

#if __IOS__
		internal
#else
		private
#endif
			void UpdateDimensionProperties()
		{
			if (this.Log().IsEnabled(LogLevel.Debug)
				&& (ActualHeight != ViewportHeight || ActualWidth != ViewportWidth)
			)
			{
				this.Log().LogDebug($"ScrollViewer setting ViewportHeight={ActualHeight}, ViewportWidth={ActualWidth}");
			}

			if (ActualWidth == 0 || ActualHeight == 0)
			{
				// Do not update properties if we don't have any valid size yet.
				// This is useful essentially for the first size changed on the Content,
				// where it already have its final size while the SV doesn't.
				// This would cause a Scrollable<Width|Height> greater than 0,
				// which will cause the materialization of the managed scrollbar
				// which might not be needed after next layout pass.
				return;
			}

			// The dimensions of the presenter (which are often but not always the same as the ScrollViewer) determine the viewport size
			ViewportHeight = (_presenter as IFrameworkElement)?.ActualHeight ?? ActualHeight;
			ViewportWidth = (_presenter as IFrameworkElement)?.ActualWidth ?? ActualWidth;

			if(Content is FrameworkElement fe)
			{
				var explicitHeight = fe.Height;
				if (explicitHeight.IsFinite())
				{
					ExtentHeight = explicitHeight;
				}
				else
				{
					var canUseActualHeightAsExtent =
						fe.ActualHeight > 0 &&
						fe.VerticalAlignment == VerticalAlignment.Stretch;

					ExtentHeight = canUseActualHeightAsExtent ? fe.ActualHeight : fe.DesiredSize.Height;
				}

				var explicitWidth = fe.Width;
				if (explicitWidth.IsFinite())
				{
					ExtentWidth = explicitWidth;
				}
				else
				{
					var canUseActualWidthAsExtent =
						fe.ActualWidth > 0 &&
						fe.HorizontalAlignment == HorizontalAlignment.Stretch;

					ExtentWidth = canUseActualWidthAsExtent ? fe.ActualWidth : fe.DesiredSize.Width;
				}
			}
			else
			{
				// TODO: fallback on native values (.ContentSize on iOS, for example)
				ExtentHeight = 0;
				ExtentWidth = 0;
			}

			ScrollableHeight = Math.Max(ExtentHeight - ViewportHeight, 0);
			ScrollableWidth = Math.Max(ExtentWidth - ViewportWidth, 0);

			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);
		}

		private void UpdateComputedVerticalScrollability(bool invalidate)
		{
			var scrollable = ScrollableHeight;
			var visibility = VerticalScrollBarVisibility;
			var mode = VerticalScrollMode;

			var allowed = ComputeIsScrollAllowed(visibility, mode);
			var computedVisibility = ComputeScrollBarVisibility(scrollable, visibility);
			var computedEnabled = ComputeIsScrollEnabled(scrollable, visibility, mode);

			if (_presenter is null)
			{
				ComputedVerticalScrollBarVisibility = computedVisibility; // Retro-compatibility, probably useless
				ComputedIsVerticalScrollEnabled = computedEnabled; // Retro-compatibility, probably useless
				return; // Control not ready yet
			}
			_presenter.CanVerticallyScroll = allowed;

			// Note: We materialize the ScrollBar BEFORE setting the ComputedVisibility in order to avoid
			//		 auto materialization due to databound visibility.
			//		 This would cause materialization of both Vertical and Horizontal templates of the ScrollBar
			//		 as we wouldn't have set the IsFixedOrientation flag yet.
			MaterializeVerticalScrollBarIfNeeded(computedVisibility);

			ComputedVerticalScrollBarVisibility = computedVisibility;
			ComputedIsVerticalScrollEnabled = computedEnabled;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.VerticalScrollBarVisibility = ComputeNativeScrollBarVisibility(scrollable, visibility, mode, _verticalScrollbar);
			if (invalidate && _verticalScrollbar is null)
			{
				InvalidateMeasure(); // Useless for managed ScrollBar, it will invalidate itself if needed.
			}
#endif
		}

		private void UpdateComputedHorizontalScrollability(bool invalidate)
		{
			var scrollable = ScrollableWidth;
			var visibility = HorizontalScrollBarVisibility;
			var mode = HorizontalScrollMode;

			var allowed = ComputeIsScrollAllowed(visibility, mode);
			var computedVisibility = ComputeScrollBarVisibility(scrollable, visibility);
			var computedEnabled = ComputeIsScrollEnabled(scrollable, visibility, mode);

			if (_presenter is null)
			{
				ComputedHorizontalScrollBarVisibility = computedVisibility; // Retro-compatibility, probably useless
				ComputedIsHorizontalScrollEnabled = computedEnabled; // Retro-compatibility, probably useless
				return; // Control not ready yet
			}
			_presenter.CanHorizontallyScroll = allowed;

			// Note: We materialize the ScrollBar BEFORE setting the ComputedVisibility in order to avoid
			//		 auto materialization due to databound visibility.
			//		 This would cause materialization of both Vertical and Horizontal templates of the ScrollBar
			//		 as we wouldn't have set the IsFixedOrientation flag yet.
			MaterializeHorizontalScrollBarIfNeeded(computedVisibility);

			ComputedHorizontalScrollBarVisibility = computedVisibility;
			ComputedIsHorizontalScrollEnabled = computedEnabled;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.HorizontalScrollBarVisibility = ComputeNativeScrollBarVisibility(scrollable, visibility, mode, _horizontalScrollbar);
			if (invalidate && _horizontalScrollbar is null)
			{
				InvalidateMeasure(); // Useless for managed ScrollBar, it will invalidate itself if needed.
			}
#endif
		}

		/// <summary>
		/// Determines if the scroll has been allowed on that scroll viewer, not matter if scroll is possible or not due to the size of the content.
		/// </summary>
		private static bool ComputeIsScrollAllowed(ScrollBarVisibility visibility, ScrollMode mode)
			=> visibility != ScrollBarVisibility.Disabled
				&& mode != ScrollMode.Disabled;

		private static Visibility ComputeScrollBarVisibility(double scrollable, ScrollBarVisibility visibility)
		{
			// Note: The ScrollMode DOES NOT impact the visibility of the ScrollBar, but just it's hit testability!

			switch (visibility)
			{
				case ScrollBarVisibility.Auto when scrollable > 0:
				case ScrollBarVisibility.Visible:
					return Visibility.Visible;

				default: // i.e.: Auto when scrollable <= 0; Hidden; Disabled;
					return Visibility.Collapsed;
			}
		}

		// Determines if the scrolling is enabled or not.
		// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		private static bool ComputeIsScrollEnabled(double scrollable, ScrollBarVisibility visibility, ScrollMode mode)
			=> scrollable > 0
				&& visibility != ScrollBarVisibility.Disabled
				&& mode != ScrollMode.Disabled;

		private ScrollBarVisibility ComputeNativeScrollBarVisibility(double scrollable, ScrollBarVisibility visibility, ScrollMode mode, ScrollBar? managedScrollbar)
			=> (scrollable, visibility, mode, managedScrollbar) switch
			{
				(_, _, ScrollMode.Disabled, _) => ScrollBarVisibility.Disabled,
				(0, ScrollBarVisibility.Auto, _, null) => ScrollBarVisibility.Hidden, // If scrollable is 0, the managed scrollbar won't be realized, we prefer to hide the native one until we are sure!
				(_, _, _, null) when Uno.UI.Xaml.Controls.ScrollViewer.GetShouldFallBackToNativeScrollBars(this) => visibility,
				(_, ScrollBarVisibility.Disabled, _, _) => ScrollBarVisibility.Disabled,
				_ => ScrollBarVisibility.Hidden // If a managed scroll bar was set in the template, native scroll bar has to stay Hidden
			};

		/// <summary>
		/// Sets the content of the ScrollViewer
		/// </summary>
		/// <param name="view"></param>
		/// <remarks>Used in the context of member initialization</remarks>
		public
#if !UNO_REFERENCE_API && !__MACOS__ && !NET461
			new
#endif
			void Add(View view)
		{
			Content = view;
		}

		protected override void OnApplyTemplate()
		{
			// Cleanup previous template
			DetachScrollBars();

			base.OnApplyTemplate();

			var scpTemplatePart = GetTemplateChild(Parts.WinUI3.Scroller) ?? GetTemplateChild(Parts.Uwp.ScrollContentPresenter);
			_presenter = scpTemplatePart as _ScrollContentPresenter;

			_isTemplateApplied = _presenter != null;

			// Load new template
			_verticalScrollbar = null;
			_isVerticalScrollBarMaterialized = false;
			_horizontalScrollbar = null;
			_isHorizontalScrollBarMaterialized = false;

#if __IOS__ || __ANDROID__
			if (scpTemplatePart is ScrollContentPresenter scp)
			{
				// For Android/iOS/MacOS, ensure that the ScrollContentPresenter contains a native scroll viewer,
				// which will handle the actual scrolling
				var nativeSCP = new NativeScrollContentPresenter(this);
				scp.Content = nativeSCP;
				_presenter = nativeSCP;
			}
#endif

			if (scpTemplatePart is ScrollContentPresenter presenter)
			{
				presenter.ScrollOwner = this;
			}

			// We update the scrollability properties here in order to make sure to set the right scrollbar visibility
			// on the _presenter as soon as possible
			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);

			ApplyScrollContentPresenterContent(Content);

			OnApplyTemplatePartial();

			// Apply correct initial zoom settings
			OnZoomModeChanged(ZoomMode);

			OnBringIntoViewOnFocusChangeChangedPartial(BringIntoViewOnFocusChange);

			PrepareScrollIndicator();
		}

		partial void OnApplyTemplatePartial();

		void IFrameworkTemplatePoolAware.OnTemplateRecycled()
		{
			if (VerticalOffset != 0 || HorizontalOffset != 0 || ZoomFactor != 1)
			{
				ChangeView(
					horizontalOffset: 0,
					verticalOffset: 0,
					zoomFactor: 1,
					disableAnimation: true
				);
			}
		}

#region Content and TemplatedParent forwarding to the ScrollContentPresenter
		protected override void OnContentChanged(object oldValue, object newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			if (_presenter != null)
			{
				// remove the explicit templated parent propagation
				// for the lack of TemplatedParentScope support
				ClearContentTemplatedParent(oldValue);

				ApplyScrollContentPresenterContent(newValue);
			}

			UpdateSizeChangedSubscription();

			_snapPointsInfo = newValue as IScrollSnapPointsInfo;
		}

		private void ApplyScrollContentPresenterContent(object content)
		{
			// Stop the automatic propagation of the templated parent on the Content
			// This prevents issues when the a ScrollViewer is hosted in a control template
			// and its content is a ContentControl or ContentPresenter, which has a TemplateBinding
			// on the Content property. This can make the Content added twice in the visual tree.
			// cf. https://github.com/unoplatform/uno/issues/3762
			if (content is IDependencyObjectStoreProvider provider)
			{
				var contentTemplatedParent = provider.Store.GetValue(provider.Store.TemplatedParentProperty);
				if (contentTemplatedParent == null || contentTemplatedParent != TemplatedParent)
				{
					// Note: Even if the TemplatedParent is already null, we make sure to set it with the local precedence
					provider.Store.SetValue(provider.Store.TemplatedParentProperty, null, DependencyPropertyValuePrecedences.Local);
				}
			}

			// Then explicitly propagate the Content to the _presenter
			if (_presenter != null)
			{
				_presenter.Content = content as View;
			}

			// Propagate the ScrollViewer's own templated parent, instead of
			// the scrollviewer itself (through ScrollContentPresenter)
			SynchronizeContentTemplatedParent(TemplatedParent);
		}

		private void UpdateSizeChangedSubscription(bool isCleanupRequired = false)
		{
			// TODO HERE
			if (!isCleanupRequired
				&& Content is IFrameworkElement element)
			{
				_sizeChangedSubscription.Disposable = Disposable.Create(() => element.SizeChanged -= OnElementSizeChanged);
				element.SizeChanged += OnElementSizeChanged;
			}
			else
			{
				_sizeChangedSubscription.Disposable = null;
			}

			void OnElementSizeChanged(object sender, SizeChangedEventArgs args)
				=> UpdateDimensionProperties();
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			SynchronizeContentTemplatedParent(e.NewValue as DependencyObject);
		}

		private void SynchronizeContentTemplatedParent(DependencyObject? templatedParent)
		{
			if (Content is View && Content is IDependencyObjectStoreProvider provider)
			{
				provider.Store.SetValue(provider.Store.TemplatedParentProperty, templatedParent, DependencyPropertyValuePrecedences.Local);
			}
		}

		private void ClearContentTemplatedParent(object oldContent)
		{
			if (oldContent is IDependencyObjectStoreProvider provider)
			{
				provider.Store.ClearValue(provider.Store.TemplatedParentProperty, DependencyPropertyValuePrecedences.Local);
			}
		}
#endregion

#region Managed scroll bars support
		private bool _isTemplateApplied;
		private ScrollBar? _verticalScrollbar;
		private ScrollBar? _horizontalScrollbar;
		private bool _isVerticalScrollBarMaterialized;
		private bool _isHorizontalScrollBarMaterialized;

		private void MaterializeVerticalScrollBarIfNeeded(Visibility computedVisibility)
		{
			if (!_isTemplateApplied || _isVerticalScrollBarMaterialized || computedVisibility != Visibility.Visible)
			{
				return;
			}

			using (ScrollBar.MaterializingFixed(Orientation.Vertical))
			{
				_verticalScrollbar = (GetTemplateChild(Parts.WinUI3.VerticalScrollBar) ?? GetTemplateChild(Parts.Uwp.VerticalScrollBar)) as ScrollBar;
				_isVerticalScrollBarMaterialized = true;
			}

			if (_verticalScrollbar is null)
			{
				return;
			}

			_verticalScrollbar.IsFixedOrientation = true; // Redundant with ScrollBar.MaterializingFixed, but twice is safer
			DetachScrollBars();
			AttachScrollBars();
		}

		private void MaterializeHorizontalScrollBarIfNeeded(Visibility computedVisibility)
		{
			if (!_isTemplateApplied || _isHorizontalScrollBarMaterialized || computedVisibility != Visibility.Visible)
			{
				return;
			}

			using (ScrollBar.MaterializingFixed(Orientation.Horizontal))
			{
				_horizontalScrollbar = (GetTemplateChild(Parts.WinUI3.HorizontalScrollBar) ?? GetTemplateChild(Parts.Uwp.HorizontalScrollBar)) as ScrollBar;
				_isHorizontalScrollBarMaterialized = true;
			}

			if (_horizontalScrollbar is null)
			{
				return;
			}

			_horizontalScrollbar.IsFixedOrientation = true; // Redundant with ScrollBar.MaterializingFixed, but twice is safer
			DetachScrollBars();
			AttachScrollBars();
		}

		private static void DetachScrollBars(object sender, RoutedEventArgs e) // OnUnloaded
			=> (sender as ScrollViewer)?.DetachScrollBars();

		private void DetachScrollBars()
		{
			if (_verticalScrollbar != null)
			{
				_verticalScrollbar.Scroll -= OnVerticalScrollBarScrolled;
				_verticalScrollbar.PointerEntered -= ShowScrollBarSeparator;
				_verticalScrollbar.PointerExited -= HideScrollBarSeparator;
			}

			if (_horizontalScrollbar != null)
			{
				_horizontalScrollbar.Scroll -= OnHorizontalScrollBarScrolled;
				_horizontalScrollbar.PointerEntered -= ShowScrollBarSeparator;
				_horizontalScrollbar.PointerExited -= HideScrollBarSeparator;
			}

			PointerMoved -= ShowScrollIndicator;
		}

		private static void AttachScrollBars(object sender, RoutedEventArgs e) // OnLoaded
		{
			if (sender is ScrollViewer sv)
			{
				sv.DetachScrollBars(); // Avoid double subscribe due to OnApplyTemplate
				sv.AttachScrollBars();
			}
		}

		private void AttachScrollBars()
		{
			bool hasManagedVerticalScrollBar;
			if (_verticalScrollbar is { } vertical)
			{
				vertical.Scroll += OnVerticalScrollBarScrolled;
				hasManagedVerticalScrollBar = true;

				PointerMoved += ShowScrollIndicator;
			}
			else
			{
				hasManagedVerticalScrollBar = false;
			}

			bool hasManagedHorizontalScrollBar;
			if (_horizontalScrollbar is { } horizontal)
			{
				horizontal.Scroll += OnHorizontalScrollBarScrolled;
				hasManagedHorizontalScrollBar = true;

				if (!hasManagedVerticalScrollBar)
				{
					PointerMoved += ShowScrollIndicator;
				}
			}
			else
			{
				hasManagedHorizontalScrollBar = false;
			}

			if (hasManagedVerticalScrollBar && hasManagedHorizontalScrollBar)
			{
				_verticalScrollbar!.PointerEntered += ShowScrollBarSeparator;
				_horizontalScrollbar!.PointerEntered += ShowScrollBarSeparator;
				_verticalScrollbar!.PointerExited += HideScrollBarSeparator;
				_horizontalScrollbar!.PointerExited += HideScrollBarSeparator;
			}
		}

		private void OnVerticalScrollBarScrolled(object sender, ScrollEventArgs e)
		{
			// We animate only if the user clicked in the scroll bar, and disable otherwise
			// (especially, we disable animation when dragging the thumb)
			var immediate = e.ScrollEventType switch
			{
				ScrollEventType.LargeIncrement => false,
				ScrollEventType.LargeDecrement => false,
				ScrollEventType.SmallIncrement => false,
				ScrollEventType.SmallDecrement => false,
				_ => true
			};

			ChangeViewCore(
				horizontalOffset: null,
				verticalOffset: e.NewValue,
				zoomFactor: null,
				disableAnimation: immediate,
				shouldSnap: true);
		}

		private void OnHorizontalScrollBarScrolled(object sender, ScrollEventArgs e)
		{
			// We animate only if the user clicked in the scroll bar, and disable otherwise
			// (especially, we disable animation when dragging the thumb)
			var immediate = e.ScrollEventType switch
			{
				ScrollEventType.LargeIncrement => false,
				ScrollEventType.LargeDecrement => false,
				ScrollEventType.SmallIncrement => false,
				ScrollEventType.SmallDecrement => false,
				_ => true
			};

			ChangeViewCore(
				horizontalOffset: e.NewValue,
				verticalOffset: null,
				zoomFactor: null,
				disableAnimation: immediate,
				shouldSnap: true);
		}
#endregion

		// Presenter to Control, i.e. OnPresenterScrolled
		internal void OnScrollInternal(double horizontalOffset, double verticalOffset, bool isIntermediate)
		{
			var h = horizontalOffset == HorizontalOffset ? null : (double?)horizontalOffset;
			var v = verticalOffset == VerticalOffset ? null : (double?)verticalOffset;

			_pendingHorizontalOffset = horizontalOffset;
			_pendingVerticalOffset = verticalOffset;

			if (isIntermediate && UpdatesMode != Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous)
			{
				RequestUpdate();
			}
			else
			{
				Update(isIntermediate);

				if(!isIntermediate)
				{
					if (HorizontalSnapPointsType != SnapPointsType.None
						|| VerticalSnapPointsType != SnapPointsType.None)
					{
						if(_snapPointsTimer == null)
						{
							_snapPointsTimer = Windows.System.DispatcherQueue.GetForCurrentThread().CreateTimer();
							_snapPointsTimer.IsRepeating = false;
							_snapPointsTimer.Interval = TimeSpan.FromMilliseconds(250);
							_snapPointsTimer.Tick += (snd, evt) => DelayedMoveToSnapPoint();
						}

						_horizontalOffsetForSnapPoints = h ?? horizontalOffset;
						_verticalOffsetForSnapPoints = v ?? verticalOffset;

						_snapPointsTimer.Start();
					}
				}
			}
		}

		private DispatcherQueueTimer? _snapPointsTimer;
		private double? _horizontalOffsetForSnapPoints;
		private double? _verticalOffsetForSnapPoints;

		private void DelayedMoveToSnapPoint()
		{
			var h = _horizontalOffsetForSnapPoints;
			var v = _verticalOffsetForSnapPoints;

			AdjustOffsetsForSnapPoints(ref h, ref v, ZoomFactor);

			if ((h == null || h == HorizontalOffset) && (v == null || v == VerticalOffset))
			{
				return; // already on a snap point
			}

			ChangeViewCore(
				horizontalOffset: h,
				verticalOffset: v,
				zoomFactor: null,
				disableAnimation: false,
				shouldSnap: false);

			_horizontalOffsetForSnapPoints = null;
			_verticalOffsetForSnapPoints = null;
		}

		// Presenter to Control, i.e. OnPresenterZoomed
		internal void OnZoomInternal(float zoomFactor)
		{
			ZoomFactor = zoomFactor;

			// Note: We should also defer the intermediate zoom changes
			Update(isIntermediate: false);

			UpdateZoomedContentAlignment();
		}

		private bool _hasPendingUpdate;
		private double _pendingHorizontalOffset;
		private double _pendingVerticalOffset;

		private void RequestUpdate()
		{
			if (_hasPendingUpdate)
			{
				return;
			}

			Dispatcher.RunIdleAsync(e =>
			{
				if (_hasPendingUpdate)
				{
					Update(isIntermediate: true);
				}
			});
			_hasPendingUpdate = true;
		}

		private void Update(bool isIntermediate)
		{
			_hasPendingUpdate = false;

			HorizontalOffset = _pendingHorizontalOffset;
			VerticalOffset = _pendingVerticalOffset;

			UpdatePartial(isIntermediate);

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// Effective viewport support
			ScrollOffsets = new Point(_pendingHorizontalOffset, _pendingVerticalOffset);
			InvalidateViewport();
#endif

			ViewChanged?.Invoke(this, new ScrollViewerViewChangedEventArgs { IsIntermediate = isIntermediate });
		}

		partial void UpdatePartial(bool isIntermediate);

		/// <summary>
		/// Causes the ScrollViewer to load a new view into the viewport using the specified offsets and zoom factor, and optionally disables scrolling animation.
		/// </summary>
		/// <param name="horizontalOffset">A value between 0 and ScrollableWidth that specifies the distance the content should be scrolled horizontally.</param>
		/// <param name="verticalOffset">A value between 0 and ScrollableHeight that specifies the distance the content should be scrolled vertically.</param>
		/// <param name="zoomFactor">A value between MinZoomFactor and MaxZoomFactor that specifies the required target ZoomFactor.</param>
		/// <returns>true if the view is changed; otherwise, false.</returns>
		public bool ChangeView(double? horizontalOffset, double? verticalOffset, float? zoomFactor)
			=> ChangeView(horizontalOffset, verticalOffset, zoomFactor, false);

		/// <summary>
		/// Causes the ScrollViewer to load a new view into the viewport using the specified offsets and zoom factor, and optionally disables scrolling animation.
		/// </summary>
		/// <param name="horizontalOffset">A value between 0 and ScrollableWidth that specifies the distance the content should be scrolled horizontally.</param>
		/// <param name="verticalOffset">A value between 0 and ScrollableHeight that specifies the distance the content should be scrolled vertically.</param>
		/// <param name="zoomFactor">A value between MinZoomFactor and MaxZoomFactor that specifies the required target ZoomFactor.</param>
		/// <param name="disableAnimation">true to disable zoom/pan animations while changing the view; otherwise, false. The default is false.</param>
		/// <returns>true if the view is changed; otherwise, false.</returns>
		public bool ChangeView(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"ChangeView(horizontalOffset={horizontalOffset}, verticalOffset={verticalOffset}, zoomFactor={zoomFactor}, disableAnimation={disableAnimation})");
			}

			if(horizontalOffset == null && verticalOffset == null && zoomFactor == null)
			{
				return true; // nothing to do
			}

			var verticalOffsetChanged = verticalOffset != null && verticalOffset != VerticalOffset;
			var horizontalOffsetChanged = horizontalOffset != null && horizontalOffset != HorizontalOffset;
			var zoomFactorChanged = zoomFactor != null && zoomFactor != ZoomFactor;

			if (verticalOffsetChanged || horizontalOffsetChanged || zoomFactorChanged)
			{
				return ChangeViewCore(
					horizontalOffset,
					verticalOffset,
					zoomFactor,
					disableAnimation,
					shouldSnap: true);
			}
			else
			{
				return false;
			}
		}

		private bool ChangeViewCore(
			double? horizontalOffset,
			double? verticalOffset,
			float? zoomFactor,
			bool disableAnimation,
			bool shouldSnap)
		{
			if (horizontalOffset is null && verticalOffset is null && zoomFactor is null)
			{
				return false;
			}

			if (shouldSnap)
			{
				AdjustOffsetsForSnapPoints(ref horizontalOffset, ref verticalOffset, zoomFactor);
			}

			return ChangeViewNative(horizontalOffset, verticalOffset, zoomFactor, disableAnimation);
		}

		#region Scroll indicators visual states (Managed scroll bars only)
		private static readonly TimeSpan _indicatorResetDelay = FeatureConfiguration.ScrollViewer.DefaultAutoHideDelay ?? TimeSpan.FromSeconds(4);
		private static readonly bool _indicatorResetDisabled = _indicatorResetDelay == TimeSpan.MaxValue;
		private DispatcherQueueTimer? _indicatorResetTimer;
		private string? _indicatorState;

		private void PrepareScrollIndicator() // OnApplyTemplate
		{
			if (_indicatorResetDisabled)
			{
				ShowScrollIndicator(PointerDeviceType.Mouse, forced: true);
			}
			else
			{
				ResetScrollIndicator(forced: true);
			}
		}

		private static void ShowScrollIndicator(object sender, PointerRoutedEventArgs e) // OnPointerMove
			=> (sender as ScrollViewer)?.ShowScrollIndicator(e.Pointer.PointerDeviceType);

		private void ShowScrollIndicator(PointerDeviceType type, bool forced = false)
		{
			if (!forced && !ComputedIsVerticalScrollEnabled && !ComputedIsHorizontalScrollEnabled)
			{
				return;
			}

			var indicatorState = type switch
			{
				PointerDeviceType.Touch => VisualStates.ScrollingIndicator.Touch,
				_ => VisualStates.ScrollingIndicator.Mouse // Mouse and pen are using the MouseIndicator
			};
			if (_indicatorState != indicatorState) // Avoid costly GoToState if useless
			{
				VisualStateManager.GoToState(this, indicatorState, true);
				_indicatorState = indicatorState;
			}

			if (_indicatorResetDisabled)
			{
				return;
			}

			// Automatically hide the scroll indicator after a delay without any interaction
			if (_indicatorResetTimer == null)
			{
				var weakRef = WeakReferencePool.RentSelfWeakReference(this);
				_indicatorResetTimer = new DispatcherQueueTimer
				{
					Interval = _indicatorResetDelay,
					IsRepeating = false
				};
				_indicatorResetTimer.Tick += (snd, e) => (weakRef.Target as ScrollViewer)?.ResetScrollIndicator();
			}
			_indicatorResetTimer.Start(); // Starts or restarts the reset timer
		}

		private static void ResetScrollIndicator(object sender, RoutedEventArgs _) // OnUnloaded
			=> (sender as ScrollViewer)?.ResetScrollIndicator(forced: true);

		private void ResetScrollIndicator(bool forced = false)
		{
			if (_indicatorResetDisabled)
			{
				return;
			}

			_indicatorResetTimer?.Stop();

			if (!forced && ((_horizontalScrollbar?.IsPointerOver ?? false) || (_verticalScrollbar?.IsPointerOver ?? false)))
			{
				// We don't auto hide the indicators if the pointer is over it!
				// Note: the pointer has to move over this ScrollViewer to exit the ScrollBar, so we will restart the reset timer!
				return;
			}

			VisualStateManager.GoToState(this, VisualStates.ScrollingIndicator.None, true);
			_indicatorState = VisualStates.ScrollingIndicator.None;

			VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.Collapsed, true);
		}

		private void ShowScrollBarSeparator(object sender, PointerRoutedEventArgs e) // ScrollBar.OnPointerEntered
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				return; // The separator is needed only for the MouseIndicator (Mouse and Pen)
			}

			if (IsAnimationEnabled || !VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.ExpandedWithoutAnimation, true))
			{
				VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.Expanded, true);
			}
		}

		private void HideScrollBarSeparator(object sender, PointerRoutedEventArgs e) // ScrollBar.OnPointerExited
		{
			if (IsAnimationEnabled || !VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.CollapsedWithoutAnimation, true))
			{
				VisualStateManager.GoToState(this, VisualStates.ScrollBarsSeparator.Collapsed, true);
			}
		}
		#endregion
	}
}
