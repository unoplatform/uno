#if XAMARIN_ANDROID
using System;
using Android.Content.Res;
using Android.OS;
using Uno.UI.Extensions;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		public Application()
		{
			Window.Current.ToString();
			Current = this;
			PermissionsHelper.Initialize();
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			callback(new ApplicationInitializationCallbackParams());
		}

		partial void OnResumingPartial()
		{
			Resuming?.Invoke(null, null);
		}

		partial void OnSuspendingPartial()
		{
			Suspending?.Invoke(this, new Windows.ApplicationModel.SuspendingEventArgs(new Windows.ApplicationModel.SuspendingOperation(DateTime.Now.AddSeconds(30))));
		}

		protected virtual void OnBackgroundActivated(BackgroundActivatedEventArgs args) { }

		public void Exit()
		{
			Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
		}
	}
}
#endif
