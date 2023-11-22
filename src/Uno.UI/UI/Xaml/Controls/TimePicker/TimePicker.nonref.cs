#if !UNO_REFERENCE_API
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.Globalization;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePicker : Control
	{
		public const string FlyoutButtonPartName = "FlyoutButton";
		public const string HourTextBlockPartName = "HourTextBlock";
		public const string MinuteTextBlockPartName = "MinuteTextBlock";
		public const string PeriodTextBlockPartName = "PeriodTextBlock"; //Aka AM/PM
		public const string ThirdTextBlockColumnPartName = "ThirdTextBlockColumn"; //AM/PM column
		public const string FlyoutButtonContentGridPartName = "FlyoutButtonContentGrid";

		private const string HeaderContentPresenterPartName = "HeaderContentPresenter";

		private Button _flyoutButton;
		private TextBlock _hourTextBlock;
		private TextBlock _minuteTextBlock;
		private TextBlock _periodTextBlock;
		private Grid _flyoutButtonContentGrid;
		private ColumnDefinition _thirdTextBlockColumn;
		private ContentPresenter _headerContentPresenter;
		private bool _isLoaded;
		private bool _isViewReady;
		private bool _bypassTimeAndSelectedTimeChanged;

		public event EventHandler<TimePickerValueChangedEventArgs> TimeChanged;
		public event TypedEventHandler<TimePicker, TimePickerSelectedValueChangedEventArgs> SelectedTimeChanged;

		public TimePicker()
		{
			ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "TimePickerLightDismissOverlayBackground", isThemeResourceExtension: true, isHotReloadSupported: false);

			DefaultStyleKey = typeof(TimePicker);
			InitPartial();
		}

		#region Time DependencyProperty

		public TimeSpan Time
		{
			get => (TimeSpan)this.GetValue(TimeProperty);
			set
			{
				ValidateTimeSpanForTimeProperty(value);
				this.SetValue(TimeProperty, value);
			}
		}

		// TODO:MZ Attach the Coerce and PropertyChanged properly
		//public static DependencyProperty TimeProperty { get; } =
		//	DependencyProperty.Register(
		//		nameof(Time),
		//		typeof(TimeSpan),
		//		typeof(TimePicker),
		//		new FrameworkPropertyMetadata(
		//			defaultValue: new TimeSpan(-1),
		//			options: FrameworkPropertyMetadataOptions.None,
		//			propertyChangedCallback: (s, e) => ((TimePicker)s)?.OnTimeChanged((TimeSpan)e.OldValue, (TimeSpan)e.NewValue),
		//			coerceValueCallback: (s, e, _) => ((TimePicker)s)?.CoerceTime((TimeSpan)e))
		//		);

		private static void ValidateTimeSpanForTimeProperty(TimeSpan span)
		{
			if (span < new TimeSpan(-1))
			{
				throw new ArgumentException("Value does not fall within the expected range.");
			}
		}

		private TimeSpan CoerceTime(TimeSpan ts)
		{
			ValidateTimeSpanForTimeProperty(ts);
			if (ts == new TimeSpan(-1))
			{
				return ts;
			}
			return new TimeSpan(ts.Days, ts.Hours, ts.Minutes, 0);
		}
		#endregion

		#region SelectedTime DependencyProperty
		public TimeSpan? SelectedTime
		{
			get => (TimeSpan?)this.GetValue(SelectedTimeProperty);
			set
			{
				ValidateTimeSpanForSelectedTimeProperty(value);
				this.SetValue(SelectedTimeProperty, value);
			}
		}

		// TODO:MZ: Attach methods
		//public static DependencyProperty SelectedTimeProperty { get; } =
		//	DependencyProperty.Register(
		//		nameof(SelectedTime),
		//		typeof(TimeSpan?),
		//		typeof(TimePicker),
		//		new FrameworkPropertyMetadata(
		//			defaultValue: null,
		//			FrameworkPropertyMetadataOptions.None,
		//			propertyChangedCallback: (s, e) => ((TimePicker)s)?.OnSelectedTimeChanged((TimeSpan?)e.OldValue, (TimeSpan?)e.NewValue),
		//			coerceValueCallback: (s, e, _) => ((TimePicker)s)?.CoerceSelectedTime((TimeSpan?)e))
		//		);

		private static void ValidateTimeSpanForSelectedTimeProperty(TimeSpan? span)
		{
			if (span.HasValue)
			{
				ValidateTimeSpanForTimeProperty(span.Value);
			}
		}

		private TimeSpan? CoerceSelectedTime(TimeSpan? ts)
		{
			ValidateTimeSpanForSelectedTimeProperty(ts);
			return ts;
		}
		#endregion

		#region MinuteIncrement DependencyProperty

		public int MinuteIncrement
		{
			get => (int)this.GetValue(MinuteIncrementProperty);
			set => this.SetValue(MinuteIncrementProperty, value);
		}

		public static DependencyProperty MinuteIncrementProperty { get; } =
			DependencyProperty.Register(
				nameof(MinuteIncrement),
				typeof(int),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(
					defaultValue: 1,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((TimePicker)s)?.OnMinuteIncrementChanged((int)e.OldValue, (int)e.NewValue),
					coerceValueCallback: (s, e, _) =>
					{
						var value = (int)e;

						if (value < 1)
							return 1;

						if (value > 30)
							return 30;

						return value;
					})
				);

		#endregion

		#region FlyoutPlacement DependencyProperty

		public FlyoutPlacementMode FlyoutPlacement
		{
			get => (FlyoutPlacementMode)this.GetValue(FlyoutPlacementProperty);
			set => this.SetValue(FlyoutPlacementProperty, value);
		}

		public static DependencyProperty FlyoutPlacementProperty { get; } =
			DependencyProperty.Register(
				nameof(FlyoutPlacement),
				typeof(FlyoutPlacementMode),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(FlyoutPlacementMode.Full));

		#endregion

		#region FlyoutPresenterStyle DependencyProperty
		// FlyoutPresenterStyle is an Uno-only property to allow the styling of the TimePicker's FlyoutPresenter.
		public Style FlyoutPresenterStyle
		{
			get => (Style)this.GetValue(FlyoutPresenterStyleProperty);
			set => this.SetValue(FlyoutPresenterStyleProperty, value);
		}

		public static DependencyProperty FlyoutPresenterStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(FlyoutPresenterStyle),
				typeof(Style),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(
					default(Style),
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		#endregion

		#region HeaderTemplate DependencyProperty

		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplate), typeof(DataTemplate),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		#endregion

		#region Header DependencyProperty

		public object Header
		{
			get => GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header), typeof(object),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(null, (s, e) => ((TimePicker)s)?.OnHeaderChanged(e)));

		private void OnHeaderChanged(DependencyPropertyChangedEventArgs e) =>
			UpdateHeaderVisibility();

		#endregion

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get => (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			set => this.SetValue(LightDismissOverlayModeProperty, value);
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
			DependencyProperty.Register(
				nameof(LightDismissOverlayMode), typeof(LightDismissOverlayMode),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

		/// <summary>
		/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc, static resource values.
		/// </summary>
		internal Brush LightDismissOverlayBackground
		{
			get => (Brush)GetValue(LightDismissOverlayBackgroundProperty);
			set => SetValue(LightDismissOverlayBackgroundProperty, value);
		}

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get; } =
			DependencyProperty.Register(nameof(LightDismissOverlayBackground), typeof(Brush), typeof(TimePicker), new FrameworkPropertyMetadata(null));

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_flyoutButton = this.GetTemplateChild(FlyoutButtonPartName) as Button;

			var flyoutContent = _flyoutButton?.Content as IFrameworkElement;

			_hourTextBlock = flyoutContent?.GetTemplateChild(HourTextBlockPartName) as TextBlock;
			_minuteTextBlock = flyoutContent?.GetTemplateChild(MinuteTextBlockPartName) as TextBlock;
			_periodTextBlock = flyoutContent?.GetTemplateChild(PeriodTextBlockPartName) as TextBlock;
			_flyoutButtonContentGrid = flyoutContent?.GetTemplateChild(FlyoutButtonContentGridPartName) as Grid;

			_headerContentPresenter = GetTemplateChild(HeaderContentPresenterPartName) as ContentPresenter;
			if (_headerContentPresenter != null)
			{
				UpdateHeaderVisibility();
			}

			var columns = _flyoutButtonContentGrid?.ColumnDefinitions as DefinitionCollectionBase;
			const int periodColumnPosition = 4;
			if ((columns?.Count ?? 0) > periodColumnPosition)
			{
				// This is a workaround for the lack of support for GetTemplateChild on non-IFrameworkElement types. (Bug #26303)
				_thirdTextBlockColumn = columns?.GetItem(periodColumnPosition) as ColumnDefinition;
			}

			_isViewReady = true;

			SetupFlyoutButton();
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_isLoaded = true;

			SetupFlyoutButton();
		}

		private void SetupFlyoutButton()
		{
			if (!_isViewReady || !_isLoaded)
			{
				return;
			}

			UpdateDisplayedDate();

			if (_flyoutButton != null)
			{
#if __IOS__ || __ANDROID__
				_flyoutButton.Flyout = new TimePickerFlyout
				{
#if __IOS__
					Placement = FlyoutPlacement,
					TimePickerFlyoutPresenterStyle = this.FlyoutPresenterStyle,
#endif
					Time = this.Time,
					MinuteIncrement = this.MinuteIncrement,
					ClockIdentifier = this.ClockIdentifier
				};

				_flyoutButton.Flyout.Opening += Flyout_Opening;
				((TimePickerFlyout)_flyoutButton.Flyout).TimePicked += TimePicker_TimePicked;

				BindToFlyout(nameof(MinuteIncrement));
				BindToFlyout(nameof(ClockIdentifier));
				_flyoutButton.Flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
				_flyoutButton.Flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));

				void Flyout_Opening(object sender, object e)
				{
					if (sender is TimePickerFlyout timePickerFlyout)
					{
						var time = Time;
						timePickerFlyout.Time = time == new TimeSpan(-1) ? DateTime.Now.TimeOfDay : time;
					}
				}

				void TimePicker_TimePicked(TimePickerFlyout sender, TimePickedEventArgs args)
				{
					Time = sender.Time;
				}
#endif
			}
		}

		private void OnTimeChanged(TimeSpan oldTime, TimeSpan newTime)
		{
			if (_bypassTimeAndSelectedTimeChanged)
			{
				return;
			}

			try
			{
				_bypassTimeAndSelectedTimeChanged = true;
				TimeSpan? newSelectedTime = newTime == new TimeSpan(-1) ? null : newTime;
				var oldSelectedTime = SelectedTime;
				SelectedTime = newSelectedTime;
				TimeChanged?.Invoke(this, new TimePickerValueChangedEventArgs(oldTime, newTime));
				SelectedTimeChanged?.Invoke(this, new TimePickerSelectedValueChangedEventArgs(oldSelectedTime, newSelectedTime));
			}
			finally
			{
				_bypassTimeAndSelectedTimeChanged = false;
			}
		}

		private void OnSelectedTimeChanged(TimeSpan? oldSelectedTime, TimeSpan? newSelectedTime)
		{
			UpdateDisplayedDate();

			if (_bypassTimeAndSelectedTimeChanged)
			{
				return;
			}

			try
			{
				_bypassTimeAndSelectedTimeChanged = true;
				Time = newSelectedTime ?? new TimeSpan(-1);
				TimeChanged?.Invoke(this, new TimePickerValueChangedEventArgs(oldSelectedTime ?? new TimeSpan(-1), newSelectedTime ?? new TimeSpan(-1)));
				SelectedTimeChanged?.Invoke(this, new TimePickerSelectedValueChangedEventArgs(oldSelectedTime, newSelectedTime));
			}
			finally
			{
				_bypassTimeAndSelectedTimeChanged = false;
			}
		}

		partial void OnClockIdentifierChangedPartial(string oldClockIdentifier, string newClockIdentifier)
		{
			if (newClockIdentifier != ClockIdentifiers.TwelveHour && newClockIdentifier != ClockIdentifiers.TwentyFourHour)
			{
				throw new ArgumentOutOfRangeException(nameof(newClockIdentifier), newClockIdentifier, "ClockIdentifier must be a valid value");
			}

			UpdateDisplayedDate();
		}

		partial void OnMinuteIncrementChanged(int oldTimeIncrement, int newTimeIncrement);

		private void UpdateDisplayedDate()
		{
			var selectedTime = SelectedTime;
			var isTwelveHour = ClockIdentifier == ClockIdentifiers.TwelveHour;
			if (selectedTime is null)
			{
				if (_hourTextBlock != null)
				{
					_hourTextBlock.Text = "hour";
				}
				if (_minuteTextBlock != null)
				{
					_minuteTextBlock.Text = "minute";
				}
				if (_periodTextBlock != null)
				{
					_periodTextBlock.Text = isTwelveHour ? "AM" : string.Empty;
				}
			}
			else
			{

				var dateTime = DateTime.Today.Add(selectedTime.Value);
				if (_hourTextBlock != null)
				{
					// http://stackoverflow.com/questions/3459677/datetime-tostringh-causes-exception
					_hourTextBlock.Text = dateTime.ToString(isTwelveHour ? "%h" : "%H", CultureInfo.CurrentCulture);
				}
				if (_minuteTextBlock != null)
				{
					_minuteTextBlock.Text = dateTime.ToString("mm", CultureInfo.CurrentCulture);
				}
				if (_periodTextBlock != null)
				{
					_periodTextBlock.Text = isTwelveHour ? dateTime.ToString("tt", CultureInfo.CurrentCulture) : string.Empty;
				}
			}

			if (_thirdTextBlockColumn != null)
			{
				_thirdTextBlockColumn.Width = isTwelveHour ? "*" : "0";
			}

#if __IOS__
			if (_flyoutButton?.Flyout is TimePickerFlyout timePickerFlyout)
			{
				timePickerFlyout.Time = Time;
			}
#endif
		}

		private void UpdateHeaderVisibility()
		{
			if (_headerContentPresenter != null)
			{
				_headerContentPresenter.Visibility =
					Header != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}

#if __IOS__ || __ANDROID__
		/// <summary>
		/// Apply 2-way binding to equivalent property on TimePickerFlyout
		/// </summary>
		/// <param name="propertyName">The property to 2-way bind</param>
		private void BindToFlyout(string propertyName)
		{
			this.Binding(propertyName, propertyName, _flyoutButton.Flyout, BindingMode.TwoWay);
		}
#endif

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TimePickerAutomationPeer(this);
		}
	}
}
#endif
