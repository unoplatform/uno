using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;

namespace Windows.System.Power;

internal class PowerSaveModeChangeReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (Android.OS.PowerManager.ActionPowerSaveModeChanged.Equals(
			intent!.Action, StringComparison.InvariantCultureIgnoreCase))
		{
			Windows.System.Power.PowerManager.RaiseEnergySaverStatusChanged();
		}
	}
}
