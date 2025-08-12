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

[assembly: UsesPermission("android.permission.ACCESS_COARSE_LOCATION")]
[assembly: UsesPermission("android.permission.ACCESS_FINE_LOCATION")]
[assembly: UsesPermission("android.permission.VIBRATE")]
[assembly: UsesPermission("android.permission.ACTIVITY_RECOGNITION")]
[assembly: UsesPermission("android.permission.ACCESS_NETWORK_STATE")]
[assembly: UsesPermission("android.permission.SET_WALLPAPER")]
[assembly: UsesPermission("android.permission.READ_CONTACTS")]
[assembly: UsesPermission("android.permission.INTERNET")]
[assembly: UsesPermission("android.permission.POST_NOTIFICATIONS")]

[assembly: UsesFeature("android.software.leanback", Required = false)]
[assembly: UsesFeature("android.hardware.touchscreen", Required = false)]

namespace SamplesApp.Droid
{
	[global::Android.App.ApplicationAttribute(
		Label = "@string/SamplesAppName",
		Icon = "@mipmap/icon",
		Banner = "@drawable/banner",
		LargeHeap = true,
		HardwareAccelerated = true,
		Theme = "@style/Theme.App.Starting"
	)]
	public class Application : Microsoft.UI.Xaml.NativeApplication
	{
		public Application(IntPtr javaReference, JniHandleOwnership transfer)
			: base(() => new App(), javaReference, transfer)
		{
			ConfigureUniversalImageLoader();
		}

		private void ConfigureUniversalImageLoader()
		{
			// Create global configuration and initialize ImageLoader with this config
			ImageLoaderConfiguration config = new ImageLoaderConfiguration
				.Builder(Context)
				.Build();

			ImageLoader.Instance.Init(config);

			ImageSource.DefaultImageLoader = ImageLoader.Instance.LoadImageAsync;
		}
	}
}
