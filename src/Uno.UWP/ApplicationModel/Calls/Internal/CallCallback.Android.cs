using System;
using System.Collections.Generic;
using System.Text;
using Android.Runtime;
using Android.Telephony;

namespace Windows.ApplicationModel.Calls
{
	internal class CallCallback : TelephonyCallback, TelephonyCallback.ICallStateListener
	{
		public void OnCallStateChanged(int state)
		{
			PhoneCallManager.RaiseCallStateChanged();
		}
	}
}
