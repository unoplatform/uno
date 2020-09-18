using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{
	public enum BackgroundAccessStatus
	{
		Unspecified,
		AllowedWithAlwaysOnRealTimeConnectivity,
		AllowedMayUseActiveRealTimeConnectivity,
		Denied,
		AlwaysAllowed,
		AllowedSubjectToSystemPolicy,
		DeniedBySystemPolicy,
		DeniedByUser,
	}
}
