using System;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

#if XAMARIN_IOS
using Foundation;
using UIKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class Slider : RangeBase
	{
		private ContentPresenter _headerContentPresenter;
		private Thumb _horizontalThumb, _verticalThumb;
		private FrameworkElement _horizontalTemplate, _verticalTemplate;
		private bool _isDragging; // between DragStart and DragCompleted
		private bool _isInDragDelta; // is reacting to a DragDelta
		private Rectangle _horizontalDecreaseRect;
		private Rectangle _horizontalTrackRect;
		private Rectangle _verticalDecreaseRect;
		private Rectangle _verticalTrackRect;
		private FrameworkElement _sliderContainer;
		private double _horizontalInitial = 0, _verticalInitial = 0;

		private Thumb Thumb => Orientation == Orientation.Horizontal ? _horizontalThumb : _verticalThumb;

		private readonly SerialDisposable _sliderContainerSubscription = new SerialDisposable();
		private readonly SerialDisposable _eventSubscriptions = new SerialDisposable();

		static Slider()
		{
			MaximumProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(100d));
			SmallChangeProperty.OverrideMetadata(typeof(Slider), new FrameworkPropertyMetadata(1d));
		}

		public Slider()
		{
#if XAMARIN
			RegisterLoadActions(SubscribeSliderContainerPressed, () => _sliderContainerSubscription.Disposable = null);
#endif

			DefaultStyleKey = typeof(Slider);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Dispose previously registered handlers if any
			_eventSubscriptions.Disposable = null;

			_headerContentPresenter = GetTemplateChild("HeaderContentPresenter") as ContentPresenter;
			if (_headerContentPresenter != null)
			{
				UpdateHeaderVisibility();
			}

			_horizontalThumb = GetTemplateChild("HorizontalThumb") as Thumb;
			_verticalThumb = GetTemplateChild("VerticalThumb") as Thumb;

			_horizontalThumb?.DisablePointersTracking();
			_verticalThumb?.DisablePointersTracking();

			_verticalTemplate = GetTemplateChild("VerticalTemplate") as FrameworkElement;
			_verticalTrackRect = GetTemplateChild("VerticalTrackRect") as Rectangle;
			_verticalDecreaseRect = GetTemplateChild("VerticalDecreaseRect") as Rectangle;

			_horizontalTemplate = GetTemplateChild("HorizontalTemplate") as FrameworkElement;
			_horizontalTrackRect = GetTemplateChild("HorizontalTrackRect") as Rectangle;
			_horizontalDecreaseRect = GetTemplateChild("HorizontalDecreaseRect") as Rectangle;
			_sliderContainer = GetTemplateChild("SliderContainer") as FrameworkElement;

			if (!IsLoaded)
			{
				_eventSubscriptions.Disposable = RegisterHandlers();
			}

			if (HasXamlTemplate)
			{
				SizeChanged += (s, e) => ApplyValueToSlide();
				ApplyValueToSlide();
			}

			UpdateCommonState(useTransitions: false);
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			if (_eventSubscriptions.Disposable == null)
			{
				_eventSubscriptions.Disposable = RegisterHandlers();
			}

			if (_sliderContainer != null)
			{
				if (_sliderContainer.Background == null)
				{
					//Set background to ensure touch events are captured, but allow it to be overwritten by bindings etc
					_sliderContainer.SetValue(BackgroundProperty, SolidColorBrushHelper.Transparent, DependencyPropertyValuePrecedences.ImplicitStyle);
				}
				SubscribeSliderContainerPressed();
			}

			UpdateOrientation(Orientation);
		}

		private bool HasXamlTemplate => _horizontalThumb != null || _verticalThumb != null;

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_eventSubscriptions.Disposable = null;
		}

		private IDisposable RegisterHandlers()
		{
			// Setup the thumbs event listeners
			if (_horizontalThumb != null)
			{
				_horizontalThumb.DragStarted += OnDragStarted;
				_horizontalThumb.DragDelta += OnDragDelta;
				_horizontalThumb.DragCompleted += OnDragCompleted;
			}

			if (_verticalThumb != null)
			{
				_verticalThumb.DragStarted += OnDragStarted;
				_verticalThumb.DragDelta += OnDragDelta;
				_verticalThumb.DragCompleted += OnDragCompleted;
			}

			return Disposable.Create(() =>
			{
				// Dispose of the thumbs event listeners
				if (_horizontalThumb != null)
				{
					_horizontalThumb.DragStarted -= OnDragStarted;
					_horizontalThumb.DragDelta -= OnDragDelta;
					_horizontalThumb.DragCompleted -= OnDragCompleted;
				}

				if (_verticalThumb != null)
				{
					_verticalThumb.DragStarted -= OnDragStarted;
					_verticalThumb.DragDelta -= OnDragDelta;
					_verticalThumb.DragCompleted -= OnDragCompleted;
				}
			});
		}

		private void OnDragCompleted(object sender, DragCompletedEventArgs args)
		{
			ApplyValueToSlide();

			_isDragging = false;
			UpdateCommonState();
		}

		private void OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			try
			{
				_isInDragDelta = true;

				if (Orientation == Orientation.Horizontal)
				{
					var maxWidth = CalculateMaxWidth(ActualWidth, GetHorizontalThumbWidth());

					_horizontalDecreaseRect.Width = Math.Min(Math.Max(0, _horizontalInitial + (float)e.TotalHorizontalChange), maxWidth);

					ApplySlideToValue(_horizontalDecreaseRect.Width / maxWidth);
				}
				else 
				{
					var thumbHeight = GetVerticalThumbHeight();
					var sliderHeight = GetSliderHeight();
					var maxHeight = CalculateMaxHeight(sliderHeight, thumbHeight);
					var delta = -e.TotalVerticalChange;

					_verticalDecreaseRect.Height = Math.Min(Math.Max(0, _verticalInitial + delta), maxHeight);

					ApplySlideToValue(_verticalDecreaseRect.Height / maxHeight);
				}
			}
			finally
			{
				_isInDragDelta = false;
			}
		}

		/// <summary>
		/// Gets the slider height
		/// </summary>
		private double GetSliderHeight()
		{
			var result = ActualHeight;

			// On android and ios the issue raised here: https://github.com/unoplatform/uno/issues/3812
			// is not fully resolved. The ActualSize is not consistent. The error would need
			// a rework of Grid. When this work is done, the following workaround should not be
			// needed anymore.
#if __ANDROID__ || __IOS__
			var maxHeightWithThumb = result;

			if (!double.IsInfinity(Height) && !double.IsNaN(Height))
			{
				result = Height;
			}
			if (!double.IsInfinity(MinHeight) && !double.IsNaN(MinHeight) && MinHeight > result)
			{
				result = MinHeight;
			}
			else if (!double.IsInfinity(MaxHeight) && !double.IsNaN(MaxHeight) && MaxHeight < result)
			{
				result = MaxHeight;
			}

			if (result > maxHeightWithThumb)
			{
				result = maxHeightWithThumb;
			}
#endif
			return result;
		}

		private void OnDragStarted(object sender, DragStartedEventArgs args)
		{
			if (HasXamlTemplate)
			{
				if (Orientation == Orientation.Horizontal)
				{
					_horizontalInitial = GetSanitizedDimension(_horizontalDecreaseRect.Width);
				}
				else
				{
					_verticalInitial = GetSanitizedDimension(_verticalDecreaseRect.Height);
				}

				_isDragging = true;
				UpdateCommonState();
			}
		}

		private void UpdateCommonState(bool useTransitions = true)
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", useTransitions);
			}
			else if (_isDragging)
			{
				VisualStateManager.GoToState(this, "Pressed", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "Normal", useTransitions);
			}
		}

		/// <summary>
		/// Replace NaN with 0, because some of the children of this control sometimes have NaN dimensions,
		/// which messes with our calculations. 
		/// </summary>
		private double GetSanitizedDimension(double dimensionValue)
		{
			if (double.IsNaN(dimensionValue))
			{
				return 0;
			}

			return dimensionValue;
		}

		/// <summary>
		/// Take the relative position of the slider and update the Value property accordingly
		/// </summary>
		/// <param name="slideFraction">Proportion of the total Slider by which the Thumb has advanced</param>
		private void ApplySlideToValue(double slideFraction)
		{
			var value = slideFraction * (Maximum - Minimum) + Minimum;

			var snapFrequency = GetSnapFrequency();

			if (snapFrequency <= 0 || double.IsNaN(snapFrequency))
			{
				throw new ArgumentException("Value does not fall within the expected range.");
			}

			var distanceToPreviousSnapValue = (value - Minimum) % snapFrequency;

			// If closer to the previous snap value, go to this previous snap value
			if (distanceToPreviousSnapValue < snapFrequency / 2)
			{
				value -= distanceToPreviousSnapValue;
			}
			else
			{
				value += (snapFrequency - distanceToPreviousSnapValue);
			}

			Value = value;
		}

		/// <summary>
		/// Take the given value and move the slider accordingly.
		/// </summary>
		private void ApplyValueToSlide()
		{
			// The _decreaseRect's height/width is updated, which in turn pushes or pulls the Thumb to its correct position
			if (Orientation == Orientation.Horizontal)
			{
				if (_horizontalThumb != null && _horizontalDecreaseRect != null)
				{
					var maxWidth = CalculateMaxWidth(ActualWidth, GetHorizontalThumbWidth());
					_horizontalDecreaseRect.Width = (Value - Minimum) / (Maximum - Minimum) * maxWidth;
				}
			}
			else
			{
				if (_verticalThumb != null && _verticalDecreaseRect != null)
				{
					var thumbHeight = GetVerticalThumbHeight();
					var sliderHeight = GetSliderHeight();
					var maxHeight = CalculateMaxHeight(sliderHeight, thumbHeight);

					_verticalDecreaseRect.Height = (Value - Minimum) / (Maximum - Minimum) * maxHeight;
				}
			}
		}

		private double GetHorizontalThumbWidth()
		{
			// In some cases, because of the timing of SizeChanged, this may be called before the thumb has been measured. If so, we rely on the fact
			// that the dimensions are hard-coded by the UWP and Fluent styles.
			if (_horizontalThumb.ActualWidth == 0 && !double.IsNaN(_horizontalThumb.Width))
			{
				return _horizontalThumb.Width;
			}

			return _horizontalThumb.ActualWidth;
		}

		private double GetVerticalThumbHeight()
		{
			// In some cases, because of the timing of SizeChanged, this may be called before the thumb has been measured. If so, we rely on the fact
			// that the dimensions are hard-coded by the UWP and Fluent styles.
			if (_verticalThumb.ActualHeight == 0 && !double.IsNaN(_verticalThumb.Height))
			{
				return _verticalThumb.Height;
			}

			return _verticalThumb.ActualHeight;
		}

		/// <summary>
		/// Calculate the max width
		/// </summary>
		/// <remarks>
		/// If the thumb is as big as the control,
		/// the thumb should be able to move but only a little bit to mimic UWP behavior.
		/// </remarks>
		/// <param name="sliderWidth">The slider height. Needed to determine if the thumb should move or not.</param>
		/// <param name="thumbWidth">The thumb height. Needed to determine if the thumb should move or not.</param>
		private double CalculateMaxWidth(double sliderWidth, double thumbWidth)
		{
			var result = sliderWidth - thumbWidth;
			var maxWidthShouldBeFixed = (result <= 0);
			if (maxWidthShouldBeFixed)
			{
				result = 0.1;
			}

			return result;
		}

		/// <summary>
		/// Calculate the controls max height
		/// </summary>
		/// <remarks>
		/// If the thumb is as big as the control,
		/// the thumb should be able to move but only a little bit to mimic UWP behavior.
		/// </remarks>
		/// <param name="sliderHeight">The slider height. Needed to determine if the thumb should move or not.</param>
		/// <param name="thumbHeight">The thumb height. Needed to determine if the thumb should move or not.</param>
		private double CalculateMaxHeight(double sliderHeight, double thumbHeight)
		{
			var result = sliderHeight - thumbHeight;
			var acceptableHeightDifference = 0;

			// On android and ios, when the thumb is the same size as the control or bigger
			// it causes the control to stretch verticaly. The actual height of the thumb
			// is not reliable and it often changes. The value of "5" for the acceptable difference
			// was obtain from multiple test but might not work for every cases. This is obviously
			// a work around has the issue is in either in the grid that makes up the slider or
			// in the measure or arrange phase.
#if __ANDROID__ || __IOS__
			acceptableHeightDifference = 5;
#endif
			var maxHeightShouldBeFixed = (result <= acceptableHeightDifference);
			if (maxHeightShouldBeFixed)
			{
				result = 0.1;
			}

			return result;
		}

		/// <summary>
		/// Get the snap frequency, given the current values of SnapsTo,
		/// StepFrequency and TickFrequency, and sanitizing the value
		/// so that it does not exceed the total range of Value.
		/// </summary>
		private double GetSnapFrequency()
		{
			var frequency = SnapsTo == SliderSnapsTo.StepValues ?
				StepFrequency
				: TickFrequency;

			return Math.Min(Maximum - Minimum, frequency);
		}

		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);

			if (!_isInDragDelta && HasXamlTemplate)
			{
				ApplyValueToSlide();
			}
		}

		private void SubscribeSliderContainerPressed()
		{
			// This allows the user to start sliding by clicking anywhere on the slider
			// In that case, the Thumb won't  be able to capture the pointer and instead we will replicate
			// its behavior and "push" to it the drag events (on which we are already subscribed).

			var container = _sliderContainer;
			if (container != null && IsTrackerEnabled)
			{
				_sliderContainerSubscription.Disposable = null;

				container.PointerPressed += OnSliderContainerPressed;
				container.PointerMoved += OnSliderContainerMoved;
				container.PointerCaptureLost += OnSliderContainerCaptureLost;

				_sliderContainerSubscription.Disposable = Disposable.Create(() =>
				{
					container.PointerPressed -= OnSliderContainerPressed;
					container.PointerMoved -= OnSliderContainerMoved;
					container.PointerCaptureLost -= OnSliderContainerCaptureLost;
				});
			}
		}

		private void OnSliderContainerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (sender is FrameworkElement container && container.CapturePointer(args.Pointer))
			{
				var point = args.GetCurrentPoint(container).Position;
				var newOffset = Orientation == Orientation.Horizontal
					? point.X / container.ActualWidth
					: 1 - (point.Y / container.ActualHeight);

				ApplySlideToValue(newOffset);
				Thumb?.StartDrag(args);
			}
		}

		private void OnSliderContainerMoved(object sender, PointerRoutedEventArgs args)
		{
			if (sender is FrameworkElement container && container.IsCaptured(args.Pointer))
			{
				Thumb?.DeltaDrag(args);
			}
		}

		private void OnSliderContainerCaptureLost(object sender, PointerRoutedEventArgs args)
		{
			ApplyValueToSlide();
			Thumb?.CompleteDrag(args);
		}

		private void UpdateHeaderVisibility()
		{
			if (_headerContentPresenter != null)
			{
				_headerContentPresenter.Visibility =
					Header != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		#region IsTrackerEnabled DependencyProperty
		/// <summary>
		/// Enables or disables tracking on the Slider container. <para />
		/// 
		/// When enabled, the Slider will intercept touch events on the entire container as well as the Thumb.
		/// This is the default value. <para />
		/// 
		/// When disabled, only the Thumb will intercept touch events. Therefore, the user cannot tap or drag
		/// on the bar to change the Slider's value. This is a better option in cases involving Sliders within
		/// a ScrollView, to avoid the Slider stealing the focus when the user tries to scroll.
		/// </summary>
		[Uno.UnoOnly]
		public bool IsTrackerEnabled
		{
			get { return (bool)GetValue(IsTrackerEnabledProperty); }
			set { SetValue(IsTrackerEnabledProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsTrackerEnabled.  This enables animation, styling, binding, etc...
		public static DependencyProperty IsTrackerEnabledProperty { get ; } =
			DependencyProperty.Register("IsTrackerEnabled", typeof(bool), typeof(Slider), new FrameworkPropertyMetadata(true));
		#endregion

		#region StepFrequency DependencyProperty

		public double StepFrequency
		{
			get { return (double)GetValue(StepFrequencyProperty); }
			set { SetValue(StepFrequencyProperty, value); }
		}

		public static DependencyProperty StepFrequencyProperty { get ; } =
			DependencyProperty.Register("StepFrequency", typeof(double), typeof(Slider), new FrameworkPropertyMetadata(1.0, (s, e) => ((Slider)s)?.OnStepFrequencyChanged(e)));

		private void OnStepFrequencyChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region Orientation DependencyProperty

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
		public static DependencyProperty OrientationProperty { get ; } =
			DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Slider), new FrameworkPropertyMetadata(Orientation.Horizontal, (s, e) => ((Slider)s)?.OnOrientationChanged(e)));


		private void OnOrientationChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is Orientation)
			{
				var newOrientation = (Orientation)e.NewValue;

				UpdateOrientation(newOrientation);
			}
			else
			{
				if (_horizontalTemplate != null)
				{
					_horizontalTemplate.Visibility = Visibility.Visible;
				}

				if (_verticalTemplate != null)
				{
					_verticalTemplate.Visibility = Visibility.Collapsed;
				}
			}
		}

		private void UpdateOrientation(Orientation newOrientation)
		{
			if (_horizontalTemplate != null)
			{
				_horizontalTemplate.Visibility = newOrientation == Orientation.Horizontal ? Visibility.Visible : Visibility.Collapsed;
			}

			if (_verticalTemplate != null)
			{
				_verticalTemplate.Visibility = newOrientation == Orientation.Vertical ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		#endregion

		#region SnapsTo DependencyProperty

		public SliderSnapsTo SnapsTo
		{
			get { return (SliderSnapsTo)GetValue(SnapsToProperty); }
			set { SetValue(SnapsToProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SnapsTo.  This enables animation, styling, binding, etc...
		public static DependencyProperty SnapsToProperty { get ; } =
			DependencyProperty.Register("SnapsTo", typeof(SliderSnapsTo), typeof(Slider), new FrameworkPropertyMetadata(SliderSnapsTo.StepValues, (s, e) => ((Slider)s)?.OnSnapsToChanged(e)));


		private void OnSnapsToChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region IsThumbToolTipEnabled DependencyProperty

		public bool IsThumbToolTipEnabled
		{
			get { return (bool)GetValue(IsThumbToolTipEnabledProperty); }
			set { SetValue(IsThumbToolTipEnabledProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsThumbToolTipEnabled.  This enables animation, styling, binding, etc...
		public static DependencyProperty IsThumbToolTipEnabledProperty { get ; } =
			DependencyProperty.Register("IsThumbToolTipEnabled", typeof(bool), typeof(Slider), new FrameworkPropertyMetadata(false, (s, e) => ((Slider)s)?.OnIsThumbToolTipEnabledChanged(e)));


		private void OnIsThumbToolTipEnabledChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region IsDirectionReversed DependencyProperty

		public bool IsDirectionReversed
		{
			get { return (bool)GetValue(IsDirectionReversedProperty); }
			set { SetValue(IsDirectionReversedProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IsDirectionReversed.  This enables animation, styling, binding, etc...
		public static DependencyProperty IsDirectionReversedProperty { get ; } =
			DependencyProperty.Register("IsDirectionReversed", typeof(bool), typeof(Slider), new FrameworkPropertyMetadata(false, (s, e) => ((Slider)s)?.OnIsDirectionReversedChanged(e)));


		private void OnIsDirectionReversedChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region IntermediateValue DependencyProperty

		public double IntermediateValue
		{
			get { return (double)GetValue(IntermediateValueProperty); }
			set { SetValue(IntermediateValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for IntermediateValue.  This enables animation, styling, binding, etc...
		public static DependencyProperty IntermediateValueProperty { get ; } =
			DependencyProperty.Register("IntermediateValue", typeof(double), typeof(Slider), new FrameworkPropertyMetadata(.5, (s, e) => ((Slider)s)?.OnIntermediateValueChanged(e)));


		private void OnIntermediateValueChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region TickPlacement DependencyProperty

		public TickPlacement TickPlacement
		{
			get { return (TickPlacement)GetValue(TickPlacementProperty); }
			set { SetValue(TickPlacementProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TickPlacement.  This enables animation, styling, binding, etc...
		public static DependencyProperty TickPlacementProperty { get ; } =
			DependencyProperty.Register("TickPlacement", typeof(TickPlacement), typeof(Slider), new FrameworkPropertyMetadata(TickPlacement.None, (s, e) => ((Slider)s)?.OnTickPlacementChanged(e)));


		private void OnTickPlacementChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region TickFrequency DependencyProperty

		public double TickFrequency
		{
			get { return (double)GetValue(TickFrequencyProperty); }
			set { SetValue(TickFrequencyProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TickFrequency.  This enables animation, styling, binding, etc...
		public static DependencyProperty TickFrequencyProperty { get ; } =
			DependencyProperty.Register("TickFrequency", typeof(double), typeof(Slider), new FrameworkPropertyMetadata(0.0, (s, e) => ((Slider)s)?.OnTickFrequencyChanged(e)));


		private void OnTickFrequencyChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region ThumbToolTipValueConverter DependencyProperty

		public IValueConverter ThumbToolTipValueConverter
		{
			get { return (IValueConverter)GetValue(ThumbToolTipValueConverterProperty); }
			set { SetValue(ThumbToolTipValueConverterProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ThumbToolTipValueConverter.  This enables animation, styling, binding, etc...
		public static DependencyProperty ThumbToolTipValueConverterProperty { get ; } =
			DependencyProperty.Register("ThumbToolTipValueConverter", typeof(IValueConverter), typeof(Slider), new FrameworkPropertyMetadata(null, (s, e) => ((Slider)s)?.OnThumbToolTipValueConverterChanged(e)));


		private void OnThumbToolTipValueConverterChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region HeaderTemplate DependencyProperty

		public DataTemplate HeaderTemplate
		{
			get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
			set { SetValue(HeaderTemplateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HeaderTemplate.  This enables animation, styling, binding, etc...
		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(Slider), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, (s, e) => ((Slider)s)?.OnHeaderTemplateChanged(e)));


		private void OnHeaderTemplateChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region Header DependencyProperty

		public object Header
		{
			get { return (object)GetValue(HeaderProperty); }
			set { SetValue(HeaderProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Header.  This enables animation, styling, binding, etc...
		public static DependencyProperty HeaderProperty { get ; } =
			DependencyProperty.Register("Header", typeof(object), typeof(Slider), new FrameworkPropertyMetadata(null, (s, e) => ((Slider)s)?.OnHeaderChanged(e)));

		private void OnHeaderChanged(DependencyPropertyChangedEventArgs e) =>
			UpdateHeaderVisibility();

		#endregion
	}
}

