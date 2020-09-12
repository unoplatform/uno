#if __ANDROID__ || __IOS__ || __MACOS__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.System.Display
{
	public partial class DisplayRequest
	{
		private static object _syncLock = new object();
		private static int _globalRequestCount = 0;

		private int _instanceRequestCount = 0;

		public void RequestActive()
		{
			lock (_syncLock)
			{
				_instanceRequestCount++;
				_globalRequestCount++;
				if (_globalRequestCount == 1)
				{
					//first global request, activate screen lock
					ActivateScreenLock();
				}
			}
		}

		public void RequestRelease()
		{
			lock (_syncLock)
			{
				if (_instanceRequestCount == 0)
				{
					throw new InvalidOperationException("No active display request to release");
				}
				_instanceRequestCount--;
				_globalRequestCount--;
				if (_globalRequestCount == 0)
				{
					//last global active request, deactivate
					DeactivateScreenLock();
				}
			}
		}

		partial void ActivateScreenLock();

		partial void DeactivateScreenLock();
	}
}
#endif
