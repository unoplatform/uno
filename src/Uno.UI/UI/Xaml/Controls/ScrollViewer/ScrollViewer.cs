#if NET461
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
using Windows.UI.Core;
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
		internal const string ScrollContentPresenterPartName = "ScrollContentPresenter";

		/// <summary>
		/// Occurs when manipulations such as scrolling and zooming have caused the view to change.
		/// </summary>
		public event EventHandler<ScrollViewerViewChangedEventArgs> ViewChanged;

		private readonly SerialDisposable _sizeChangedSubscription = new SerialDisposable();

		internal Foundation.Size ViewportMeasureSize { get; private set; }
		internal Foundation.Size ViewportArrangeSize { get; private set; }

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
			UpdatesMode = Uno.UI.Xaml.Controls.ScrollViewer.GetUpdatesMode(this);
			InitializePartial();
		}

		partial void InitializePartial();

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

		public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty =
			DependencyProperty.RegisterAttached(
				"HorizontalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollBarVisibility.Disabled,
					propertyChangedCallback: OnHorizontalScrollBarVisibilityPropertyChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
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

		public static readonly DependencyProperty VerticalScrollBarVisibilityProperty =
			DependencyProperty.RegisterAttached(
				"VerticalScrollBarVisibility",
				typeof(ScrollBarVisibility),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollBarVisibility.Auto,
					propertyChangedCallback: OnVerticalScrollBarVisibilityPropertyChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
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

		public static readonly DependencyProperty HorizontalScrollModeProperty =
			DependencyProperty.RegisterAttached(
				"HorizontalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnHorizontalScrollModeChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
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
		public static readonly DependencyProperty VerticalScrollModeProperty =
			DependencyProperty.RegisterAttached(
				"VerticalScrollMode",
				typeof(ScrollMode),
				typeof(ScrollViewer),
				new FrameworkPropertyMetadata(
					ScrollMode.Enabled,
					propertyChangedCallback: OnVerticalScrollModeChanged,
					options: FrameworkPropertyMetadataOptions.Inherits
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

		#region BringIntoViewOnFocusChange (Attached DP - inherited)
#if __IOS__
		[global::Uno.NotImplemented]
#endif
		public static bool GetBringIntoViewOnFocusChange( global::Windows.UI.Xaml.DependencyObject element)
			=> (bool)element.GetValue(BringIntoViewOnFocusChangeProperty);

#if __IOS__
		[global::Uno.NotImplemented]
#endif
		public static void SetBringIntoViewOnFocusChange( global::Windows.UI.Xaml.DependencyObject element,  bool bringIntoViewOnFocusChange)
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
		public static ZoomMode GetZoomMode(DependencyObject obj)
			=> (ZoomMode)obj.GetValue(ZoomModeProperty);

		public static void SetZoomMode(DependencyObject obj, ZoomMode value)
			=> obj.SetValue(ZoomModeProperty, value);

		public ZoomMode ZoomMode
		{
			get => (ZoomMode)GetValue(ZoomModeProperty);
			set => SetValue(ZoomModeProperty, value);
		}

		public static readonly DependencyProperty ZoomModeProperty =
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

		public static readonly DependencyProperty MinZoomFactorProperty =
			DependencyProperty.Register("MinZoomFactor", typeof(float), typeof(ScrollViewer), new PropertyMetadata(0.1f, (o, e) => ((ScrollViewer)o).OnMinZoomFactorChanged(e)));

		private void OnMinZoomFactorChanged(DependencyPropertyChangedEventArgs args)
		{
			_sv?.OnMinZoomFactorChanged((float)args.NewValue);
		}
		#endregion

		#region MaxZoomFactor (DP)
		public float MaxZoomFactor
		{
			get => (float)GetValue(MaxZoomFactorProperty);
			set => SetValue(MaxZoomFactorProperty, value);
		}

		public static readonly DependencyProperty MaxZoomFactorProperty =
			DependencyProperty.Register("MaxZoomFactor", typeof(float), typeof(ScrollViewer), new PropertyMetadata(10f, (o, e) => ((ScrollViewer)o).OnMaxZoomFactorChanged(e)));

		private void OnMaxZoomFactorChanged(DependencyPropertyChangedEventArgs args)
		{
			_sv?.OnMaxZoomFactorChanged((float)args.NewValue);
		}
		#endregion

		#region ZoomFactor (DP - readonly)
		public float ZoomFactor
		{
			get => (float)GetValue(ZoomFactorProperty);
			private set { SetValue(ZoomFactorProperty, value); }
		}

		public static readonly DependencyProperty ZoomFactorProperty =
			DependencyProperty.Register("ZoomFactor", typeof(float), typeof(ScrollViewer), new PropertyMetadata(1f));
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
				new PropertyMetadata(
					(double)0,
					null
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
				new PropertyMetadata(
					(double)0,
					null
				)
			);
		#endregion

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

#if !NET461
		/// <summary>
		/// Sets the content of the ScrollViewer
		/// </summary>
		/// <param name="view"></param>
		/// <remarks>Used in the context of member initialization</remarks>
		public
#if !__WASM__ && !__MACOS__
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

			ApplyScrollContentPresenterContent();

			OnApplyTemplatePartial();

			// Apply correct initial zoom settings
			OnZoomModeChanged(ZoomMode);

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
			StopContentTemplatedParentPropagation();

			_sv.Content = Content as View;

			// Propagate the ScrollViewer's own templated parent, instead of 
			// the scrollviewer itself (through ScrollContentPreset
			SynchronizeContentTemplatedParent(TemplatedParent);
		}

		private void StopContentTemplatedParentPropagation()
		{
			if (Content is IDependencyObjectStoreProvider provider)
			{
				provider.Store.SetValue(provider.Store.TemplatedParentProperty, null, DependencyPropertyValuePrecedences.Local);
			}
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

		/// <summary>
		/// If this flag is enabled, the ScrollViewer will report offsets less than 0 and greater than <see cref="ScrollableHeight"/> when 
		/// 'overscrolling' on iOS. By default this is false, matching Windows behaviour.
		/// </summary>
		[UnoOnly]
		public bool ShouldReportNegativeOffsets { get; set; } = false;

		/// <summary>
		/// Cached value of <see cref="Uno.UI.Xaml.Controls.ScrollViewer.UpdatesModeProperty"/>,
		/// in order to not access the DP on each scroll (perf considerations)
		/// </summary>
		internal Uno.UI.Xaml.Controls.ScrollViewerUpdatesMode UpdatesMode { get; set; }

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
