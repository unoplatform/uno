#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.OS;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		public static bool IsSupported() => Build.VERSION.SdkInt >= BuildVersionCodes.NMr1;
	}
}
#endif
