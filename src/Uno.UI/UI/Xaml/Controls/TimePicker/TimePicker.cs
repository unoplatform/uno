#if !__WASM__
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Uno.UI;
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

		private Button _flyoutButton;
		private TextBlock _hourTextBlock;
		private TextBlock _minuteTextBlock;
		private TextBlock _periodTextBlock;
		private Grid _flyoutButtonContentGrid;
		private ColumnDefinition _thirdTextBlockColumn;
		private bool _isLoaded;
		private bool _isViewReady;

		public TimePicker()
		{
			ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "FlyoutLightDismissOverlayBackground", isThemeResourceExtension: true);

			DefaultStyleKey = typeof(TimePicker);
		}

		#region Time DependencyProperty

		public TimeSpan Time
		{
			get { return (TimeSpan)this.GetValue(TimeProperty); }
			set { this.SetValue(TimeProperty, value); }
		}

		public static readonly DependencyProperty TimeProperty =
			DependencyProperty.Register(
				"Time",
				typeof(TimeSpan),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(
					defaultValue: DateTime.Now.TimeOfDay,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((TimePicker)s)?.OnTimeChangedPartial((TimeSpan)e.OldValue, (TimeSpan)e.NewValue),
					coerceValueCallback: (s, e) =>
					{
						var ts = (TimeSpan)e;
						return new TimeSpan(ts.Days, ts.Hours, ts.Minutes, 0);
					})
				);

		#endregion

		#region MinuteIncrement DependencyProperty

		public int MinuteIncrement
		{
			get { return (int)this.GetValue(MinuteIncrementProperty); }
			set { this.SetValue(MinuteIncrementProperty, value); }
		}

		public static readonly DependencyProperty MinuteIncrementProperty =
			DependencyProperty.Register(
				"MinuteIncrement",
				typeof(int),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(
					defaultValue: 1,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((TimePicker)s)?.OnMinuteIncrementChanged((int)e.OldValue, (int)e.NewValue),
					coerceValueCallback: (s, e) =>
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
			get { return (FlyoutPlacementMode)this.GetValue(FlyoutPlacementProperty); }
			set { this.SetValue(FlyoutPlacementProperty, value); }
		}

		public static readonly DependencyProperty FlyoutPlacementProperty =
			DependencyProperty.Register(
				"FlyoutPlacement",
				typeof(FlyoutPlacementMode),
				typeof(TimePicker),
				new FrameworkPropertyMetadata(FlyoutPlacementMode.Full));

		#endregion


		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}
		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

		/// <summary>
		/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc, static resource values.
		/// </summary>
		internal Brush LightDismissOverlayBackground
		{
			get { return (Brush)GetValue(LightDismissOverlayBackgroundProperty); }
			set { SetValue(LightDismissOverlayBackgroundProperty, value); }
		}

		internal static readonly DependencyProperty LightDismissOverlayBackgroundProperty =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(TimePicker), new PropertyMetadata(null));

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_flyoutButton = this.GetTemplateChild(FlyoutButtonPartName) as Button;

			var flyoutContent = _flyoutButton?.Content as IFrameworkElement;

			_hourTextBlock = flyoutContent?.GetTemplateChild(HourTextBlockPartName) as TextBlock;
			_minuteTextBlock = flyoutContent?.GetTemplateChild(MinuteTextBlockPartName) as TextBlock;
			_periodTextBlock = flyoutContent?.GetTemplateChild(PeriodTextBlockPartName) as TextBlock;
			_flyoutButtonContentGrid = flyoutContent?.GetTemplateChild(FlyoutButtonContentGridPartName) as Grid;

			var columns = _flyoutButtonContentGrid?.ColumnDefinitions;
			const int periodColumnPosition = 4;
			if ((columns?.Count ?? 0) > periodColumnPosition)
			{
				// This is a workaround for the lack of support for GetTemplateChild on non-IFrameworkElement types. (Bug #26303)
				_thirdTextBlockColumn = columns.ElementAt(periodColumnPosition);
			}

			_isViewReady = true;

			SetupFlyoutButton();
		}

		protected override void OnLoaded()
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
#endif
					Time = this.Time,
					MinuteIncrement = this.MinuteIncrement,
					ClockIdentifier = this.ClockIdentifier
				};

				BindToFlyout(nameof(Time));
				BindToFlyout(nameof(MinuteIncrement));
				BindToFlyout(nameof(ClockIdentifier));
				_flyoutButton.Flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
				_flyoutButton.Flyout.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));
#endif
			}
		}

		void OnTimeChangedPartial(TimeSpan oldTime, TimeSpan newTime)
		{
			UpdateDisplayedDate();
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
			var dateTime = DateTime.Today.Add(Time);
			var isTwelveHour = ClockIdentifier == ClockIdentifiers.TwelveHour;
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

		/// <summary>
		/// Apply 2-way binding to equivalent property on TimePickerFlyout
		/// </summary>
		/// <param name="propertyName">The property to 2-way bind</param>
		private void BindToFlyout(string propertyName)
		{
			this.Binding(propertyName, propertyName, _flyoutButton.Flyout, BindingMode.TwoWay);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new TimePickerAutomationPeer(this);
		}
	}
}
#endif
