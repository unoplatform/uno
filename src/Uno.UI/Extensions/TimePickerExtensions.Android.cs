using Android.Widget;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Extensions
{
	public static class TimePickerExtensions
	{
		public static int GetHourCompat(this TimePicker timePicker)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				return timePicker.Hour;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
				return (int)timePicker.CurrentHour;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public static void SetHourCompat(this TimePicker timePicker, int hour)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				timePicker.Hour = hour;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
				timePicker.CurrentHour = (Java.Lang.Integer)hour;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public static int GetMinuteCompat(this TimePicker timePicker)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				return timePicker.Minute;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
				return (int)timePicker.CurrentMinute;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public static void SetMinuteCompat(this TimePicker timePicker, int minute)
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
			{
				timePicker.Minute = minute;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
				timePicker.CurrentMinute = (Java.Lang.Integer)minute;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}
	}
}
