using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml.Media;
using Com.Nostra13.Universalimageloader.Core;
using Windows.Foundation.Metadata;
using Uno.Extensions;
using Windows.ApplicationModel.Activation;
using Microsoft.Extensions.Logging;
using Windows.UI.StartScreen;
using Java.Interop;
using Uno.UI.Runtime.Skia.Android;
using Microsoft.UI.Xaml;

[assembly: UsesPermission("android.permission.ACCESS_COARSE_LOCATION")]
[assembly: UsesPermission("android.permission.ACCESS_FINE_LOCATION")]
[assembly: UsesPermission("android.permission.VIBRATE")]
[assembly: UsesPermission("android.permission.ACTIVITY_RECOGNITION")]
[assembly: UsesPermission("android.permission.ACCESS_NETWORK_STATE")]
[assembly: UsesPermission("android.permission.SET_WALLPAPER")]
[assembly: UsesPermission("android.permission.READ_CONTACTS")]
[assembly: UsesPermission("android.permission.INTERNET")]

[assembly: UsesFeature("android.software.leanback", Required = false)]
[assembly: UsesFeature("android.hardware.touchscreen", Required = false)]

namespace SamplesApp.Droid
{
	[global::Android.App.ApplicationAttribute(
		Label = "@string/ApplicationName",
		Banner = "@drawable/banner",
		LargeHeap = true,
		HardwareAccelerated = true,
		Theme = "@style/AppTheme"
	)]
	public class Application : NativeApplication
	{
		public Application(IntPtr javaReference, JniHandleOwnership transfer)
			: base(() => new App(), javaReference, transfer)
		{

		}
	}
}
