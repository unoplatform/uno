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

namespace Windows.UI.Xaml
{
	public class NativeApplication : Android.App.Application
	{
		private readonly Application _app;
		private Intent _lastHandledIntent;

		public NativeApplication(Windows.UI.Xaml.Application app, IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{
			_app = app;
			ResourceHelper.ResourcesService = new ResourcesService(this);
		}

		public override void OnCreate()
		{
			RegisterActivityLifecycleCallbacks(new ActivityCallbacks(this));
		}

		private void OnActivityStarted(Activity activity)
		{
			_app.InitializationCompleted();
			if (_lastHandledIntent != activity.Intent &&
			    activity.Intent?.Extras?.ContainsKey(JumpListItem.ArgumentsExtraKey) == true)
			{
				_lastHandledIntent = activity.Intent;
				_app.OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, activity.Intent.GetStringExtra(JumpListItem.ArgumentsExtraKey)));
			}
			else
			{
				_app.OnLaunched(new LaunchActivatedEventArgs());
			}
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
