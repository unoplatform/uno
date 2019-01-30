#if !NET46 && !__WASM__
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Uno.Extensions;
using Windows.Globalization;
using Windows.UI.Xaml.Data;
using System.Linq;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Automation.Peers;

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

		public TimePicker() { }

		//Properties defined in DependencyPropertyMixins.tt

		/// <summary>
		/// Property that allows apps to specify any flyout placement 
		/// (especially FlyoutPlacementMode.Full, which is commonly used on iPhone)
		/// </summary>
		public FlyoutPlacementMode FlyoutPlacement { get; set; }

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

			SetupFlyoutButton();
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();
			SetupFlyoutButton();
		}

		private void SetupFlyoutButton()
		{
			if (_flyoutButton != null)
			{
#if __IOS__ || __ANDROID__
				_flyoutButton.Flyout = new TimePickerFlyout
				{
#if __IOS__
					Placement = FlyoutPlacement,
#endif
					Time = this.Time,
					ClockIdentifier = this.ClockIdentifier
				};

				BindToFlyout(nameof(Time));
				BindToFlyout(nameof(ClockIdentifier));
#endif
			}

			UpdateDisplayedDate();
		}

		partial void OnTimeChangedPartial(TimeSpan oldTime, TimeSpan newTime)
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
