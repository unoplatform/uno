using AppKit;
using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using Uno.System.Profile;

namespace Windows.System.Profile
{
	public static partial class AnalyticsInfo
	{
		private static UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
	}
}
