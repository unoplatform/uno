using System;
using System.Collections.Generic;
using System.Text;

using NativeMethods = __Windows.__System.Launcher.NativeMethods;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static void ShowPhoneCallUIImpl(string phoneNumber, string displayName)
		{
			var uri = new Uri($"tel:{phoneNumber}");
			NativeMethods.Open(uri.AbsoluteUri);
		}
	}
}
