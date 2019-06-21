#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.Telephony;
using Uno.UI;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static readonly TelephonyManager _telephonyManager;

		public static PhoneCallManager()
		{
			_telephonyManager = (TelephonyManager)ContextHelper.Current
				.GetSystemService(Context.TelephonyService);
		}

		public static bool IsCallActive =>
			_telephonyManager.CallState == CallState.Offhook;

		public static bool IsCallIncoming =>
			_telephonyManager.CallState == CallState.Ringing;


		public static void ShowPhoneCallSettingsUI()
		{
			var intent = new Intent(Android.Provider.Settings.ActionNetworkOperatorSettings);
			ContextHelper.Current.StartActivity(intent);
		}
	}
}
#endif
