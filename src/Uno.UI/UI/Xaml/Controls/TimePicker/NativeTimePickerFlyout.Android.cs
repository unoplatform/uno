#nullable disable

using Android.OS;
using System;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.Globalization;
using Microsoft.UI.Xaml.Controls.Primitives;
using static Android.App.TimePickerDialog;
using Windows.ApplicationModel.Core;

namespace Microsoft.UI.Xaml.Controls;

partial class NativeTimePickerFlyout : TimePickerFlyout
{
	private bool _programmaticallyDismissed;
	private UnoTimePickerDialog _dialog;
	private TimeSpan _initialTime;

	internal bool IsNativeDialogOpen => _dialog?.IsShowing ?? false;
	internal UnoTimePickerDialog GetNativeDialog() => _dialog;


	internal protected override void Open()
	{
		if (Time.Ticks == DEFAULT_TIME_TICKS)
		{
			Time = GetCurrentTime();
		}

		SaveInitialTime();

		ShowTimePicker();

		AddToOpenFlyouts();
	}

	private void SaveInitialTime() => _initialTime = Time;


	private void SaveTime(TimeSpan time)
	{
		if (Time != time)
		{
			Time = time;
			SaveInitialTime();
		}
	}

	private void AdjustAndSaveTime(int hourOfDay, int minutes)
	{
		var oldTime = Time;
		if (Time.Hours != hourOfDay || Time.Minutes != minutes)
		{
			if (_dialog.IsInSpinnerMode)
			{
				minutes = minutes * MinuteIncrement;
			}

#if ANDROID_SKIA
			var time = new TimeSpan(0, hourOfDay, minutes, 0);
#else
			var time = FeatureConfiguration.TimePickerFlyout.UseLegacyTimeSetting
				? new TimeSpan(Time.Days, hourOfDay, minutes, Time.Seconds, Time.Milliseconds)
				: new TimeSpan(0, hourOfDay, minutes, 0);
#endif
			SaveTime(time.RoundToMinuteInterval(MinuteIncrement));
		}

		OnTimePicked(new TimePickedEventArgs(oldTime, Time));
	}

	private void ShowTimePicker()
	{
		var time = _initialTime.RoundToNextMinuteInterval(MinuteIncrement);
		var listener = new OnSetTimeListener((view, hourOfDay, minute) => AdjustAndSaveTime(hourOfDay, minute));

		var themeResourceId = CoreApplication.RequestedTheme == Uno.Helpers.Theming.SystemTheme.Light ?
			global::Android.Resource.Style.ThemeDeviceDefaultLightDialog :
			global::Android.Resource.Style.ThemeDeviceDefaultDialog;

		_dialog = new UnoTimePickerDialog(
				ContextHelper.Current,
				themeResourceId,
				listener,
				time.Hours,
				time.Minutes,
				ClockIdentifier == ClockIdentifiers.TwentyFourHour,
				MinuteIncrement
			);

		_dialog.DismissEvent += OnDismiss;
		_dialog.Show();
	}

	private void OnDismiss(object sender, EventArgs e)
	{
		if (!_programmaticallyDismissed)
		{
			Hide(canCancel: false);
			RemoveFromOpenFlyouts();
		}
	}

	private protected override void OnClosed()
	{
		_programmaticallyDismissed = true;
		_dialog?.Dismiss();
		base.OnClosed();
		_programmaticallyDismissed = false;
	}

	//partial void OnTimeChangedPartialNative(TimeSpan oldTime, TimeSpan newTime) { }

	public class OnSetTimeListener : Java.Lang.Object, IOnTimeSetListener
	{
		private Action<ATimePicker, int, int> _action;

		public OnSetTimeListener(Action<ATimePicker, int, int> action)
		{
			_action = action;
		}

		public void OnTimeSet(ATimePicker view, int hourOfDay, int minute) => _action(view, hourOfDay, minute);
	}
}
