#if XAMARIN_ANDROID
using System;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml
{
	public partial class Application
	{
		public Application()
		{
			Windows.UI.Xaml.GenericStyles.Initialize();
			Window.Current.ToString();
			Current = this;
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			callback(new ApplicationInitializationCallbackParams());
		}

		internal void OnResuming()
		{
			Resuming?.Invoke(null, null);
		}

		internal void OnSuspending()
		{
			Suspending?.Invoke(this, new ApplicationModel.SuspendingEventArgs(new ApplicationModel.SuspendingOperation(DateTime.Now.AddSeconds(30))));
		}
	}
}
#endif
