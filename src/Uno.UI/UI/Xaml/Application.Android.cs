#if XAMARIN_ANDROID
using System;
using Android.Content.Res;
using Android.OS;
using Uno.UI.Extensions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml.Controls.Primitives;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace Microsoft.UI.Xaml
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

		/// <remarks>
		/// The 5 second timeout seems to be the safest timeout for suspension activities.
		/// See - https://stackoverflow.com/a/3987733/732221
		/// </remarks>
		private SuspendingOperation CreateSuspendingOperation() =>
			new SuspendingOperation(DateTimeOffset.Now.AddSeconds(5), null);
	}
}
#endif
