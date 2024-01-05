using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Telephony;
using Uno.UI;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static readonly TelephonyManager _telephonyManager;

		static PhoneCallManager()
		{
			if (ContextHelper.Current == null)
			{
				throw new InvalidOperationException(
					"PhoneCallManager was used too early in the application lifetime. " +
					"Android app context needs to be available.");
			}
			_telephonyManager = (TelephonyManager)ContextHelper.Current!
				.GetSystemService(Context.TelephonyService)!;
#pragma warning disable CS0618 // TelephonyManager is obsolete in API 31
#pragma warning disable CA1422 // Validate platform compatibility
			_telephonyManager.Listen(new CallStateListener(), PhoneStateListenerFlags.CallState);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // TelephonyManager is obsolete in API 31
		}

		public static event EventHandler<object?>? CallStateChanged;

		public static bool IsCallActive =>
#pragma warning disable CS0618 // TelephonyManager is obsolete in API 31
#pragma warning disable CA1422 // Validate platform compatibility
			_telephonyManager.CallState == CallState.Offhook;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // TelephonyManager is obsolete in API 31

		public static bool IsCallIncoming =>
#pragma warning disable CS0618 // TelephonyManager is obsolete in API 31
#pragma warning disable CA1422 // Validate platform compatibility
			_telephonyManager.CallState == CallState.Ringing;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // TelephonyManager is obsolete in API 31

		public static void ShowPhoneCallSettingsUI()
		{
			var intent = new Intent(Android.Provider.Settings.ActionNetworkOperatorSettings)
				.SetFlags(ActivityFlags.ClearTop)
				.SetFlags(ActivityFlags.NewTask);
			ContextHelper.Current.StartActivity(intent);
		}

		internal static void RaiseCallStateChanged() => CallStateChanged?.Invoke(null, null);

		private static void ShowPhoneCallUIImpl(string phoneNumber, string displayName)
		{
			if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
			{
				phoneNumber = PhoneNumberUtils.FormatNumber(phoneNumber, Java.Util.Locale.GetDefault(Java.Util.Locale.Category.Format!).Country)!;
			}
			else if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
			{
				phoneNumber = PhoneNumberUtils.FormatNumber(phoneNumber, Java.Util.Locale.Default.Country)!;
			}
			else
			{
#pragma warning disable CS0618
#pragma warning disable CA1422 // Validate platform compatibility
				phoneNumber = PhoneNumberUtils.FormatNumber(phoneNumber)!;
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618
			}

			var phoneCallIntent = new Intent(
				Intent.ActionDial,
				Android.Net.Uri.Parse($"tel:{phoneNumber}"))
					.SetFlags(ActivityFlags.ClearTop)
					.SetFlags(ActivityFlags.NewTask);

			ContextHelper.Current.StartActivity(phoneCallIntent);
		}
	}
}
