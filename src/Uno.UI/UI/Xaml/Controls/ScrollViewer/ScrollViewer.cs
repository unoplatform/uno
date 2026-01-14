#if IS_UNIT_TESTS
#pragma warning disable CS0067
#endif

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Windows.Foundation.Metadata;
using Uno.UI.Xaml.Core;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __APPLE_UIKIT__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

#if UNO_HAS_MANAGED_SCROLL_PRESENTER
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.ScrollContentPresenter;
#else
using _ScrollContentPresenter = Microsoft.UI.Xaml.Controls.IScrollContentPresenter;
#endif

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Media;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer : ContentControl, IFrameworkTemplatePoolAware
	{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
		private bool m_isInConstantVelocityPan;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

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

				// On WinUI3 visuals states are prefixed with "ScrollBar***s***" (with a trailing 's')
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

		internal event SizeChangedEventHandler? ExtentSizeChanged;

		public ScrollViewer()
		{
			DefaultStyleKey = typeof(ScrollViewer);

			UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewer.GetUpdatesMode(this);
			InitializePartial();
		}

		partial void InitializePartial();

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			EnsureAttachScrollBars();

			OnLoadedPartial();
		}

		private partial void OnLoadedPartial();

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			DetachScrollBars();
			ResetScrollIndicator();

			OnUnloadedPartial();
		}
		private partial void OnUnloadedPartial();

		protected override AutomationPeer OnCreateAutomationPeer() => new ScrollViewerAutomationPeer(this);


		#region -- Common DP callbacks --
		private static PropertyChangedCallback OnHorizontalScrollabilityPropertyChanged = (obj, _)
			=> (obj as ScrollViewer)?.UpdateComputedHorizontalScrollability(invalidate: true);
		private static PropertyChangedCallback OnVerticalScrollabilityPropertyChanged = (obj, _)
			=> (obj as ScrollViewer)?.UpdateComputedVerticalScrollability(invalidate: true);
		#endregion

		#region HorizontalScrollBarVisibility (Attached DP)
		public static ScrollBarVisibility GetHorizontalScrollBarVisibility(DependencyObject element)
			=> (ScrollBarVisibility)element.GetValue(HorizontalScrollBarVisibilityProperty);

		public static void SetHorizontalScrollBarVisibility(DependencyObject element, ScrollBarVisibility horizontalScrollBarVisibility)
			=> element.SetValue(HorizontalScrollBarVisibilityProperty, horizontalScrollBarVisibility);

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => (ScrollBarVisibility)this.GetValue(HorizontalScrollBarVisibilityProperty);
			set => this.SetValue(HorizontalScrollBarVisibilityProperty, value);
		}

		public static DependencyProperty HorizontalScrollBarVisibilityProperty
		{
			[DynamicDependency(nameof(GetHorizontalScrollBarVisibility))]
			[DynamicDependency(nameof(SetHorizontalScrollBarVisibility))]
			get;
		} = DependencyProperty.RegisterAttached(
				"HorizontalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollBarVisibility.Disabled,
					propertyChangedCallback: OnHorizontalScrollabilityPropertyChanged
				)
			);
		#endregion

		#region VerticalScrollBarVisibility (Attached DP)
		public static ScrollBarVisibility GetVerticalScrollBarVisibility(DependencyObject element)
			=> (ScrollBarVisibility)element.GetValue(VerticalScrollBarVisibilityProperty);

		public static void SetVerticalScrollBarVisibility(DependencyObject element, ScrollBarVisibility verticalScrollBarVisibility)
			=> element.SetValue(VerticalScrollBarVisibilityProperty, verticalScrollBarVisibility);

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get => (ScrollBarVisibility)this.GetValue(VerticalScrollBarVisibilityProperty);
			set => this.SetValue(VerticalScrollBarVisibilityProperty, value);
		}

		public static DependencyProperty VerticalScrollBarVisibilityProperty
		{
			[DynamicDependency(nameof(GetVerticalScrollBarVisibility))]
			[DynamicDependency(nameof(SetVerticalScrollBarVisibility))]
			get;
		} = DependencyProperty.RegisterAttached(
				"VerticalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollBarVisibility.Auto,
					propertyChangedCallback: OnVerticalScrollabilityPropertyChanged
				)
			);
		#endregion

		#region HorizontalScrollMode (Attached DP)
		public static ScrollMode GetHorizontalScrollMode(DependencyObject element)
			=> (ScrollMode)element.GetValue(HorizontalScrollModeProperty);

		public static void SetHorizontalScrollMode(DependencyObject element, ScrollMode horizontalScrollMode)
			=> element.SetValue(HorizontalScrollModeProperty, horizontalScrollMode);

		public ScrollMode HorizontalScrollMode
		{
			get => (ScrollMode)this.GetValue(HorizontalScrollModeProperty);
			set => this.SetValue(HorizontalScrollModeProperty, value);
		}

		public static DependencyProperty HorizontalScrollModeProperty
		{
			[DynamicDependency(nameof(GetHorizontalScrollMode))]
			[DynamicDependency(nameof(SetHorizontalScrollMode))]
			get;
		} = DependencyProperty.RegisterAttached(
				"HorizontalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnHorizontalScrollabilityPropertyChanged
				)
			);
		#endregion

		#region VerticalScrollMode (Attached DP)

		public static ScrollMode GetVerticalScrollMode(DependencyObject element)
			=> (ScrollMode)element.GetValue(VerticalScrollModeProperty);

		public static void SetVerticalScrollMode(DependencyObject element, ScrollMode verticalScrollMode)
			=> element.SetValue(VerticalScrollModeProperty, verticalScrollMode);

		public ScrollMode VerticalScrollMode
		{
			get => (ScrollMode)this.GetValue(VerticalScrollModeProperty);
			set => this.SetValue(VerticalScrollModeProperty, value);
		}

		// Using a DependencyProperty as the backing store for VerticalScrollMode.  This enables animation, styling, binding, etc...
		public static DependencyProperty VerticalScrollModeProperty
		{
			[DynamicDependency(nameof(GetVerticalScrollMode))]
			[DynamicDependency(nameof(SetVerticalScrollMode))]
			get;
		} = DependencyProperty.RegisterAttached(
				"VerticalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnVerticalScrollabilityPropertyChanged
				)
			);
		#endregion

		#region BringIntoViewOnFocusChange (Attached DP)
#if __APPLE_UIKIT__
		[global::Uno.NotImplemented]
#endif
		public static bool GetBringIntoViewOnFocusChange(global::Microsoft.UI.Xaml.DependencyObject element)
			=> (bool)element.GetValue(BringIntoViewOnFocusChangeProperty);

#if __APPLE_UIKIT__
		[global::Uno.NotImplemented]
#endif
		public static void SetBringIntoViewOnFocusChange(global::Microsoft.UI.Xaml.DependencyObject element, bool bringIntoViewOnFocusChange)
			=> element.SetValue(BringIntoViewOnFocusChangeProperty, bringIntoViewOnFocusChange);

#if __APPLE_UIKIT__
		[global::Uno.NotImplemented]
#endif
		public bool BringIntoViewOnFocusChange
		{
			get => (bool)GetValue(BringIntoViewOnFocusChangeProperty);
			set => SetValue(BringIntoViewOnFocusChangeProperty, value);
		}

		public static DependencyProperty BringIntoViewOnFocusChangeProperty
		{
			[DynamicDependency(nameof(GetBringIntoViewOnFocusChange))]
			[DynamicDependency(nameof(SetBringIntoViewOnFocusChange))]
			get;
		} = DependencyProperty.RegisterAttached(
				"BringIntoViewOnFocusChange",
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					true,
					propertyChangedCallback: OnBringIntoViewOnFocusChangeChanged));

		private static void OnBringIntoViewOnFocusChangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = dependencyObject as ScrollViewer;

			view?.OnBringIntoViewOnFocusChangeChangedPartial((bool)args.NewValue);
		}

		partial void OnBringIntoViewOnFocusChangeChangedPartial(bool newValue);
		#endregion

		#region ZoomMode (Attached DP)
		public static ZoomMode GetZoomMode(DependencyObject element)
			=> (ZoomMode)element.GetValue(ZoomModeProperty);

		public static void SetZoomMode(DependencyObject element, ZoomMode zoomMode)
			=> element.SetValue(ZoomModeProperty, zoomMode);

		public ZoomMode ZoomMode
		{
			get => (ZoomMode)GetValue(ZoomModeProperty);
			set => SetValue(ZoomModeProperty, value);
		}

		public static DependencyProperty ZoomModeProperty
		{
			[DynamicDependency(nameof(GetZoomMode))]
			[DynamicDependency(nameof(SetZoomMode))]
			get;
		} = DependencyProperty.RegisterAttached(
				"ZoomMode",
				typeof(ZoomMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ZoomMode.Disabled,
					propertyChangedCallback: (o, e) => ((ScrollViewer)o).OnZoomModeChanged((ZoomMode)e.NewValue)
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
				new FrameworkPropertyMetadata(Visibility.Visible));

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
				new FrameworkPropertyMetadata(Visibility.Visible));

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

		public static DependencyProperty VerticalOffsetProperty { get; } =
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

		public static DependencyProperty HorizontalOffsetProperty { get; } =
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
		/// Gets the ScrollContentPresenter resolved from the template.
		/// Be aware that on iOS and Android this might be only a wrapper onto the NativeScrollContentPresenter.
		/// </summary>
		/// <remarks>
		/// This is a temporary workaround until the NativeSCP knows its managed SCP and will most probably been removed in a near .
		/// Try to avoid usage of this property as much as possible!
		/// </remarks>
		internal ScrollContentPresenter? Presenter { get; private set; }

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
		internal bool ComputedIsHorizontalScrollEnabled { get; private set; }

		/// <summary>
		/// Determines if the vertical scrolling is allowed or not.
		/// Unlike the Visibility of the scroll bar, this will also applies to the mousewheel!
		/// </summary>
		internal bool ComputedIsVerticalScrollEnabled { get; private set; }

		internal double MinHorizontalOffset => 0;

		internal double MinVerticalOffset => 0;

		protected override Size MeasureOverride(Size availableSize)
		{
			ViewportMeasureSize = availableSize;

			var size = base.MeasureOverride(availableSize);

			return size;
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

		// TODO: Revisit if this can use SizeChanged += (_, _) => OnControlsBoundsChanged(); on all platforms.
#if UNO_HAS_ENHANCED_LIFECYCLE
		internal override void AfterArrange()
		{
			base.AfterArrange();
#else
		internal override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();
#endif
			UpdateDimensionProperties();
			UpdateZoomedContentAlignment();
		}

		private double LayoutRoundIfNeeded(FrameworkElement fe, double value)
		{
			return this.GetUseLayoutRounding() ? fe.LayoutRound(value) : value;
		}

#if __APPLE_UIKIT__
		internal
#else
		private
#endif
			void UpdateDimensionProperties()
		{
			// The dimensions of the presenter (which are often but not always the same as the ScrollViewer) determine the viewport size
			var vpHeight = (_presenter as IFrameworkElement)?.ActualHeight ?? ActualHeight;
			var vpWidth = (_presenter as IFrameworkElement)?.ActualWidth ?? ActualWidth;

			if (vpHeight == 0 || vpWidth == 0)
			{
				// Do not update properties if we don't have any valid size yet.
				// This is useful essentially for the first size changed on the Content,
				// where it already have its final size while the SV doesn't.
				// This would cause a Scrollable<Width|Height> greater than 0,
				// which will cause the materialization of the managed scrollbar
				// which might not be needed after next layout pass.
				return;
			}

			if ((ActualHeight != vpHeight || ActualWidth != vpWidth) &&
				this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"ScrollViewer setting ViewportHeight={ActualHeight}, ViewportWidth={ActualWidth}");
			}

			ViewportHeight = vpHeight;
			ViewportWidth = vpWidth;

			var oldSize = new Size(ExtentWidth, ExtentHeight);

			if (_presenter?.CustomContentExtent is { } customExtent)
			{
				ExtentHeight = customExtent.Height;
				ExtentWidth = customExtent.Width;
			}
			else if (Content is FrameworkElement fe)
			{
				ExtentHeight = CalculateExtent(this, fe, isHorizontal: false);
				ExtentWidth = CalculateExtent(this, fe, isHorizontal: true);

				static double CalculateExtent(ScrollViewer sv, FrameworkElement fe, bool isHorizontal)
				{
					var margin = isHorizontal ? GetEffectiveMargin(fe.Margin.Left, fe.Margin.Right) : GetEffectiveMargin(fe.Margin.Top, fe.Margin.Bottom);
					var @explicit = isHorizontal ? fe.Width : fe.Height;
					if (@explicit.IsFinite())
					{
						return sv.LayoutRoundIfNeeded(fe, @explicit + margin);
					}

					var isStretchAlign = isHorizontal ? fe.HorizontalAlignment == HorizontalAlignment.Stretch : fe.VerticalAlignment == VerticalAlignment.Stretch;
					var actual = isHorizontal ? fe.ActualWidth : fe.ActualHeight;
					if (actual > 0 && isStretchAlign &&
						// Due to #2269, TextBlock ActualSize is implemented via DesiredSize
						// which includes the Margin already. We just let it flow to the next block
						// to avoid including margin twice here.
						fe is not TextBlock
					)
					{
						return sv.LayoutRoundIfNeeded(fe, actual + margin);
					}

					// DesiredSize includes the margin already, so we don't need to add it again.
					var desired = isHorizontal ? fe.DesiredSize.Width : fe.DesiredSize.Height;
					return sv.LayoutRoundIfNeeded(fe, desired);
				}
				static double GetEffectiveMargin(double leadingMargin, double trailingMargin)
				{
#if !__WASM__
					return leadingMargin + trailingMargin;
#else
					// Issue needs to be fixed first for WASM for missing trailing Margin
					// Details here: https://github.com/unoplatform/uno/issues/7000
					return leadingMargin;
#endif
				}
			}
			else
			{
				ExtentHeight = 0;
				ExtentWidth = 0;
			}

			// For scrollable height and scrollable width we apply rounding
			// to ensure there is no unwanted difference caused by double
			// precision, which could then cause the scroll bars to appear
			// for no reason.

			var scrollableHeight = Math.Max(Math.Round(ExtentHeight - ViewportHeight, 4), 0);

			ScrollableHeight = scrollableHeight;

			var scrollableWidth = Math.Max(Math.Round(ExtentWidth - ViewportWidth, 4), 0);

			ScrollableWidth = scrollableWidth;

			if (Presenter is not null)
			{
				Presenter.ExtentHeight = ExtentHeight;
				Presenter.ExtentWidth = ExtentWidth;
			}

			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);

			TrimOverscroll(Orientation.Vertical);
			TrimOverscroll(Orientation.Horizontal);

			var newSize = new Size(ExtentWidth, ExtentWidth);
			if (oldSize != newSize)
			{
				ExtentSizeChanged?.Invoke(this, new(this, oldSize, newSize));
			}
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

			MaterializeVerticalScrollBarIfNeeded(computedVisibility);

			ComputedVerticalScrollBarVisibility = computedVisibility;
			ComputedIsVerticalScrollEnabled = computedEnabled;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.NativeVerticalScrollBarVisibility = ComputeNativeScrollBarVisibility(scrollable, visibility, mode, _verticalScrollbar);
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

			MaterializeHorizontalScrollBarIfNeeded(computedVisibility);

			ComputedHorizontalScrollBarVisibility = computedVisibility;
			ComputedIsHorizontalScrollEnabled = computedEnabled;

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.NativeHorizontalScrollBarVisibility = ComputeNativeScrollBarVisibility(scrollable, visibility, mode, _horizontalScrollbar);
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

#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
		private ScrollBarVisibility ComputeNativeScrollBarVisibility(double scrollable, ScrollBarVisibility visibility, ScrollMode mode, ScrollBar? managedScrollbar)
			=> (scrollable, visibility, mode, managedScrollbar) switch
			{
				(_, _, ScrollMode.Disabled, _) => ScrollBarVisibility.Disabled,
				(0, ScrollBarVisibility.Auto, _, null) => ScrollBarVisibility.Hidden, // If scrollable is 0, the managed scrollbar won't be realized, we prefer to hide the native one until we are sure!
				(_, _, _, null) when Uno.UI.Xaml.Controls.ScrollViewer.GetShouldFallBackToNativeScrollBars(this) => visibility,
				(_, ScrollBarVisibility.Disabled, _, _) => ScrollBarVisibility.Disabled,
				_ => ScrollBarVisibility.Hidden // If a managed scroll bar was set in the template, native scroll bar has to stay Hidden
			};
#endif

		/// <summary>
		/// Sets the content of the ScrollViewer
		/// </summary>
		/// <param name="view"></param>
		/// <remarks>Used in the context of member initialization</remarks>
		public
#if !UNO_REFERENCE_API && !IS_UNIT_TESTS
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

#if __WASM__ || __SKIA__
			if (_presenter != null && ForceChangeToCurrentView)
			{
				_presenter.ForceChangeToCurrentView = ForceChangeToCurrentView;
			}
#endif
			// Load new template
			_verticalScrollbar = null;
			_isVerticalScrollBarMaterialized = false;
			_horizontalScrollbar = null;
			_isHorizontalScrollBarMaterialized = false;

#if __APPLE_UIKIT__ || __ANDROID__
			if (scpTemplatePart is ScrollContentPresenter scp && scp.Native is null)
			{
				// For Android and iOS, ensure that the ScrollContentPresenter contains a native SCP,
				// which will handle the actual scrolling.
				var nativeSCP = new NativeScrollContentPresenter(this);
				scp.Content = scp.Native = nativeSCP;
				_presenter = nativeSCP;
			}
#endif

			if (scpTemplatePart is ScrollContentPresenter presenter)
			{
				presenter.ScrollOwner = this;
				Presenter = presenter;
			}
			else
			{
				Presenter = null;
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

		#region Content forwarding to the ScrollContentPresenter
		protected override void OnContentChanged(object? oldValue, object? newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			if (_presenter is not null)
			{
				ApplyScrollContentPresenterContent(newValue);
			}

			UpdateSizeChangedSubscription();

			_snapPointsInfo = newValue as IScrollSnapPointsInfo;
		}

		private void ApplyScrollContentPresenterContent(object? content)
		{
			// Then explicitly propagate the Content to the _presenter
			if (_presenter != null)
			{
				_presenter.Content = content as View;
			}
		}

		private void UpdateSizeChangedSubscription(bool isCleanupRequired = false)
		{
			_sizeChangedSubscription.Disposable = null;
			if (!isCleanupRequired &&
				Content is IFrameworkElement element)
			{
				element.SizeChanged += OnElementSizeChanged;
				_sizeChangedSubscription.Disposable = Disposable.Create(() =>
					element.SizeChanged -= OnElementSizeChanged
				);
			}

			void OnElementSizeChanged(object sender, SizeChangedEventArgs args)
				=> UpdateDimensionProperties();
		}
		#endregion

		#region Managed scroll bars support
		private bool _isTemplateApplied;
		private ScrollBar? _verticalScrollbar;
		private ScrollBar? _horizontalScrollbar;
		private bool _isVerticalScrollBarMaterialized;
		private bool _isHorizontalScrollBarMaterialized;

		internal ScrollBar? ElementHorizontalScrollBar => _horizontalScrollbar;
		internal ScrollBar? ElementVerticalScrollBar => _verticalScrollbar;

		private void MaterializeVerticalScrollBarIfNeeded(Visibility computedVisibility)
		{
			if (!_isTemplateApplied || _isVerticalScrollBarMaterialized || computedVisibility != Visibility.Visible)
			{
				return;
			}

			_verticalScrollbar = (GetTemplateChild(Parts.WinUI3.VerticalScrollBar) ?? GetTemplateChild(Parts.Uwp.VerticalScrollBar)) as ScrollBar;
			_isVerticalScrollBarMaterialized = true;

			if (_verticalScrollbar is null)
			{
				return;
			}

			DetachScrollBars();
			AttachScrollBars();
		}

		private void MaterializeHorizontalScrollBarIfNeeded(Visibility computedVisibility)
		{
			if (!_isTemplateApplied || _isHorizontalScrollBarMaterialized || computedVisibility != Visibility.Visible)
			{
				return;
			}

			_horizontalScrollbar = (GetTemplateChild(Parts.WinUI3.HorizontalScrollBar) ?? GetTemplateChild(Parts.Uwp.HorizontalScrollBar)) as ScrollBar;
			_isHorizontalScrollBarMaterialized = true;

			if (_horizontalScrollbar is null)
			{
				return;
			}

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

		private void EnsureAttachScrollBars()
		{
			DetachScrollBars(); // Avoid double subscribe due to OnApplyTemplate
			AttachScrollBars();
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

			// On Windows, ScrollViewer ignores ScrollBar's SmallChange/LargeChange values.
			// No matter how SmallChange/LargeChange are set, ScrollViewer will always scroll by 16 (instead of SmallChange)
			// or ScrollViewer's Height (instead of LargeChange).
			var (immediate, offset) = e.ScrollEventType switch
			{
				ScrollEventType.LargeIncrement => (false, VerticalOffset + ActualHeight),
				ScrollEventType.LargeDecrement => (false, VerticalOffset - ActualHeight),
				ScrollEventType.SmallIncrement => (false, VerticalOffset + 16),
				ScrollEventType.SmallDecrement => (false, VerticalOffset - 16),
				_ => (true, e.NewValue)
			};

			ChangeViewCore(
				horizontalOffset: null,
				verticalOffset: offset,
				zoomFactor: null,
				disableAnimation: immediate,
				shouldSnap: true);
		}

		private void OnHorizontalScrollBarScrolled(object sender, ScrollEventArgs e)
		{
			// We animate only if the user clicked in the scroll bar, and disable otherwise
			// (especially, we disable animation when dragging the thumb)

			// On Windows, ScrollViewer ignores ScrollBar's SmallChange/LargeChange values.
			// No matter how SmallChange/LargeChange are set, ScrollViewer will always scroll by 16 (instead of SmallChange)
			// or ScrollViewer's Width (instead of LargeChange).
			var (immediate, offset) = e.ScrollEventType switch
			{
				ScrollEventType.LargeIncrement => (false, HorizontalOffset + ActualWidth),
				ScrollEventType.LargeDecrement => (false, HorizontalOffset - ActualWidth),
				ScrollEventType.SmallIncrement => (false, HorizontalOffset + 16),
				ScrollEventType.SmallDecrement => (false, HorizontalOffset - 16),
				_ => (true, e.NewValue)
			};

			ChangeViewCore(
				horizontalOffset: offset,
				verticalOffset: null,
				zoomFactor: null,
				disableAnimation: immediate,
				shouldSnap: true);
		}
		#endregion

		// Presenter to Control, i.e. OnPresenterScrolled
		internal void OnPresenterScrolled(double horizontalOffset, double verticalOffset, bool isIntermediate)
		{
			_pendingHorizontalOffset = horizontalOffset;
			_pendingVerticalOffset = verticalOffset;

			if (isIntermediate && UpdatesMode != Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous)
			{
				RequestUpdate();
				_snapPointsTimer?.Stop();
			}
			else
			{
				Update(isIntermediate);

				if (isIntermediate)
				{
					// when intermediate (aka manual) scrolling occurs,
					// we want to cancel any pending snapping, to prevent snapping to occur mid-scroll.
					_snapPointsTimer?.Stop();
				}
				if (!isIntermediate
#if __APPLE_UIKIT__ || __ANDROID__
					&& (_presenter as ListViewBaseScrollContentPresenter)?.NativePanel?.UseNativeSnapping != true
#endif
					)
				{
					if (HorizontalSnapPointsType != SnapPointsType.None
						|| VerticalSnapPointsType != SnapPointsType.None
						|| ShouldSnapToTouchTextBox())
					{
						_horizontalOffsetForSnapPoints = horizontalOffset;
						_verticalOffsetForSnapPoints = verticalOffset;

						if (_snapPointsTimer == null)
						{
							_snapPointsTimer = global::Windows.System.DispatcherQueue.GetForCurrentThread().CreateTimer();
							_snapPointsTimer.IsRepeating = false;
							_snapPointsTimer.Interval = FeatureConfiguration.ScrollViewer.SnapDelay;
							_snapPointsTimer.Tick += (snd, evt) =>
							{
								DelayedMoveToSnapPoint();
							};
						}

						_snapPointsTimer.Start();
					}
				}
			}

#if __WASM__
			// On WASM, a large wheel scroll can be a large number of OnScroll events in sequence.
			// In that case, the queue will be drowning with scroll events before any chance of layout
			// updates. The ScrollContentPresenter will scroll smoothly since the native scrolling/rendering
			// is on a separate thread, but the ScrollBars will be frozen until the end of the (long) scrolling
			// duration.
			_horizontalScrollbar?.Arrange(LayoutInformation.GetLayoutSlot(_horizontalScrollbar));
			_verticalScrollbar?.Arrange(LayoutInformation.GetLayoutSlot(_verticalScrollbar));
#endif
		}

		// Presenter to Control, i.e. OnPresenterZoomed
		internal void OnPresenterZoomed(float zoomFactor)
		{
			ZoomFactor = zoomFactor;

			// Note: We should also defer the intermediate zoom changes
			Update(isIntermediate: false);

			UpdateZoomedContentAlignment();
		}

		#region Deferred update (i.e. ViewChanged) support
		private bool _hasPendingUpdate;
		private double _pendingHorizontalOffset;
		private double _pendingVerticalOffset;

		private void RequestUpdate()
		{
			if (_hasPendingUpdate)
			{
				return;
			}

			_ = Dispatcher.RunIdleAsync(e =>
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

			var oldHorizontalOffset = HorizontalOffset;
			var oldVerticalOffset = VerticalOffset;

			HorizontalOffset = _pendingHorizontalOffset;
			VerticalOffset = _pendingVerticalOffset;

			// Not ideal, and doesn't match WinUI. This can miss raising some automation events.
			if (AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged) &&
				GetAutomationPeer() is ScrollViewerAutomationPeer peer)
			{
				peer.RaiseAutomationEvents(
					ExtentWidth,
					ExtentHeight,
					ViewportWidth,
					ViewportHeight,
					MinHorizontalOffset,
					MinVerticalOffset,
					oldHorizontalOffset,
					oldVerticalOffset);
			}

			UpdatePartial(isIntermediate);

			ViewChanged?.Invoke(this, new ScrollViewerViewChangedEventArgs { IsIntermediate = isIntermediate });
		}

		partial void UpdatePartial(bool isIntermediate);
		#endregion

		#region SnapPoints enforcement
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
		#endregion

		public void ScrollToHorizontalOffset(double offset)
			=> ChangeView(offset, null, null, true);

		public void ScrollToVerticalOffset(double offset)
			=> ChangeView(null, offset, null, true);

		/// <summary>
		/// Scroll content by one page to the left.
		/// </summary>
		internal void PageLeft()
			=> HandleHorizontalScroll(ScrollEventType.LargeDecrement);

		/// <summary>
		/// Scroll content by one line to the right.
		/// </summary>
		internal void LineLeft()
			=> HandleHorizontalScroll(ScrollEventType.SmallDecrement);

		/// <summary>
		/// Scroll content by one line to the right.
		/// </summary>
		internal void LineRight()
			=> HandleHorizontalScroll(ScrollEventType.SmallIncrement);

		/// <summary>
		/// Scroll content by one page to the right.
		/// </summary>
		internal void PageRight()
			=> HandleHorizontalScroll(ScrollEventType.LargeIncrement);

		/// <summary>
		/// Scroll content by one page to the top.
		/// </summary>
		internal void PageUp()
			=> HandleVerticalScroll(ScrollEventType.LargeDecrement);

		/// <summary>
		/// Scroll content by one line to the top.
		/// </summary>
		internal void LineUp()
			=> HandleVerticalScroll(ScrollEventType.SmallDecrement);

		/// <summary>
		/// Scroll content by one line to the bottom.
		/// </summary>
		internal void LineDown()
			=> HandleVerticalScroll(ScrollEventType.SmallIncrement);

		/// <summary>
		/// Scroll content by one page to the bottom.
		/// </summary>
		internal void PageDown()
			=> HandleVerticalScroll(ScrollEventType.LargeIncrement);

		/// <summary>
		/// Scroll content to the beginning.
		/// </summary>
		internal void PageHome()
			=> HandleVerticalScroll(ScrollEventType.First);

		/// <summary>
		/// Scroll content to the end.
		/// </summary>
		internal void PageEnd()
			=> HandleVerticalScroll(ScrollEventType.Last);

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

			if (horizontalOffset == null && verticalOffset == null && zoomFactor == null)
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
				AdjustOffsetsForSnapPoints(ref horizontalOffset, ref verticalOffset, zoomFactor, canBypassSingle: true);
			}

			return ChangeViewNative(horizontalOffset, verticalOffset, zoomFactor, disableAnimation);
		}

		#region Scroll indicators visual states (Managed scroll bars only)

		private static readonly TimeSpan _indicatorResetDelay = FeatureConfiguration.ScrollViewer.DefaultAutoHideDelay ?? TimeSpan.FromSeconds(4);
		private static readonly bool _indicatorResetDisabled = _indicatorResetDelay == TimeSpan.MaxValue;
		private DispatcherQueueTimer? _indicatorResetTimer;
		private string? _indicatorState;
		//private bool m_isInIntermediateViewChangedMode;
		//private bool m_isViewChangedRaisedInIntermediateMode;
		//private bool m_isDraggingThumb;

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

#if !__ANDROID__ && !__APPLE_UIKIT__ // ScrollContentPresenter.[Horizontal|Vertical]Offset not implemented on Android and iOS
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			// On WASM, we could choose to scroll in the managed layer and suppress the native scrolling
			// but it can lead to some chaotic scenarios where it's really difficult to reconcile the
			// numbers between ScrollViewer and ScrollContentPresenter, so we choose to keep the scrolling native
#if !__WASM__
			var key = args.Key;

			// WinUI stops keyboard scrolling if TemplatedParentHandlesScrolling
			// but interestingly that doesn't seem to affect pointer wheel scrolling
			// despite the generic name implying that it would stop all scrolling
			if (Presenter is null || TemplatedParentHandlesScrolling)
			{
				return;
			}

			var oldHorizontalOffset = Presenter.TargetHorizontalOffset;
			var oldVerticalOffset = Presenter.TargetVerticalOffset;

			// Check whether scrolling is allowed and focus can be moved.
			var (shouldScroll, shouldMoveFocus) = HandleKeyDownForXYNavigation(args);

			if (!shouldScroll)
			{
				return;
			}

			var newOffset = key switch
			{
				VirtualKey.Up => Math.Max(0, oldVerticalOffset - GetDelta(ActualHeight)),
				VirtualKey.Down => Math.Min(oldVerticalOffset + GetDelta(ActualHeight), ScrollableHeight),
				VirtualKey.Left => Math.Max(0, oldHorizontalOffset - GetDelta(ActualWidth)),
				VirtualKey.Right => Math.Min(oldHorizontalOffset + GetDelta(ActualWidth), ScrollableWidth),
				VirtualKey.PageUp => Math.Max(0, oldVerticalOffset - ActualHeight),
				VirtualKey.PageDown => Math.Min(oldVerticalOffset + ActualHeight, ScrollableHeight),
				VirtualKey.Home => 0,
				VirtualKey.End => ScrollableHeight,
				_ => double.E
			};

			if (newOffset == double.E)
			{
				return;
			}

			if (Content is UIElement)
			{
				var canScrollHorizontally = Presenter.CanHorizontallyScroll;
				var canScrollVertically = Presenter.CanVerticallyScroll;

				if (canScrollHorizontally && key is VirtualKey.Left or VirtualKey.Right)
				{
					ScrollToHorizontalOffset(newOffset);
					args.Handled = !NumericExtensions.AreClose(oldHorizontalOffset, Presenter.TargetHorizontalOffset);
				}
				else if (canScrollVertically && key is not (VirtualKey.Left or VirtualKey.Right))
				{
					ScrollToVerticalOffset(newOffset);
					args.Handled = !NumericExtensions.AreClose(oldVerticalOffset, Presenter.TargetVerticalOffset);
				}

				args.Handled |= key is VirtualKey.PageUp or VirtualKey.PageDown;
			}

			if (args.Handled && shouldMoveFocus)
			{
				// Continue bubbling the event so that the focus can be moved.
				args.Handled = false;
			}

			// This gets the delta that should be applied when arrow keys are pressed as a function of the
			// ScrollViewer length in the scrolling direction. WinUI's logic is not quite clear, I just
			// reverse-engineered the numbers until they matched precisely. I think the original code just
			// has some weird rounding somewhere that makes the numbers weird to calculate.
			static int GetDelta(double l)
			{
				var length = (int)Math.Max(0, Math.Round(l) - 16);
				var result = 2 + length / 20 * 3;

				switch (length % 20)
				{
					case 0:
						break;
					case <= 7:
						result += 1;
						break;
					case <= 14:
						result += 2;
						break;
					default:
						result += 3;
						break;
				}

				return result;
			}
#endif
		}
#endif

#if __CROSSRUNTIME__
		private static bool _warnedAboutZoomedContentAlignment;

		[NotImplemented]
		private void UpdateZoomedContentAlignment()
		{
			if (_warnedAboutZoomedContentAlignment)
			{
				return;
			}

			_warnedAboutZoomedContentAlignment = true;
			if (this.Log().IsEnabled(ApiInformation.NotImplementedLogLevel))
			{
				this.Log().Log(ApiInformation.NotImplementedLogLevel, "Zoom-based content alignment is not implemented on this platform.");
			}
		}
#endif

		/// <summary>
		/// Handles the vertical ScrollBar.Scroll event and updates the UI.
		/// </summary>
		internal void HandleVerticalScroll(ScrollEventType scrollEventType, double offset = 0)
		{
			//UNO TODO: Implement HandleVerticalScroll on ScrollViewer
		}

		/// <summary>
		/// Handles the horizontal ScrollBar.Scroll event and updates the UI.
		/// </summary>
		internal void HandleHorizontalScroll(ScrollEventType scrollEventType, double offset = 0)
		{
			//UNO TODO: Implement HandleHorizontalScroll on ScrollViewer
		}

		/// <summary>
		/// Determines whether this ScrollViewer is pannable.
		/// Returns false only if both vertical and horizontal scrolling are disabled.
		/// </summary>
		internal override bool IsDraggableOrPannable()
		{
			// If both vertical and horizontal scrolling are disabled, return false.
			// This matches WinUI's ScrollViewer::IsDraggableOrPannableImpl.
			return !(
				(VerticalScrollBarVisibility == ScrollBarVisibility.Disabled ||
					VerticalScrollMode == ScrollMode.Disabled)
				&&
				(HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled ||
					HorizontalScrollMode == ScrollMode.Disabled)
			);
		}
	}
}
