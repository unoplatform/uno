using Foundation;
using System;
using AppKit;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using ObjCRuntime;
using Windows.Graphics.Display;
using Uno.UI.Services;

namespace Windows.UI.Xaml
{
	[Register("UnoAppDelegate")]
	public partial class Application : NSApplicationDelegate
	{
		private bool _suspended;
		internal bool IsSuspended => _suspended;

		public Application()
		{
			Current = this;
			Windows.UI.Xaml.GenericStyles.Initialize();
			ResourceHelper.ResourcesService = new ResourcesService(new[] { NSBundle.MainBundle });
		}

		public Application(IntPtr handle) : base(handle)
		{
		}

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			callback(new ApplicationInitializationCallbackParams());
		}

		public override void DidFinishLaunching(NSNotification notification)
		{
			OnLaunched(new LaunchActivatedEventArgs());
		}

        /// <summary>
        /// This method enables UI Tests to get the output path
        /// of the current application, in the context of the simulator.
        /// </summary>
        /// <returns>The host path to get the container</returns>
        [Export("getApplicationDataPath")]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public NSString GetWorkingFolder() => new NSString(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
	}
}
