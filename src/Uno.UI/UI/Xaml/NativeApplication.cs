#nullable disable
#pragma warning disable DNAA0001 // Application class 'NativeApplication' does not have an Activation Constructor (NativeApplication is used by apps, not by itself)

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Java.Interop;
using Windows.ApplicationModel.Activation;
using Windows.UI.StartScreen;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Uno.Extensions;
using Windows.Foundation.Metadata;
using System.ComponentModel;
using Uno.Foundation.Logging;
using Windows.UI.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;
using IOnPreDrawListener = Android.Views.ViewTreeObserver.IOnPreDrawListener;

namespace Microsoft.UI.Xaml
{
	public class NativeApplication : AApplication
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
		public NativeApplication(AppBuilder appBuilder, IntPtr javaReference, JniHandleOwnership transfer)
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
						this.Log().LogDebug("Intent contained JumpList extra arguments, calling OnLaunched.");
					}

#if ANDROID_SKIA
					Application.SetArguments(intent.GetStringExtra(JumpListItem.ArgumentsExtraKey));
#else
					_app.OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, intent.GetStringExtra(JumpListItem.ArgumentsExtraKey)));
#endif
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

#if ANDROID_SKIA
						if (_isRunning)
						{
							_app?.OnActivated(new ProtocolActivatedEventArgs(uri, ApplicationExecutionState.Running));
						}
						else
						{
							Application.SetActivationUri(uri);
						}
#else
						_app.OnActivated(new ProtocolActivatedEventArgs(uri, _isRunning ? ApplicationExecutionState.Running : ApplicationExecutionState.NotRunning));
#endif
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
#if NET10_0_OR_GREATER
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) =>
			throw new NotSupportedException("`static` methods with [Export] are not supported on NativeAOT.");
#else   // !NET10_0_OR_GREATER
		[Export(nameof(GetTypeAssemblyFullName))]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public static string GetTypeAssemblyFullName(string type) => Type.GetType(type)?.Assembly.FullName;
#endif  // !NET10_0_OR_GREATER

		private class ActivityCallbacks : Java.Lang.Object, IActivityLifecycleCallbacks
		{
			private readonly NativeApplication _app;

			public ActivityCallbacks(NativeApplication app)
			{
				_app = app;
			}

			public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
			{

			}

			public void OnActivityDestroyed(Activity activity)
			{

			}

			public void OnActivityPaused(Activity activity)
			{

			}

			public void OnActivityResumed(Activity activity)
			{
			}

			public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
			{

			}

			public void OnActivityStarted(Activity activity)
			{
				_app.OnActivityStarted(activity);
			}

			public void OnActivityStopped(Activity activity)
			{

			}
		}

	}
}
#endif
