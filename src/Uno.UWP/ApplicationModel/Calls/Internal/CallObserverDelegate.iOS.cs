#nullable disable

#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using CallKit;

namespace Windows.ApplicationModel.Calls
{
	internal class CallObserverDelegate : CXCallObserverDelegate
	{
		public override void CallChanged(CXCallObserver callObserver, CXCall call) =>
			PhoneCallManager.RaiseCallStateChanged();
	}
}
#endif
