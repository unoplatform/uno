#nullable disable

#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.System.Display
{
	public partial class DisplayRequest
	{
		partial void ActivateScreenLock()
		{
			UIApplication.SharedApplication.IdleTimerDisabled = true;
		}

		partial void DeactivateScreenLock()
		{
			UIApplication.SharedApplication.IdleTimerDisabled = false;
		}
	}
}
#endif
