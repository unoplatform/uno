#if XAMARIN_ANDROID
using System;
using Uno.UI.Extensions;
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
			Suspending?.Invoke(this, new ApplicationModel.SuspendingEventArgs(new ApplicationModel.SuspendingOperation(DateTime.Now.AddSeconds(30))));
		}
	}
}
#endif
