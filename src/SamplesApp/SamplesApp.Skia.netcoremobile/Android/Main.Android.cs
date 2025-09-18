using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using Uno.Extensions;
using Windows.ApplicationModel.Activation;
using Microsoft.Extensions.Logging;
using Windows.UI.StartScreen;
using Java.Interop;
using Uno.UI.Runtime.Skia.Android;
using Microsoft.UI.Xaml;
using Windows.Services.Store.Internal;
using Uno.Foundation.Extensibility;
using Uno.Devices.Sensors;
using Uno.UI.Foldable;
using Windows.UI.ViewManagement;
using Uno.UI;

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
		Label = "@string/SamplesAppName",
		Icon = "@mipmap/icon",
		Banner = "@drawable/banner",
		LargeHeap = true,
		HardwareAccelerated = true,
		Theme = "@style/Theme.App.Starting"
	)]
	public class Application : NativeApplication
	{
		public Application(IntPtr javaReference, JniHandleOwnership transfer)
			: base(() => new App(), javaReference, transfer)
		{
			// Copyright 2017 The Chromium Authors. All rights reserved.
			//
			// 	Redistribution and use in source and binary forms, with or without
			// 	modification, are permitted provided that the following conditions are
			// met:
			//
			// * Redistributions of source code must retain the above copyright
			// 	notice, this list of conditions and the following disclaimer.
			// * Redistributions in binary form must reproduce the above
			// copyright notice, this list of conditions and the following disclaimer
			// 	in the documentation and/or other materials provided with the
			// distribution.
			// * Neither the name of Google Inc. nor the names of its
			// 	contributors may be used to endorse or promote products derived from
			// this software without specific prior written permission.
			//
			// 	THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
			// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
			// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
			// 	A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
			// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
			// 	SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
			// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
			// 	DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
			// 	THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
			// 	(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
			// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

			// https://github.com/fluttercommunity/plus_plugins/blob/bfcd3e19cb457178c5f69c5cf146a8fd8b2dec44/packages/device_info_plus/device_info_plus/android/src/main/kotlin/dev/fluttercommunity/plus/device_info/MethodCallHandlerImpl.kt#L110-L125
			var isEmulator = Build.Brand!.StartsWith("generic", StringComparison.InvariantCulture) &&
							 Build.Device!.StartsWith("generic", StringComparison.InvariantCulture)
							 || Build.Fingerprint!.StartsWith("generic", StringComparison.InvariantCulture)
							 || Build.Fingerprint.StartsWith("unknown", StringComparison.InvariantCulture)
							 || Build.Hardware!.Contains("goldfish", StringComparison.InvariantCulture)
							 || Build.Hardware.Contains("ranchu")
							 || Build.Model!.Contains("google_sdk")
							 || Build.Model.Contains("Emulator")
							 || Build.Model.Contains("Android SDK built for x86")
							 || Build.Manufacturer!.Contains("Genymotion")
							 || Build.Product!.Contains("sdk_google")
							 || Build.Product.Contains("google_sdk")
							 || Build.Product.Contains("sdk")
							 || Build.Product.Contains("sdk_x86")
							 || Build.Product.Contains("vbox86p")
							 || Build.Product.Contains("emulator")
							 || Build.Product.Contains("simulator");

			// Android/skia is crashing in emulators in When_OnDragItemsCompleted
			// when using HWA and SkiaSharp 3.
			if (isEmulator)
			{
				FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid = false;
			}
		}

		public override void OnCreate()
		{
			base.OnCreate();

#if RUNTIME_CORECLR
			// TODO: remove once the Android+CoreCLR runtime properly inits crypto, possibly .NET 10 RC2?
			Java.Lang.JavaSystem.LoadLibrary("System.Security.Cryptography.Native.Android");
#endif  // RUNTIME_CORECLR

			// Initialize Android-specific extensions.
			// These would be generally registered automatically by App.xaml generator,
			// but in our case it runs in context of SamplesApp.Skia, which does not reference
			// this Android-specific addin.
			ApiExtensibility.Register(typeof(IStoreContextExtension), o => new global::Uno.UI.GooglePlay.StoreContextExtension(o));
			ApiExtensibility.Register(typeof(INativeHingeAngleSensor), o => new FoldableHingeAngleSensor(o));
			ApiExtensibility.Register(typeof(IApplicationViewSpanningRects), o => new FoldableApplicationViewSpanningRects(o));
		}
	}
}
