#if XAMARIN_ANDROID
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Java.Interop;
using Uno.UI.Services;
using Windows.ApplicationModel.Activation;
using Windows.UI.StartScreen;
using Android.Content;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Windows.UI.Xaml
{
	public class NativeApplication : Android.App.Application
	{
		private readonly Application _app;
		private Intent _lastHandledIntent;

		private bool _isRunning = false;

		public delegate Windows.UI.Xaml.Application AppBuilder();

		/// <summary>
		/// Creates an android Application instance
		/// </summary>
		/// <param name="appBuilder">A <see cref="AppBuilder"/> delegate that provides an <see cref="Application"/> instance.</param>
		public NativeApplication(AppBuilder appBuilder, IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			// Delay create the Windows.UI.Xaml.Application in order to get the
			// Android.App.Application.Context to be populated properly. This enables
			// APIs such as Windows.Storage.ApplicationData.Current.LocalSettings to function properly.
			_app = appBuilder();

			ResourceHelper.ResourcesService = new ResourcesService(this);
		}

		public override void OnCreate()
		{
			RegisterActivityLifecycleCallbacks(new ActivityCallbacks(this));
		}

		private void OnActivityStarted(Activity activity)
		{
			if (activity is ApplicationActivity)
			{
				_app.InitializationCompleted();

				var handled = TryHandleIntent(activity.Intent);

				// default to normal launch
				if (!handled && !_isRunning)
				{
					_app.OnLaunched(new LaunchActivatedEventArgs());
				}
				_isRunning = true;
			}
		}

		internal bool TryHandleIntent(Intent intent)
		{
			var handled = false;
			if (_lastHandledIntent != intent)
			{
				_lastHandledIntent = intent;
				if (intent?.Extras?.ContainsKey(JumpListItem.ArgumentsExtraKey) == true)
				{
					_app.OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, intent.GetStringExtra(JumpListItem.ArgumentsExtraKey)));
					handled = true;
				}
				else if (intent.Data != null)
				{
					if (Uri.TryCreate(intent.Data.ToString(), UriKind.Absolute, out var uri))
					{
						_app.OnActivated(new ProtocolActivatedEventArgs(uri, _isRunning ? ApplicationExecutionState.Running : ApplicationExecutionState.NotRunning));
						handled = true;
					}
					else
					{
						// log error and fall back to normal launch
						this.Log().LogError($"Activation URI {intent.Data} could not be parsed");
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
		[Android.Runtime.Preserve]
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
#endif
