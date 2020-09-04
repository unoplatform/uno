using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{
	public enum SystemTriggerType
	{
		Invalid,
		SmsReceived,
		UserPresent,
		UserAway,
		NetworkStateChange,
		ControlChannelReset,
		InternetAvailable,
		SessionConnected,
		ServicingComplete,
		LockScreenApplicationAdded,
		LockScreenApplicationRemoved,
		TimeZoneChange,
		OnlineIdConnectedStateChange,
		BackgroundWorkCostChange,
		PowerStateChange,
		DefaultSignInAccountChange,
	}
}
