
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{
	public enum SystemConditionType
	{
		Invalid,
		UserPresent,
		UserNotPresent,
		InternetAvailable,
		InternetNotAvailable,
		SessionConnected,
		SessionDisconnected,
		FreeNetworkAvailable,
		BackgroundWorkCostNotHigh,
	}

}
