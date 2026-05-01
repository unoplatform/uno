#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollViewer
	{
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
	}
}
