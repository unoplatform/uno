#nullable disable
#pragma warning disable DNAA0001 // Application class 'NativeApplication' does not have an Activation Constructor (NativeApplication is used by apps, not by itself)

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Java.Interop;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using IOnPreDrawListener = Android.Views.ViewTreeObserver.IOnPreDrawListener;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Base <see cref="Android.App.Application"/> for an Uno Platform app. Derive from this type in the
	/// Android head, mark it with <see cref="ApplicationAttribute"/>, and override <see cref="CreateHost"/>.
	/// </summary>
	/// <remarks>
	/// Android has no managed entry point — the .NET for Android SDK rewrites <c>OutputType</c> from
	/// <c>Exe</c> to <c>Library</c>, and startup is driven by ART instantiating the class named in the
	/// manifest. <see cref="CreateHost"/> is therefore the Android equivalent of the <c>Main</c> method
	/// other targets use to build their <see cref="UnoPlatformHost"/>.
	/// </remarks>
	public abstract class NativeApplication : AApplication
	{
		private Intent _lastHandledIntent;

		private bool _isRunning;

		protected NativeApplication(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			// Register assemblies earlier than Application itself, otherwise
			// ApiInformation may return APIs as not implemented incorrectly.
			ApiInformation.RegisterAssembly(typeof(Application).Assembly);
			ApiInformation.RegisterAssembly(typeof(global::Windows.Storage.ApplicationData).Assembly);
			ApiInformation.RegisterAssembly(typeof(Microsoft.UI.Composition.Compositor).Assembly);
		}

		/// <summary>
		/// Builds the <see cref="UnoPlatformHost"/> that runs this application.
		/// </summary>
		/// <example>
		/// <code>
		/// protected override UnoPlatformHost CreateHost() =&gt;
		/// 	UnoPlatformHostBuilder.Create()
		/// 		.App(() =&gt; new App())
		/// 		.UseAndroid()
		/// 		.Build();
		/// </code>
		/// </example>
		/// <remarks>
		/// Called once, when the first <see cref="ApplicationActivity"/> starts — late enough for
		/// <see cref="Uno.UI.ContextHelper.Current"/> to be set. Do not perform UI work in
		/// <see cref="OnCreate"/> instead: it also runs for process entries that have no activity,
		/// such as background services and broadcast receivers.
		/// </remarks>
		protected abstract UnoPlatformHost CreateHost();

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

				// We need to call TryHandleIntent first so the application arguments are set correctly.
				// Then, when the Application is created, it will use those arguments.
				_ = TryHandleIntent(activity.Intent);
				if (!_isRunning)
				{
					StartHost();
				}

				_isRunning = true;
			}
		}

		private void StartHost()
		{
			try
			{
				// The host is created this late for ContextHelper.Current, set by BaseActivity,
				// to have been populated.
				CreateHost().Run();
			}
			catch (Exception e)
			{
				// Mono truncates managed stacks thrown out of Activity.OnStart, so log before rethrowing.
				this.Log().Error("The Uno Platform host failed to start.", e);
				throw;
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

					// The app does not exist yet at this point (the host is built in CreateHost() below),
					// so the arguments are stashed and picked up by Application.InvokeOnLaunched.
					Application.SetArguments(intent.GetStringExtra(JumpListItem.ArgumentsExtraKey));
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
