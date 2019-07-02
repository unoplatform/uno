#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.Runtime;
using Android.Telephony;

namespace Windows.ApplicationModel.Calls
{
	internal class CallStateListener : PhoneStateListener
	{
		public override void OnCallStateChanged([GeneratedEnum] CallState state, string phoneNumber)
		{
			PhoneCallManager.RaiseCallStateChanged();
		}
	}
}
#endif
