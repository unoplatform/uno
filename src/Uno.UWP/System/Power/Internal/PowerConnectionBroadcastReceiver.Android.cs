using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Windows.System.Power;

namespace Windows.System.Power;

internal class PowerConnectionBroadcastReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (Intent.ActionPowerConnected.Equals(
			intent!.Action, StringComparison.InvariantCultureIgnoreCase) ||
			Intent.ActionPowerDisconnected.Equals(
				intent.Action, StringComparison.InvariantCultureIgnoreCase))
		{
			Windows.System.Power.PowerManager.RaisePowerSupplyStatusChanged();
			Windows.System.Power.PowerManager.RaiseBatteryStatusChanged();
		}
	}
}
