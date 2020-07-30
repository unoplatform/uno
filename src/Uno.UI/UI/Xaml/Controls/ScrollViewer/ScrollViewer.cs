#if NET461
#pragma warning disable CS0067
#endif

using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Windows.UI.Core;
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

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollViewer : ContentControl, IFrameworkTemplatePoolAware
	{
		internal const string ScrollContentPresenterPartName = "ScrollContentPresenter";
		private const string VerticalScrollBarPartName = "VerticalScrollBar";
		private const string HorizontalScrollBarPartName = "HorizontalScrollBar";

		/// <summary>
		/// Occurs when manipulations such as scrolling and zooming have caused the view to change.
		/// </summary>
		public event EventHandler<ScrollViewerViewChangedEventArgs> ViewChanged;

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

			UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewer.GetUpdatesMode(this);
			InitializePartial();
		}

		partial void InitializePartial();

		#region Common DP callbacks
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

		public static DependencyProperty HorizontalScrollBarVisibilityProperty { get ; } =
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

		public static DependencyProperty VerticalScrollBarVisibilityProperty { get ; } =
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

		public static DependencyProperty HorizontalScrollModeProperty { get ; } =
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
		public static DependencyProperty VerticalScrollModeProperty { get ; } =
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

		public static DependencyProperty ZoomModeProperty { get ; } =
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

		public static DependencyProperty MinZoomFactorProperty { get ; } =
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

		public static DependencyProperty MaxZoomFactorProperty { get ; } =
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

		public static DependencyProperty ZoomFactorProperty { get ; } =
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
				new PropertyMetadata(default(Visibility)));

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
				new PropertyMetadata(default(Visibility)));

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
		private IScrollContentPresenter _presenter;
#pragma warning restore 649 // unused member for Unit tests
		private ScrollBar _verticalScrollbar;
		private ScrollBar _horizontalScrollbar;

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

			var size = base.ArrangeOverride(finalSize);

			UpdateDimensionProperties();
			UpdateZoomedContentAlignment();

			return size;
		}

		private void UpdateDimensionProperties()
		{
			if (this.Log().IsEnabled(LogLevel.Debug)
				&& (ActualHeight != ViewportHeight || ActualWidth != ViewportWidth)
			)
			{
				this.Log().LogDebug($"ScrollViewer setting ViewportHeight={ActualHeight}, ViewportWidth={ActualWidth}");
			}

			ViewportHeight = ActualHeight;
			ViewportWidth = ActualWidth;

			ExtentHeight = (Content as IFrameworkElement)?.ActualHeight ?? 0;
			ExtentWidth = (Content as IFrameworkElement)?.ActualWidth ?? 0;

			ScrollableHeight = Math.Max(ExtentHeight - ViewportHeight, 0);
			ScrollableWidth = Math.Max(ExtentWidth - ViewportWidth, 0);

			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);
#if __WASM__
			Console.WriteLine($"[{this}-{HtmlId}] ViewPort:{ViewportWidth:N0}x{ViewportHeight:N0} "
				+ $"| Extent:{ExtentWidth:N0}x{ExtentHeight:N0} "
				+ $"| Scrollable:{ScrollableWidth:N0}x{ScrollableHeight:N0} "
				+ $"| ComputedVisibility:{ComputedHorizontalScrollBarVisibility}/{ComputedHorizontalScrollBarVisibility} "
				+ $"| ComputedIsEnabled:{ComputedIsHorizontalScrollEnabled}/{ComputedIsVerticalScrollEnabled}");
#endif
		}

		private void UpdateComputedVerticalScrollability(bool invalidate)
		{
			var scrollable = ScrollableHeight;
			var visibility = VerticalScrollBarVisibility;
			var mode = VerticalScrollMode;

			ComputedVerticalScrollBarVisibility = ComputeScrollBarVisibility(scrollable, visibility);
			ComputedIsVerticalScrollEnabled = ComputeIsScrollEnabled(scrollable, visibility, mode);

			if (_presenter == default)
			{
				return; // Control not ready yet
			}

			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.HorizontalScrollBarVisibility = ComputeNativeScrollBarVisibility(visibility, mode, _verticalScrollbar);
			if (invalidate && _verticalScrollbar == default)
			{
				InvalidateMeasure(); // Useless for managed ScrollBar, it will invalidate itself if needed.
			}
		}

		private void UpdateComputedHorizontalScrollability(bool invalidate)
		{
			var scrollable = ScrollableWidth;
			var visibility = HorizontalScrollBarVisibility;
			var mode = HorizontalScrollMode;

			ComputedHorizontalScrollBarVisibility = ComputeScrollBarVisibility(scrollable, visibility);
			ComputedIsHorizontalScrollEnabled = ComputeIsScrollEnabled(scrollable, visibility, mode);

			if (_presenter == default)
			{
				return; // Control not ready yet
			}

			// Support for the native scroll bars (delegated to the native _presenter).
			_presenter.HorizontalScrollBarVisibility = ComputeNativeScrollBarVisibility(visibility, mode, _horizontalScrollbar);
			if (invalidate && _horizontalScrollbar == default)
			{
				InvalidateMeasure(); // Useless for managed ScrollBar, it will invalidate itself if needed.
			}
		}

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

		private static ScrollBarVisibility ComputeNativeScrollBarVisibility(ScrollBarVisibility visibility, ScrollMode mode, ScrollBar managedScrollbar)
			=> mode == ScrollMode.Disabled
				? ScrollBarVisibility.Disabled
				: managedScrollbar == default
					? ScrollBarVisibility.Hidden // If a managed scroll bar was set in the template, native scroll bar has to stay Hidden
					: visibility;

		/// <summary>
		/// Sets the content of the ScrollViewer
		/// </summary>
		/// <param name="view"></param>
		/// <remarks>Used in the context of member initialization</remarks>
		public
#if !NETSTANDARD2_0 && !__MACOS__ && !NET461
			new
#endif
			void Add(View view)
		{
			Content = view;
		}

		protected override void OnApplyTemplate()
		{
			// Clean up previous template
			if (_verticalScrollbar != null)
			{
				_verticalScrollbar.Scroll -= OnVerticalScrollBarScrolled;
			}

			if (_horizontalScrollbar != null)
			{
				_horizontalScrollbar.Scroll -= OnHorizontalScrollBarScrolled;
			}

			base.OnApplyTemplate();

			// Load new template
			_verticalScrollbar = GetTemplateChild(VerticalScrollBarPartName) as ScrollBar;
			if (_verticalScrollbar != null)
			{
				_verticalScrollbar.Scroll += OnVerticalScrollBarScrolled;
			}

			_horizontalScrollbar = GetTemplateChild(HorizontalScrollBarPartName) as ScrollBar;
			if (_horizontalScrollbar != null)
			{
				_horizontalScrollbar.Scroll += OnHorizontalScrollBarScrolled;
			}

			var scpTemplatePart = GetTemplateChild(ScrollContentPresenterPartName);
			_presenter = scpTemplatePart as IScrollContentPresenter;

#if !NETSTANDARD
			if (scpTemplatePart is ScrollContentPresenter scp)
			{
				// For Android/iOS/MacOS, ensure that the ScrollContentPresenter contains a native scroll viewer,
				// which will handle the actual scrolling
				var nativeSCP = new NativeScrollContentPresenter();
				scp.Content = nativeSCP;
				_presenter = nativeSCP;
			}
#endif

			if (_presenter == null)
			{
				throw new InvalidOperationException("The template part ScrollContentPresenter could not be found or is not a ScrollContentPresenter");
			}

			// We update the scrollability properties here in order to make sure to set the right scrollbar visibility
			// on the _presenter as soon as possible
			UpdateComputedVerticalScrollability(invalidate: false);
			UpdateComputedHorizontalScrollability(invalidate: false);

			ApplyScrollContentPresenterContent();

			OnApplyTemplatePartial();

			// Apply correct initial zoom settings
			OnZoomModeChanged(ZoomMode);

			OnBringIntoViewOnFocusChangeChangedPartial(BringIntoViewOnFocusChange);
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

				ApplyScrollContentPresenterContent();
			}

			UpdateSizeChangedSubscription();
		}

		private void ApplyScrollContentPresenterContent()
		{
			// Stop the automatic propagation of the templated parent on the Content
			// This prevents issues when the a ScrollViewer is hosted in a control template
			// and its content is a ContentControl or ContentPresenter, which has a TemplateBinding
			// on the Content property. This can make the Content added twice in the visual tree.
			if (Content is IDependencyObjectStoreProvider provider)
			{
				provider.Store.SetValue(provider.Store.TemplatedParentProperty, null, DependencyPropertyValuePrecedences.Local);
			}

			// Then explicitly propagate the Content to the _presenter
			_presenter.Content = Content as View;

			// Propagate the ScrollViewer's own templated parent, instead of 
			// the scrollviewer itself (through ScrollContentPresenter)
			SynchronizeContentTemplatedParent(TemplatedParent);
		}

		private void UpdateSizeChangedSubscription(bool isCleanupRequired = false)
		{
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

		internal protected override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			SynchronizeContentTemplatedParent(e.NewValue as DependencyObject);
		}

		private void SynchronizeContentTemplatedParent(DependencyObject templatedParent)
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

			ChangeViewScroll(null, e.NewValue, disableAnimation: immediate);
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

			ChangeViewScroll(e.NewValue, null, disableAnimation: immediate);
		}

		// Presenter to Control, i.e. OnPresenterScrolled
		internal void OnScrollInternal(double horizontalOffset, double verticalOffset, bool isIntermediate)
		{
			_pendingHorizontalOffset = horizontalOffset;
			_pendingVerticalOffset = verticalOffset;

			if (isIntermediate && UpdatesMode != Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode.Synchronous)
			{
				RequestUpdate();
			}
			else
			{
				Update(isIntermediate);
			}
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

			ViewChanged?.Invoke(this, new ScrollViewerViewChangedEventArgs { IsIntermediate = isIntermediate });
		}

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

			var verticalOffsetChanged = verticalOffset != null && verticalOffset != VerticalOffset;
			var horizontalOffsetChanged = horizontalOffset != null && horizontalOffset != HorizontalOffset;

			var zoomFactorChanged = zoomFactor != null && zoomFactor != ZoomFactor;

			if (verticalOffsetChanged || horizontalOffsetChanged)
			{
				ChangeViewScroll(horizontalOffset, verticalOffset, disableAnimation);
			}
			if (zoomFactorChanged)
			{
				ChangeViewZoom(zoomFactor.Value, disableAnimation);
			}

			return verticalOffsetChanged || horizontalOffsetChanged || zoomFactorChanged;
		}

		/// <summary>
		/// Causes the ScrollViewer to load a new view into the viewport using the specified offsets and zoom factor, and optionally disables scrolling animation.
		/// </summary>
		/// <param name="horizontalOffset">A value between 0 and ScrollableWidth that specifies the distance the content should be scrolled horizontally.</param>
		/// <param name="verticalOffset">A value between 0 and ScrollableHeight that specifies the distance the content should be scrolled vertically.</param>
		/// <param name="zoomFactor">A value between MinZoomFactor and MaxZoomFactor that specifies the required target ZoomFactor.</param>
		/// <returns>true if the view is changed; otherwise, false.</returns>
		public bool ChangeView(double? horizontalOffset, double? verticalOffset, float? zoomFactor) => ChangeView(horizontalOffset, verticalOffset, zoomFactor, false);

		partial void ChangeViewScroll(double? horizontalOffset, double? verticalOffset, bool disableAnimation);
		partial void ChangeViewZoom(float zoomFactor, bool disableAnimation);
	}
}
