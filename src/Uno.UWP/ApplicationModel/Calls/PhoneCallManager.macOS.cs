using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using Foundation;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static void ShowPhoneCallUIImpl(string phoneNumber, string displayName)
		{
			var uri = $"tel:{phoneNumber}";
			NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(uri));
		}
	}
}