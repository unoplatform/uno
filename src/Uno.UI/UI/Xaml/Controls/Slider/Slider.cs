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
		private bool _duringDrag;
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
			MaximumProperty.OverrideMetadata(typeof(Slider), new PropertyMetadata(100d));
			SmallChangeProperty.OverrideMetadata(typeof(Slider), new PropertyMetadata(1d));
		}

		public Slider()
		{
#if XAMARIN
			RegisterLoadActions(SubscribeSliderContainerPressed, () => _sliderContainerSubscription.Disposable = null);
#endif
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Dispose previously registered handlers if any
			_eventSubscriptions.Disposable = null;

			_headerContentPresenter = GetTemplateChild("HeaderContentPresenter") as ContentPresenter;
			_horizontalThumb = GetTemplateChild("HorizontalThumb") as Thumb;
			_verticalThumb = GetTemplateChild("VerticalThumb") as Thumb;

			if (_horizontalThumb != null)
			{
				_horizontalThumb.ShouldCapturePointer = false;
			}

			if (_verticalThumb != null)
			{
				_verticalThumb.ShouldCapturePointer = false;
			}

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

		protected override void OnLoaded()
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

		protected override void OnUnloaded()
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

			// Add the gesture recognizer necessary for touch event captures on iOS
			RegisterNativeHandlers();

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

				// Get rid of the iOS gesture recognizer
				UnregisterNativeHandlers();
			});
		}

		partial void RegisterNativeHandlers();

		partial void UnregisterNativeHandlers();

		private void OnDragCompleted(object sender, DragCompletedEventArgs args)
		{
			ApplyValueToSlide();

			IsPointerPressed = false;
			UpdateCommonState();
		}

		private void OnDragDelta(object sender, DragDeltaEventArgs e)
		{
			try
			{
				_duringDrag = true;

				if (Orientation == Orientation.Horizontal)
				{
					var maxWidth = ActualWidth - _horizontalThumb.ActualWidth;

					_horizontalDecreaseRect.Width = Math.Min(Math.Max(0, _horizontalInitial + (float)e.HorizontalChange), maxWidth);

					ApplySlideToValue(_horizontalDecreaseRect.Width / maxWidth);
				}
				else
				{
					var maxHeight = ActualHeight - _horizontalThumb.ActualHeight;

					_verticalDecreaseRect.Height = Math.Min(Math.Max(0, _verticalInitial - (float)e.VerticalChange), (float)maxHeight);

					ApplySlideToValue(_verticalDecreaseRect.Height / maxHeight);
				}
			}
			finally
			{
				_duringDrag = false;
			}
		}

		private void OnDragStarted(object sender, DragStartedEventArgs args)
		{
			if (HasXamlTemplate)
			{
				// Disable parent scrolling on Android
#if XAMARIN_ANDROID
				this.RequestDisallowInterceptTouchEvent(true);
#endif
				if (Orientation == Orientation.Horizontal)
				{
					_horizontalInitial = GetSanitizedDimension(_horizontalDecreaseRect.Width);
				}
				else
				{
					_verticalInitial = GetSanitizedDimension(_verticalDecreaseRect.Height);
				}

				IsPointerPressed = true;
				UpdateCommonState();
			}
		}

		private void UpdateCommonState(bool useTransitions = true)
		{
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", useTransitions);
			}
			else if (IsPointerPressed)
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
                    var maxWidth = ActualWidth - _horizontalThumb.ActualWidth;
                    _horizontalDecreaseRect.Width = (float)((Value - Minimum) / (Maximum - Minimum)) * maxWidth;
                }
			}
			else
			{
                if (_verticalThumb != null && _verticalDecreaseRect != null)
                {
                    var maxHeight = ActualHeight - _verticalThumb.ActualHeight;
                    _verticalDecreaseRect.Height = (float)((Value - Minimum) / (Maximum - Minimum)) * maxHeight;
                }
			}
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

			if (!_duringDrag && HasXamlTemplate)
			{
				ApplyValueToSlide();
			}
		}

		private void SubscribeSliderContainerPressed()
		{
			if (_sliderContainer != null && IsTrackerEnabled)
			{
				_sliderContainer.PointerPressed += OnSliderContainerPressed;
				_sliderContainer.PointerMoved += OnSliderContainerMoved;
				_sliderContainer.PointerReleased += OnSliderContainerReleased;
				_sliderContainer.PointerCanceled += OnSliderContainerCanceled;
				_sliderContainerSubscription.Disposable = Disposable.Create(() =>
				{
					_sliderContainer.PointerPressed -= OnSliderContainerPressed;
					_sliderContainer.PointerMoved -= OnSliderContainerMoved;
					_sliderContainer.PointerReleased -= OnSliderContainerReleased;
					_sliderContainer.PointerCanceled -= OnSliderContainerCanceled;
				});
			}
		}

		private void OnSliderContainerPressed(object sender, PointerRoutedEventArgs e)
		{
			var container = sender as FrameworkElement;
			var point = e.GetCurrentPoint(container).Position;

			var newOffset = Orientation == Orientation.Horizontal ?
				point.X / container.ActualWidth :
				point.Y / container.ActualHeight;

			ApplySlideToValue(newOffset);

			Thumb?.StartDrag(point);

			// This captures downstream events outside the slider's bounds.
			container.CapturePointer(e.Pointer);

			// This is currently obligatory on Android to be able to receive downstream touch events (eg PointerMoved etc).
			e.Handled = true;
		}

		private void OnSliderContainerCanceled(object sender, PointerRoutedEventArgs e)
		{
			OnDragCompleted(sender, null);

			var container = sender as FrameworkElement;
			container.ReleasePointerCapture(e.Pointer);
		}

		private void OnSliderContainerReleased(object sender, PointerRoutedEventArgs e)
		{
			var container = sender as FrameworkElement;
			var point = e.GetCurrentPoint(container).Position;

			ApplyValueToSlide();

			Thumb?.CompleteDrag(point);

			container.ReleasePointerCapture(e.Pointer);

			e.Handled = true;
		}

		private void OnSliderContainerMoved(object sender, PointerRoutedEventArgs e)
		{
			var container = sender as FrameworkElement;
#if __WASM__
			if (!container.IsPointerCaptured)
			{
				return;
			}
#endif

			var point = e.GetCurrentPoint(container).Position;

			Thumb?.DeltaDrag(point);

			e.Handled = true;
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
		public static readonly DependencyProperty IsTrackerEnabledProperty =
			DependencyProperty.Register("IsTrackerEnabled", typeof(bool), typeof(Slider), new PropertyMetadata(true));
		#endregion

		#region StepFrequency DependencyProperty

		public double StepFrequency
		{
			get { return (double)GetValue(StepFrequencyProperty); }
			set { SetValue(StepFrequencyProperty, value); }
		}

		public static readonly DependencyProperty StepFrequencyProperty =
			DependencyProperty.Register("StepFrequency", typeof(double), typeof(Slider), new PropertyMetadata(1.0, (s, e) => ((Slider)s)?.OnStepFrequencyChanged(e)));

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
		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Slider), new PropertyMetadata(Orientation.Horizontal, (s, e) => ((Slider)s)?.OnOrientationChanged(e)));


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
		public static readonly DependencyProperty SnapsToProperty =
			DependencyProperty.Register("SnapsTo", typeof(SliderSnapsTo), typeof(Slider), new PropertyMetadata(SliderSnapsTo.StepValues, (s, e) => ((Slider)s)?.OnSnapsToChanged(e)));


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
		public static readonly DependencyProperty IsThumbToolTipEnabledProperty =
			DependencyProperty.Register("IsThumbToolTipEnabled", typeof(bool), typeof(Slider), new PropertyMetadata(false, (s, e) => ((Slider)s)?.OnIsThumbToolTipEnabledChanged(e)));


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
		public static readonly DependencyProperty IsDirectionReversedProperty =
			DependencyProperty.Register("IsDirectionReversed", typeof(bool), typeof(Slider), new PropertyMetadata(false, (s, e) => ((Slider)s)?.OnIsDirectionReversedChanged(e)));


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
		public static readonly DependencyProperty IntermediateValueProperty =
			DependencyProperty.Register("IntermediateValue", typeof(double), typeof(Slider), new PropertyMetadata(.5, (s, e) => ((Slider)s)?.OnIntermediateValueChanged(e)));


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
		public static readonly DependencyProperty TickPlacementProperty =
			DependencyProperty.Register("TickPlacement", typeof(TickPlacement), typeof(Slider), new PropertyMetadata(TickPlacement.None, (s, e) => ((Slider)s)?.OnTickPlacementChanged(e)));


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
		public static readonly DependencyProperty TickFrequencyProperty =
			DependencyProperty.Register("TickFrequency", typeof(double), typeof(Slider), new PropertyMetadata(0.0, (s, e) => ((Slider)s)?.OnTickFrequencyChanged(e)));


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
		public static readonly DependencyProperty ThumbToolTipValueConverterProperty =
			DependencyProperty.Register("ThumbToolTipValueConverter", typeof(IValueConverter), typeof(Slider), new PropertyMetadata(null, (s, e) => ((Slider)s)?.OnThumbToolTipValueConverterChanged(e)));


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
		public static readonly DependencyProperty HeaderTemplateProperty =
			DependencyProperty.Register("HeaderTemplate", typeof(DataTemplate), typeof(Slider), new PropertyMetadata(null, (s, e) => ((Slider)s)?.OnHeaderTemplateChanged(e)));


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
		public static readonly DependencyProperty HeaderProperty =
			DependencyProperty.Register("Header", typeof(object), typeof(Slider), new PropertyMetadata(null, (s, e) => ((Slider)s)?.OnHeaderChanged(e)));

		private void OnHeaderChanged(DependencyPropertyChangedEventArgs e)
		{
			if (_headerContentPresenter != null)
			{
				_headerContentPresenter.Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		#endregion
	}
}

