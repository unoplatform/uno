using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Android.Widget;
using Uno.UI;
using Uno.Extensions;
using Windows.Globalization;
using Uno.Logging;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls
{
    public partial class TimePickerSelector : ContentControl
    {
        private Android.Widget.TimePicker _nativePicker;

        protected override void OnLoaded()
        {
            base.OnLoaded();

            _nativePicker = this.FindFirstChild<Android.Widget.TimePicker>();

            if (_nativePicker != null)
            {
                //By settings DescendantFocusability to BlockDescendants it disables the possibility to use the keyboard to modify time which was causing issues in 4.4
                _nativePicker.DescendantFocusability = Android.Views.DescendantFocusability.BlockDescendants;
                SetTimeOnNativePicker(Time);
                OnClockIdentifierChangedPartialNative(null, ClockIdentifier);
            }
            else if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
            {
                this.Log().Debug($"No native TimePicker was found in the visual hierarchy.");
            }
        }

        protected override void OnUnloaded()
        {
            base.OnUnloaded();
        }

        partial void OnTimeChangedPartialNative(TimeSpan oldTime, TimeSpan newTime)
        {
            SetTimeOnNativePicker(newTime);
        }

        private void SetTimeOnNativePicker(TimeSpan newTime)
        {
            if (_nativePicker != null)
            {
                _nativePicker.SetHourCompat(newTime.Hours);
                _nativePicker.SetMinuteCompat(newTime.Minutes);
            }
        }

        internal void UpdateTime()
        {
            if (_nativePicker != null)
            {
                var newTime = new TimeSpan(_nativePicker.GetHourCompat(), _nativePicker.GetMinuteCompat(), seconds: 0);
                Time = newTime;
            }
        }

        partial void OnClockIdentifierChangedPartialNative(string oldClockIdentifier, string newClockIdentifier)
        {
            if (_nativePicker != null)
            {
                _nativePicker.SetIs24HourView(Java.Lang.Boolean.ValueOf(newClockIdentifier != ClockIdentifiers.TwelveHour));
            }
        }
	}
}
