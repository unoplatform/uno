using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;

namespace Windows.System.Power;

internal class BatteryChangedBroadcastReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (Intent.ActionBatteryLow.Equals(
				intent!.Action, StringComparison.InvariantCultureIgnoreCase) ||
			Intent.ActionBatteryOkay.Equals(
				intent.Action, StringComparison.InvariantCultureIgnoreCase))
		{
			Windows.System.Power.PowerManager.RaiseBatteryStatusChanged();
			Windows.System.Power.PowerManager.RaiseRemainingChargePercentChanged();
		}
	}
}
