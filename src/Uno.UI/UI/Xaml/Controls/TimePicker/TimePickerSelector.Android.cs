using System;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.Globalization;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerSelector : ContentControl
	{
		private Android.Widget.TimePicker _picker;
		private TimeSpan _initialTime;

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_picker = this.FindFirstChild<Android.Widget.TimePicker>();

			if (_picker != null)
			{
				//By settings DescendantFocusability to BlockDescendnts it disables the possibility to use the keyboard to modify time which was causing issues in 4.4
				_picker.DescendantFocusability = Android.Views.DescendantFocusability.BlockDescendants;

				this.Binding(nameof(Time), nameof(Time), Content, BindingMode.TwoWay);
				this.Binding(nameof(MinuteIncrement), nameof(MinuteIncrement), Content, BindingMode.TwoWay);
				this.Binding(nameof(ClockIdentifier), nameof(ClockIdentifier), Content, BindingMode.TwoWay);
			}
			else if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"No native TimePicker was found in the visual hierarchy.");
			}
		}

		public void Initialize()
		{
			SaveInitialTime();
			SetPickerTime(Time);
			SetPickerClockIdentifier(ClockIdentifier);
		}

		private void SaveInitialTime() => _initialTime = Time;

		internal void SaveTime()
		{
			if (_picker != null)
			{
				Time = new TimeSpan(_picker.GetHourCompat(), _picker.GetMinuteCompat(), seconds: 0);
				SaveInitialTime();
			}
		}

		private void SetPickerClockIdentifier(string clockIdentifier)
		{
			if (_picker != null)
			{
				_picker.SetIs24HourView(Java.Lang.Boolean.ValueOf(clockIdentifier != ClockIdentifiers.TwelveHour));
			}
		}

		private void SetPickerTime(TimeSpan newTime)
		{
			if (_picker != null)
			{
				var time = newTime.RoundToNextMinuteInterval(MinuteIncrement);

				_picker.SetHourCompat(time.Hours);
				_picker.SetMinuteCompat(time.Minutes);
			}
		}

		partial void OnClockIdentifierChangedPartialNative(string oldClockIdentifier, string newClockIdentifier)
		{
			SetPickerClockIdentifier(newClockIdentifier);
		}

		partial void OnTimeChangedPartialNative(TimeSpan oldTime, TimeSpan newTime)
		{
			SetPickerTime(newTime);
		}
	}
}
