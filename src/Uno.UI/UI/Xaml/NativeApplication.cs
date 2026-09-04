#nullable disable

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Android.App;
using Android.Content;
<<<<<<< HEAD:src/Uno.UI/UI/Xaml/NativeApplication.cs
=======
using Android.OS;
using Android.Runtime;
using Java.Interop;
<<<<<<< HEAD:src/Uno.UI/UI/Xaml/NativeApplication.cs
>>>>>>> 50cd5beb (feat: Abstract away shared OnActivated and OnLaunched logic):src/Uno.UI.Runtime.Android/NativeApplication.cs
=======
using Microsoft.Windows.AppLifecycle;
>>>>>>> c82677c7 (feat!: Route all activations through AppInstance):src/Uno.UI.Runtime.Android/NativeApplication.cs
using Uno.Extensions;
using Uno.Foundation.Logging;
<<<<<<< HEAD:src/Uno.UI/UI/Xaml/NativeApplication.cs
using Windows.UI.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
=======
using Uno.UI.Hosting;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Application = Microsoft.UI.Xaml.Application;
>>>>>>> 50cd5beb (feat: Abstract away shared OnActivated and OnLaunched logic):src/Uno.UI.Runtime.Android/NativeApplication.cs
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
					_app.OnLaunched(new LaunchActivatedEventArgs());
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
						this.Log().LogDebug("Intent contained JumpList extra arguments, reporting a Launch activation.");
					}

<<<<<<< HEAD:src/Uno.UI/UI/Xaml/NativeApplication.cs
<<<<<<< HEAD:src/Uno.UI/UI/Xaml/NativeApplication.cs
#if ANDROID_SKIA
=======
					// The app does not exist yet at this point (the host is built in CreateHost() below),
					// so the arguments are stashed and picked up by Application.InvokeOnLaunched.
>>>>>>> 50cd5beb (feat: Abstract away shared OnActivated and OnLaunched logic):src/Uno.UI.Runtime.Android/NativeApplication.cs
					Application.SetArguments(intent.GetStringExtra(JumpListItem.ArgumentsExtraKey));
#else
					_app.OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, intent.GetStringExtra(JumpListItem.ArgumentsExtraKey)));
#endif
=======
					var arguments = intent.GetStringExtra(JumpListItem.ArgumentsExtraKey);

					if (!_isRunning)
					{
						// The app does not exist yet at this point (the host is built in CreateHost() below),
						// so the arguments are stashed for the LaunchActivatedEventArgs OnLaunched will get.
						Application.SetArguments(arguments);
					}

					ReportActivation(AppActivationArguments.CreateLaunch(
						new global::Windows.ApplicationModel.Activation.LaunchActivatedEventArgs(ActivationKind.Launch, arguments)));
>>>>>>> c82677c7 (feat!: Route all activations through AppInstance):src/Uno.UI.Runtime.Android/NativeApplication.cs
					handled = true;
				}
				else if (intent.Data != null)
				{
					if (Uri.TryCreate(intent.Data.ToString(), UriKind.Absolute, out var uri))
					{
						if (this.Log().IsEnabled(LogLevel.Debug))
						{
<<<<<<< HEAD:src/Uno.UI/UI/Xaml/NativeApplication.cs
							this.Log().LogDebug("Intent data parsed successfully as Uri, calling OnActivated.");
						}

<<<<<<< HEAD:src/Uno.UI/UI/Xaml/NativeApplication.cs
						_app.OnActivated(new ProtocolActivatedEventArgs(uri, _isRunning ? ApplicationExecutionState.Running : ApplicationExecutionState.NotRunning));
=======
						if (_isRunning)
						{
							// Application.Current rather than a cached instance: the app is created by the
							// host built in CreateHost(), which this type never sees.
							Application.Current?.InvokeOnActivated(new ProtocolActivatedEventArgs(uri, ApplicationExecutionState.Running));
						}
						else
						{
							Application.SetActivationUri(uri);
						}

>>>>>>> 50cd5beb (feat: Abstract away shared OnActivated and OnLaunched logic):src/Uno.UI.Runtime.Android/NativeApplication.cs
=======
							this.Log().LogDebug("Intent data parsed successfully as Uri, reporting a Protocol activation.");
						}

						ReportActivation(AppActivationArguments.CreateProtocol(
							new ProtocolActivatedEventArgs(
								uri,
								_isRunning ? ApplicationExecutionState.Running : ApplicationExecutionState.NotRunning)));
>>>>>>> c82677c7 (feat!: Route all activations through AppInstance):src/Uno.UI.Runtime.Android/NativeApplication.cs
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
		/// Hands an activation to <see cref="AppInstance"/>, which stores it for a cold start or raises
		/// <see cref="AppInstance.Activated"/> when the app is already up. Android delivers both through
		/// the same intent callbacks, and <see cref="AppInstance"/> — not this type — knows which case it is.
		/// </summary>
		private static void ReportActivation(AppActivationArguments args)
			=> AppInstance.GetCurrent().SetOrRaiseActivation(args);

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
