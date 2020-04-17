#if XAMARIN_IOS
using Foundation;
using System;
using UIKit;
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
	public partial class Application : UIApplicationDelegate
	{
		private bool _suspended;
		internal bool IsSuspended => _suspended;

		private bool _preventSecondaryActivationHandling = false;

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

		/// <summary>
		/// Used to handle application launch. Previously used <see cref="FinishedLaunching(UIApplication)" />
		/// which however does not support launch with arguments and is "technically" deprecated.
		/// </summary>
		/// <param name="application">UI Application.</param>
		/// <param name="launchOptions">Launch options.</param>
		/// <returns>Value indicating whether launch can be handled.</returns>
		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
		{
			InitializationCompleted();
			if (launchOptions != null)
			{
				if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUrlKey, out var urlObject))
				{
					_preventSecondaryActivationHandling = true;
					var url = (NSUrl)urlObject;
					OnActivated(new ProtocolActivatedEventArgs(new Uri(url.ToString()), ApplicationExecutionState.NotRunning));
					return true;
				}
				else if (launchOptions.TryGetValue(UIApplication.LaunchOptionsShortcutItemKey, out var shortcutItemObject))
				{
					_preventSecondaryActivationHandling = true;
					var shortcutItem = (UIApplicationShortcutItem)shortcutItemObject;
					OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, shortcutItem.Type));
					return true;
				}
			}
			OnLaunched(new LaunchActivatedEventArgs());
			return true;
		}

		public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
		{
			// If the application was not running, URL was already handled by FinishedLaunching
			if (!_preventSecondaryActivationHandling)
			{
				OnActivated(new ProtocolActivatedEventArgs(new Uri(url.ToString()), ApplicationExecutionState.Running));
			}
			_preventSecondaryActivationHandling = false;
			return true;
		}

		public override void PerformActionForShortcutItem(
			UIApplication application,
			UIApplicationShortcutItem shortcutItem,
			UIOperationHandler completionHandler)
		{
			if (!_preventSecondaryActivationHandling)
			{
				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, shortcutItem.Type));
			}
			_preventSecondaryActivationHandling = false;
		}

		public override void DidEnterBackground(UIApplication application)
			=> OnSuspending();

		partial void OnSuspendingPartial()
		{
			var operation = new SuspendingOperation(DateTime.Now.AddSeconds(10));

			Suspending?.Invoke(this, new ApplicationModel.SuspendingEventArgs(operation));

			_suspended = true;
		}

		public override void WillEnterForeground(UIApplication application)
			=> OnResuming();

		partial void OnResumingPartial()
		{
			if (_suspended)
			{
				_suspended = false;

				Resuming?.Invoke(this, null);
			}
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, [Transient] UIWindow forWindow)
		{
			return DisplayInformation.AutoRotationPreferences.ToUIInterfaceOrientationMask();
		}

		/// <summary>
		/// This method enables UI Tests to get the output path
		/// of the current application, in the context of the simulator.
		/// </summary>
		/// <returns>The host path to get the container</returns>
		[Export("getApplicationDataPath")]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public NSString GetWorkingFolder() => new NSString(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

		private ApplicationTheme GetDefaultSystemTheme()
		{
			//Ensure the current device is running 12.0 or higher, because `TraitCollection.UserInterfaceStyle` was introduced in iOS 12.0
			if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
			{
				if (UIScreen.MainScreen.TraitCollection.UserInterfaceStyle == UIUserInterfaceStyle.Dark)
				{
					return ApplicationTheme.Dark;
				}
			}
			return ApplicationTheme.Light;
		}
	}
}
#endif
