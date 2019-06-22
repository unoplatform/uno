#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		public static bool IsSupported() => UIDevice.CurrentDevice.CheckSystemVersion(9, 0);
	}
}
#endif
