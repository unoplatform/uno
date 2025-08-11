﻿#nullable disable

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Android.App;
using Android.Content;
using Java.Interop;
using Microsoft.Windows.AppLifecycle;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using IOnPreDrawListener = Android.Views.ViewTreeObserver.IOnPreDrawListener;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml
{
	public class NativeApplication : Android.App.Application
	{
		private Application _app;

#if ANDROID_SKIA
		private AppBuilder _appBuilder;
#endif

		private Intent _lastHandledIntent;

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
			ApiInformation.RegisterAssembly(typeof(global::Windows.Storage.ApplicationData).Assembly);
			ApiInformation.RegisterAssembly(typeof(Microsoft.UI.Composition.Compositor).Assembly);

			// Delay create the Microsoft.UI.Xaml.Application in order to get the
			// Android.App.Application.Context to be populated properly. This enables
			// APIs such as Windows.Storage.ApplicationData.Current.LocalSettings to function properly.
#if ANDROID_SKIA
			_appBuilder = appBuilder;
#else
			_app = appBuilder();
#endif

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

#if ANDROID_SKIA
				// We need to call TryHandleIntent first so the application arguments are set correctly.
				// Then, when the Application is created, it will use those arguments.
				_ = TryHandleIntent(activity.Intent);
				if (!_isRunning)
				{
					// We create the host late enough for the ContextHelper.Context to have been set correctly.
					new Uno.UI.Runtime.Skia.Android.AndroidHost(() => _app = _appBuilder()).Run();
				}
#else
				_app.InitializationCompleted();
				var handled = TryHandleIntent(activity.Intent);

				// default to normal launch
				if (!handled && !_isRunning)
				{
					_app.InvokeOnLaunched(new LaunchActivatedEventArgs());
				}
#endif

				_isRunning = true;
			}
		}

		internal bool TryHandleIntent(Intent intent)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Trying to handle intent with data: {intent?.Data?.ToString() ?? "(null)"}");
			}

			var handled = false;
			if (_lastHandledIntent != intent)
			{
				_lastHandledIntent = intent;
				if (intent?.Extras?.ContainsKey(JumpListItem.ArgumentsExtraKey) == true)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug("Intent contained JumpList extra arguments, calling OnLaunched.");
					}

					var jumplistKey = intent.GetStringExtra(JumpListItem.ArgumentsExtraKey);
					var launchArgs = new LaunchActivatedEventArgs(ActivationKind.Launch, jumplistKey);
					_app.InvokeOnLaunched(launchArgs);

					handled = true;
				}
				else if (intent.Data != null)
				{
					if (Uri.TryCreate(intent.Data.ToString(), UriKind.Absolute, out var uri))
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug("Intent data parsed successfully as Uri, calling OnActivated.");
						}

						var protocolArgs = new ProtocolActivatedEventArgs(uri, _isRunning ? ApplicationExecutionState.Running : ApplicationExecutionState.NotRunning);
						_app.InvokeOnActivated(protocolArgs);

						handled = true;
					}
					else
					{
						// log warning and continue with normal launch
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().LogWarning("URI cannot be parsed from Intent.Data, continuing unhandled");
						}
					}
				}
			}

			return handled;
		}

		/// <summary>
		/// This method is used by UI Test frameworks to get
		/// the Xamarin compatible name for a control in Java.
		/// </summary>
		/// <param name="type">A type full name</param>
		/// <returns>The assembly that contains the specified type</returns>
		[Export(nameof(GetTypeAssemblyFullName))]
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
#endif
