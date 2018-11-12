#if NET46
#pragma warning disable CS0067 
#endif

using System;
using System.Collections.Generic;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Uno;
using Uno.Extensions;
using Microsoft.Extensions.Logging;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollViewer : ContentControl, IFrameworkTemplatePoolAware
	{
		/// <summary>
		/// Occurs when manipulations such as scrolling and zooming have caused the view to change.
		/// </summary>
		public event EventHandler<ScrollViewerViewChangedEventArgs> ViewChanged;

		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

		internal Foundation.Size ViewportMeasureSize { get; private set; }
		internal Foundation.Size ViewportArrangeSize { get; private set; }

		static ScrollViewer()
		{
#if !NET46
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

		public ScrollViewer() { }

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

		#region HorizontalScrollBarVisibility
		public static ScrollBarVisibility GetHorizontalScrollBarVisibility(DependencyObject obj)
		{
			return (ScrollBarVisibility)obj.GetValue(HorizontalScrollBarVisibilityProperty);
		}

		public static void SetHorizontalScrollBarVisibility(DependencyObject obj, ScrollBarVisibility value)
		{
			obj.SetValue(HorizontalScrollBarVisibilityProperty, value);
		}

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return (ScrollBarVisibility)this.GetValue(HorizontalScrollBarVisibilityProperty); }
			set { this.SetValue(HorizontalScrollBarVisibilityProperty, value); }
		}

		internal const string ScrollContentPresenterPartName = "ScrollContentPresenter";

		// Using a DependencyProperty as the backing store for HorizontalScrollBarVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
			DependencyProperty.RegisterAttached(
				"HorizontalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new PropertyMetadata(
					ScrollBarVisibility.Disabled,
					propertyChangedCallback: OnHorizontalScrollBarVisibilityPropertyChanged
				)
			);

		private static void OnHorizontalScrollBarVisibilityPropertyChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if XAMARIN
			var view = dependencyObject as ScrollViewer;

			if (view?._sv != null)
			{
				view._sv.HorizontalScrollBarVisibility = view.HorizontalScrollBarVisibility;
				view.InvalidateMeasure();
			}
#endif
		}
		#endregion

		#region VerticalScrollBarVisibility
		public static ScrollBarVisibility GetVerticalScrollBarVisibility(DependencyObject obj)
		{
			return (ScrollBarVisibility)obj.GetValue(VerticalScrollBarVisibilityProperty);
		}

		public static void SetVerticalScrollBarVisibility(DependencyObject obj, ScrollBarVisibility value)
		{
			obj.SetValue(VerticalScrollBarVisibilityProperty, value);
		}

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return (ScrollBarVisibility)this.GetValue(VerticalScrollBarVisibilityProperty); }
			set { this.SetValue(VerticalScrollBarVisibilityProperty, value); }
		}

		// Using a DependencyProperty as the backing store for VerticalScrollBarVisibility.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
			DependencyProperty.RegisterAttached(
				"VerticalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new PropertyMetadata(
					ScrollBarVisibility.Auto,
					propertyChangedCallback: OnVerticalScrollBarVisibilityPropertyChanged
				)
			);
		private static void OnVerticalScrollBarVisibilityPropertyChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if XAMARIN
			var view = dependencyObject as ScrollViewer;
			if (view?._sv != null)
			{

				view._sv.VerticalScrollBarVisibility = view.VerticalScrollBarVisibility;
				view.InvalidateMeasure();
			}
#endif
		}
		#endregion

		#region HorizontalScrollMode DependencyProperty

		public static ScrollMode GetHorizontalScrollMode(DependencyObject obj)
		{
			return (ScrollMode)obj.GetValue(HorizontalScrollModeProperty);
		}

		public static void SetHorizontalScrollMode(DependencyObject obj, ScrollMode value)
		{
			obj.SetValue(HorizontalScrollModeProperty, value);
		}

		public ScrollMode HorizontalScrollMode
		{
			get { return (ScrollMode)this.GetValue(HorizontalScrollModeProperty); }
			set { this.SetValue(HorizontalScrollModeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HorizontalScrollMode.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HorizontalScrollModeProperty =
			DependencyProperty.RegisterAttached(
				"HorizontalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new PropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnHorizontalScrollModeChanged
				)
			);


		private static void OnHorizontalScrollModeChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if XAMARIN
			var view = dependencyObject as ScrollViewer;
			if (view?._sv != null)
			{
				view._sv.HorizontalScrollMode = view.HorizontalScrollMode;
				view.InvalidateMeasure();
			}
#endif
		}

		#endregion

		#region VerticalScrollMode DependencyProperty

		public static ScrollMode GetVerticalScrollMode(DependencyObject obj)
		{
			return (ScrollMode)obj.GetValue(VerticalScrollModeProperty);
		}

		public static void SetVerticalScrollMode(DependencyObject obj, ScrollMode value)
		{
			obj.SetValue(VerticalScrollModeProperty, value);
		}

		public ScrollMode VerticalScrollMode
		{
			get { return (ScrollMode)this.GetValue(VerticalScrollModeProperty); }
			set { this.SetValue(VerticalScrollModeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for VerticalScrollMode.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty VerticalScrollModeProperty =
			DependencyProperty.RegisterAttached(
				"VerticalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new PropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnVerticalScrollModeChanged
				)
			);


		private static void OnVerticalScrollModeChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
#if XAMARIN
			var view = dependencyObject as ScrollViewer;
			if (view?._sv != null)
			{
				view._sv.VerticalScrollMode = view.VerticalScrollMode;
				view.InvalidateMeasure();
			}
#endif
		}

		#endregion

		public ZoomMode ZoomMode
		{
			get { return (ZoomMode)GetValue(ZoomModeProperty); }
			set { SetValue(ZoomModeProperty, value); }
		}

		public static readonly DependencyProperty ZoomModeProperty =
			DependencyProperty.Register("ZoomMode", typeof(ZoomMode), typeof(ScrollViewer), new PropertyMetadata(ZoomMode.Disabled, (o, e) => ((ScrollViewer)o).OnZoomModeChanged((ZoomMode)e.NewValue)));

		private void OnZoomModeChanged(ZoomMode zoomMode)
		{
			OnZoomModeChangedPartial(zoomMode);
		}

		partial void OnZoomModeChangedPartial(ZoomMode zoomMode);

		public float MinZoomFactor
		{
			get { return (float)GetValue(MinZoomFactorProperty); }
			set { SetValue(MinZoomFactorProperty, value); }
		}

		public static readonly DependencyProperty MinZoomFactorProperty =
			DependencyProperty.Register("MinZoomFactor", typeof(float), typeof(ScrollViewer), new PropertyMetadata(0.1f, (o, e) => ((ScrollViewer)o).OnMinZoomFactorChanged(e)));

		private void OnMinZoomFactorChanged(DependencyPropertyChangedEventArgs args)
		{
			_sv?.OnMinZoomFactorChanged((float)args.NewValue);
		}


		public float MaxZoomFactor
		{
			get { return (float)GetValue(MaxZoomFactorProperty); }
			set { SetValue(MaxZoomFactorProperty, value); }
		}

		public static readonly DependencyProperty MaxZoomFactorProperty =
			DependencyProperty.Register("MaxZoomFactor", typeof(float), typeof(ScrollViewer), new PropertyMetadata(10f, (o, e) => ((ScrollViewer)o).OnMaxZoomFactorChanged(e)));

		private void OnMaxZoomFactorChanged(DependencyPropertyChangedEventArgs args)
		{
			_sv?.OnMaxZoomFactorChanged((float)args.NewValue);
		}

		public float ZoomFactor
		{
			get { return (float)GetValue(ZoomFactorProperty); }
			private set { SetValue(ZoomFactorProperty, value); }
		}

		public static readonly DependencyProperty ZoomFactorProperty =
			DependencyProperty.Register("ZoomFactor", typeof(float), typeof(ScrollViewer), new PropertyMetadata(1f));

		public SnapPointsType HorizontalSnapPointsType
		{
			get
			{
				return (SnapPointsType)this.GetValue(HorizontalSnapPointsTypeProperty);
			}
			set
			{
				this.SetValue(HorizontalSnapPointsTypeProperty, value);
			}
		}

		public static DependencyProperty HorizontalSnapPointsTypeProperty { get; } =
		DependencyProperty.Register(
			"HorizontalSnapPointsType", typeof(SnapPointsType),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(SnapPointsType)));

		public SnapPointsAlignment HorizontalSnapPointsAlignment
		{
			get
			{
				return (SnapPointsAlignment)this.GetValue(HorizontalSnapPointsAlignmentProperty);
			}
			set
			{
				this.SetValue(HorizontalSnapPointsAlignmentProperty, value);
			}
		}

		public static DependencyProperty HorizontalSnapPointsAlignmentProperty { get; } =
		DependencyProperty.Register(
			"HorizontalSnapPointsAlignment", typeof(SnapPointsAlignment),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(SnapPointsAlignment)));

		public SnapPointsType VerticalSnapPointsType
		{
			get
			{
				return (SnapPointsType)this.GetValue(VerticalSnapPointsTypeProperty);
			}
			set
			{
				this.SetValue(VerticalSnapPointsTypeProperty, value);
			}
		}

		public static DependencyProperty VerticalSnapPointsTypeProperty { get; } =
		DependencyProperty.Register(
			"VerticalSnapPointsType", typeof(SnapPointsType),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(SnapPointsType)));

		public SnapPointsAlignment VerticalSnapPointsAlignment
		{
			get
			{
				return (SnapPointsAlignment)this.GetValue(VerticalSnapPointsAlignmentProperty);
			}
			set
			{
				this.SetValue(VerticalSnapPointsAlignmentProperty, value);
			}
		}

		public static DependencyProperty VerticalSnapPointsAlignmentProperty { get; } =
		DependencyProperty.Register(
			"VerticalSnapPointsAlignment", typeof(SnapPointsAlignment),
			typeof(ScrollViewer),
			new FrameworkPropertyMetadata(default(SnapPointsAlignment)));

		public double ExtentHeight
		{
			get => (double)GetValue(ExtentHeightProperty);
			private set => SetValue(ExtentHeightProperty, value);
		}

		public static DependencyProperty ExtentHeightProperty { get; } =
			DependencyProperty.Register(
				"ExtentHeight", typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));

		public double ExtentWidth
		{
			get => (double)GetValue(ExtentWidthProperty);
			private set => SetValue(ExtentWidthProperty, value);
		}

		public static DependencyProperty ExtentWidthProperty { get; } =
			DependencyProperty.Register(
				"ExtentWidth", typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));

		public double ViewportHeight
		{
			get => (double)GetValue(ViewportHeightProperty);
			private set => SetValue(ViewportHeightProperty, value);
		}

		public static DependencyProperty ViewportHeightProperty { get; } =
			DependencyProperty.Register(
				"ViewportHeight", typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));

		public double ViewportWidth
		{
			get => (double)GetValue(ViewportWidthProperty);
			private set => SetValue(ViewportWidthProperty, value);
		}

		public static DependencyProperty ViewportWidthProperty { get; } =
			DependencyProperty.Register(
				"ViewportWidth", typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));

		public double ScrollableHeight
		{
			get => (double)GetValue(ScrollableHeightProperty);
			private set => SetValue(ScrollableHeightProperty, value);
		}

		public static DependencyProperty ScrollableHeightProperty { get; } =
			DependencyProperty.Register(
				"ScrollableHeight", typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));

		public double ScrollableWidth
		{
			get => (double)GetValue(ScrollableWidthProperty);
			private set => SetValue(ScrollableWidthProperty, value);
		}

		public static DependencyProperty ScrollableWidthProperty { get; } =
			DependencyProperty.Register(
				"ScrollableWidth", typeof(double),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(default(double)));

#if __IOS__
		[global::Uno.NotImplemented]
#endif
		public bool BringIntoViewOnFocusChange
		{
			get { return (bool)GetValue(BringIntoViewOnFocusChangeProperty); }
			set { SetValue(BringIntoViewOnFocusChangeProperty, value); }
		}

		public static DependencyProperty BringIntoViewOnFocusChangeProperty { get; } =
			DependencyProperty.RegisterAttached(
				"BringIntoViewOnFocusChange",
				typeof(bool),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(true, OnBringIntoViewOnFocusChangeChanged));

		private static void OnBringIntoViewOnFocusChangeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = dependencyObject as ScrollViewer;

			view?.OnBringIntoViewOnFocusChangeChangedPartial((bool) args.NewValue);
		}

		partial void OnBringIntoViewOnFocusChangeChangedPartial(bool newValue);

		private static void OnGenericPropertyChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var view = dependencyObject as View;

			if (view != null)
			{
				view.InvalidateMeasure();
			}
		}

		protected override Foundation.Size MeasureOverride(Foundation.Size availableSize)
		{
			ViewportMeasureSize = availableSize;

			return base.MeasureOverride(availableSize);
		}

#pragma warning disable 649 // unused member for Unit tests
		private IScrollContentPresenter _sv;
#pragma warning restore 649 // unused member for Unit tests

		protected override Foundation.Size ArrangeOverride(Foundation.Size finalSize)
		{
			ViewportArrangeSize = finalSize;

			var size = base.ArrangeOverride(finalSize);

			UpdateDimensionProperties();

			UpdateZoomedContentAlignment();

			return size;
		}

		private void UpdateDimensionProperties()
		{
			ViewportHeight = this.ActualHeight;
			ViewportWidth = this.ActualWidth;

			ExtentHeight = (Content as IFrameworkElement)?.ActualHeight ?? 0;
			ExtentWidth = (Content as IFrameworkElement)?.ActualWidth ?? 0;

			ScrollableHeight = Math.Max(ExtentHeight - ViewportHeight, 0);
			ScrollableWidth = Math.Max(ExtentWidth - ViewportWidth, 0);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"ScrollViewer setting ViewportHeight={ViewportHeight}, ViewportWidth={ViewportWidth}");
			}
		}

#if !NET46
		/// <summary>
		/// Sets the content of the ScrollViewer
		/// </summary>
		/// <param name="view"></param>
		/// <remarks>Used in the context of member initialization</remarks>
		public
#if !NETSTANDARD2_0 && !__MACOS__
			new 
#endif
			void Add(View view)
		{
			Content = view;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_sv = this.GetTemplateChild(ScrollContentPresenterPartName) as IScrollContentPresenter;

			if (_sv == null)
			{
				throw new InvalidOperationException("The template part ScrollContentPresenter could not be found or is not a ScrollContentPresenter");
			}

			_sv.Content = Content as View;

			// Apply correct initial zoom settings
			OnZoomModeChanged(ZoomMode);

			OnApplyTemplatePartial();

			OnBringIntoViewOnFocusChangeChangedPartial(BringIntoViewOnFocusChange);
		}

		partial void OnApplyTemplatePartial();

		protected override void OnContentChanged(object oldValue, object newValue)
		{
			base.OnContentChanged(oldValue, newValue);

			if (_sv != null)
			{
				// remove the explicit templated parent propagation
				// for the lack of TemplatedParentScope support
				ClearContentTemplatedParent(oldValue);

				_sv.Content = Content as View;

				// Propagate the ScrollViewer's own templated parent, instead of 
				// the scrollviewer itself (through ScrollContentPreset
				SynchronizeContentTemplatedParent(TemplatedParent);
			}

			UpdateSizeChangedSubscription();
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
		}

		private void OnElementSizeChanged(object sender, SizeChangedEventArgs args)
		{
			UpdateDimensionProperties();
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

		#region VerticalOffset DependencyProperty

		public double VerticalOffset
		{
			get { return (double)GetValue(VerticalOffsetProperty); }
			private set { SetValue(VerticalOffsetProperty, value); }
		}

		public static DependencyProperty VerticalOffsetProperty =
			DependencyProperty.Register(
				"VerticalOffset",
				typeof(double),
				typeof(ScrollViewer),
				new PropertyMetadata(
					(double)0,
					null
				)
			);

		#endregion

		#region HorizontalOffset DependencyProperty

		public double HorizontalOffset
		{
			get { return (double)GetValue(HorizontalOffsetProperty); }
			private set { SetValue(HorizontalOffsetProperty, value); }
		}

		public static DependencyProperty HorizontalOffsetProperty =
			DependencyProperty.Register(
				"HorizontalOffset",
				typeof(double),
				typeof(ScrollViewer),
				new PropertyMetadata(
					(double)0,
					null
				)
			);

		#endregion

		/// <summary>
		/// If this flag is enabled, the ScrollViewer will report offsets less than 0 and greater than <see cref="ScrollableHeight"/> when 
		/// 'overscrolling' on iOS. By default this is false, matching Windows behaviour.
		/// </summary>
		[UnoOnly]
		public bool ShouldReportNegativeOffsets { get; set; } = false;

		internal void OnScrollInternal(double horizontalOffset, double verticalOffset, bool isIntermediate)
		{
			VerticalOffset = verticalOffset;
			HorizontalOffset = horizontalOffset;

			ViewChanged?.Invoke(this, new ScrollViewerViewChangedEventArgs { IsIntermediate = isIntermediate });
		}

		internal void OnZoomInternal(float zoomFactor)
		{
			ZoomFactor = zoomFactor;

			ViewChanged?.Invoke(this, new ScrollViewerViewChangedEventArgs());

			UpdateZoomedContentAlignment();
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
#endif
	}
}
