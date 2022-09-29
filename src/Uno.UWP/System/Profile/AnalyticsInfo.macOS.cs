#nullable disable

using AppKit;
using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public static partial class AnalyticsInfo
{
	private static UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
}
