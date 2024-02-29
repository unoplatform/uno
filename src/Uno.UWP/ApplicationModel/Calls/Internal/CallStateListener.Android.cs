using System;
using System.Collections.Generic;
using System.Text;
using Android.Runtime;
using Android.Telephony;

namespace Windows.ApplicationModel.Calls
{
#pragma warning disable CS0618, CS0672 // PhoneStateListener is obsolete
	internal class CallStateListener : PhoneStateListener
	{
		public override void OnCallStateChanged([GeneratedEnum] CallState state, string? phoneNumber)
		{
			PhoneCallManager.RaiseCallStateChanged();
		}
	}
#pragma warning restore CS0618, CS0672 // PhoneStateListener is obsolete
}
