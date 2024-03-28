using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
	public class UnoTimePickerDialog : TimePickerDialog
	{
		private Android.Widget.TimePicker _picker;
		private int _minuteIncrement = 1;
		private int _hourOfDay;
		private int _minute;
		private bool _is24HourView;

		public bool IsInSpinnerMode { get; private set; }

		public UnoTimePickerDialog(Context context, IOnTimeSetListener listener, int hourOfDay, int minute, bool is24HourView, int minuteIncrement) : base(context, listener, hourOfDay, minute, is24HourView)
		{
			_minuteIncrement = minuteIncrement;
			_hourOfDay = hourOfDay;
			_minute = minute;
			_is24HourView = is24HourView;

			Initialize();
		}

		public UnoTimePickerDialog(Context context, EventHandler<TimeSetEventArgs> callBack, int hourOfDay, int minute, bool is24HourView, int minuteIncrement) : base(context, callBack, hourOfDay, minute, is24HourView)
		{
			_minuteIncrement = minuteIncrement;
			_hourOfDay = hourOfDay;
			_minute = minute;

			Initialize();
		}

		public UnoTimePickerDialog(Context context, int themeResId, IOnTimeSetListener listener, int hourOfDay, int minute, bool is24HourView, int minuteIncrement) : base(context, themeResId, listener, hourOfDay, minute, is24HourView)
		{
			_minuteIncrement = minuteIncrement;
			_hourOfDay = hourOfDay;
			_minute = minute;

			Initialize();
		}

		public UnoTimePickerDialog(Context context, int theme, EventHandler<TimeSetEventArgs> callBack, int hourOfDay, int minute, bool is24HourView, int minuteIncrement) : base(context, theme, callBack, hourOfDay, minute, is24HourView)
		{
			_minuteIncrement = minuteIncrement;
			_hourOfDay = hourOfDay;
			_minute = minute;

			Initialize();
		}

		public override void SetView(View view)
		{
			_picker = (Android.Widget.TimePicker)view;
			base.SetView(_picker);
		}

		private void Initialize()
		{
			if (_minuteIncrement >= 1 && _minuteIncrement <= 30)
			{
				SetMinuteIncrement();
			}
		}

		private void SetMinuteIncrement()
		{
			var minutePicker = FindMinuteNumberPicker(_picker as ViewGroup);

			if (minutePicker != null)
			{
				IsInSpinnerMode = true;

				var values = new List<int>();

				for (int i = 0; i < 60; i += _minuteIncrement)
				{
					values.Add(i);
				}

				minutePicker.Value = values.FindIndex(num => num == _minute);
				minutePicker.MinValue = 0;
				minutePicker.MaxValue = values.Count - 1;
				minutePicker.SetDisplayedValues(values.Select(num => num.ToString("00", CultureInfo.CurrentCulture)).ToArray());
			}
			else
			{
				IsInSpinnerMode = false;
			}
		}

		private NumberPicker FindMinuteNumberPicker(ViewGroup control)
		{
			return FindNumberPicker(control, 59);
		}

		private NumberPicker FindNumberPicker(ViewGroup control, int maxValue)
		{
			for (var i = 0; i < control.ChildCount; i++)
			{
				var child = control.GetChildAt(i);
				var picker = child as NumberPicker;

				if (picker != null)
				{
					if (picker.MaxValue == maxValue)
					{
						return picker;
					}
				}

				var childViewGroup = child as ViewGroup;

				if (childViewGroup != null)
				{
					var childResult = FindNumberPicker(childViewGroup, maxValue);
					if (childResult != null)
						return childResult;
				}
			}

			return null;
		}
	}
}
