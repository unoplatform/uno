using Android.OS;
using System;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.Globalization;
using Windows.UI.Xaml.Controls.Primitives;
using static Android.App.TimePickerDialog;
using Windows.ApplicationModel.Core;

namespace Windows.UI.Xaml.Controls;

partial class NativeTimePickerFlyout
{
	private bool _programmaticallyDismissed;
	private UnoTimePickerDialog _dialog;
	private TimeSpan _initialTime;

	internal bool IsNativeDialogOpen => _dialog?.IsShowing ?? false;

	internal protected override void Open()
	{
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

			var time = FeatureConfiguration.TimePickerFlyout.UseLegacyTimeSetting
				? new TimeSpan(Time.Days, hourOfDay, minutes, Time.Seconds, Time.Milliseconds)
				: new TimeSpan(0, hourOfDay, minutes, 0);
			SaveTime(time.RoundToMinuteInterval(MinuteIncrement));
		}

		OnTimePicked(new TimePickedEventArgs(oldTime, Time));
	}

	private void ShowTimePicker()
	{
		var time = Time.RoundToNextMinuteInterval(MinuteIncrement);
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
		private Action<Android.Widget.TimePicker, int, int> _action;

		public OnSetTimeListener(Action<Android.Widget.TimePicker, int, int> action)
		{
			_action = action;
		}

		public void OnTimeSet(Android.Widget.TimePicker view, int hourOfDay, int minute) => _action(view, hourOfDay, minute);
	}
}
