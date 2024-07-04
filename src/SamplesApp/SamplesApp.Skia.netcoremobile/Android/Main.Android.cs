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

	public class NativeApplication : Android.App.Application
	{
		private Microsoft.UI.Xaml.Application _app;

		private bool _isRunning;

		public delegate Microsoft.UI.Xaml.Application AppBuilder();

		/// <summary>
		/// Creates an android Application instance
		/// </summary>
		/// <param name="appBuilder">A <see cref="AppBuilder"/> delegate that provides an <see cref="Application"/> instance.</param>
		public NativeApplication(AppBuilder appBuilder, IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			// Register assemblies earlier than Application itself, otherwise
			// ApiInformation may return APIs as not implemented incorrectly.
			ApiInformation.RegisterAssembly(typeof(Application).Assembly);
			ApiInformation.RegisterAssembly(typeof(Windows.Storage.ApplicationData).Assembly);
			ApiInformation.RegisterAssembly(typeof(Windows.UI.Composition.Compositor).Assembly);

			// Delay create the Windows.UI.Xaml.Application in order to get the
			// Android.App.Application.Context to be populated properly. This enables
			// APIs such as Windows.Storage.ApplicationData.Current.LocalSettings to function properly.
			new AndroidSkiaHost(() => _app = appBuilder()).Run();
		}

		public override void OnCreate()
		{
			RegisterActivityLifecycleCallbacks(new ActivityCallbacks(this));
		}

		private void OnActivityStarted(Activity activity)
		{
			if (activity is ApplicationActivity)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug($"Application activity started with intent {activity.Intent}");
				}

				_app.InitializationCompleted();

				var handled = TryHandleIntent(activity.Intent);

				// default to normal launch
				if (!handled && !_isRunning)
				{
					_app.OnLaunched(new Microsoft.UI.Xaml.LaunchActivatedEventArgs());
				}

				_isRunning = true;
			}
		}

		internal bool TryHandleIntent(Intent intent)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Trying to handle intent with data: {intent?.Data?.ToString() ?? "(null)"}");
			}

			return false;
		}

		/// <summary>
		/// This method is used by UI Test frameworks to get
		/// the Xamarin compatible name for a control in Java.
		/// </summary>
		/// <param name="type">A type full name</param>
		/// <returns>The assembly that contains the specified type</returns>
		[Export]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) => Type.GetType(type)?.Assembly.FullName;

		private class ActivityCallbacks : Java.Lang.Object, IActivityLifecycleCallbacks
		{
			private readonly NativeApplication _app;

			public ActivityCallbacks(NativeApplication app)
			{
				_app = app;
			}

			public void OnActivityCreated(Android.App.Activity activity, Android.OS.Bundle savedInstanceState)
			{

			}

			public void OnActivityDestroyed(Android.App.Activity activity)
			{

			}

			public void OnActivityPaused(Android.App.Activity activity)
			{

			}

			public void OnActivityResumed(Android.App.Activity activity)
			{
			}

			public void OnActivitySaveInstanceState(Android.App.Activity activity, Android.OS.Bundle outState)
			{

			}

			public void OnActivityStarted(Android.App.Activity activity)
			{
				_app.OnActivityStarted(activity);
			}

			public void OnActivityStopped(Android.App.Activity activity)
			{

			}
		}

	}
}
