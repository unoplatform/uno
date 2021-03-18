#if __MACOS__
using Uno.Helpers;

namespace Windows.System.Display
{
	public partial class DisplayRequest
	{
		private uint _displayRequestId;

		partial void ActivateScreenLock()
		{
			IOKit.PreventUserIdleDisplaySleep("DisplayRequest", out _displayRequestId);
		}

		partial void DeactivateScreenLock()
		{
			IOKit.AllowUserIdleDisplaySleep(_displayRequestId);
		}
	}
}
#endif