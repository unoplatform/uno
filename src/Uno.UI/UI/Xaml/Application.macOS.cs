using Foundation;
using System;
using AppKit;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel;
using Windows.Globalization;
using System.Globalization;
using Uno.Foundation.Logging;
using System.Linq;
using Uno.Helpers.Theming;

using Selector = ObjCRuntime.Selector;
using Windows.UI.Core;
using Uno.Foundation.Extensibility;
using Uno.UI.Runtime.MacOS;
using Uno.UI.Xaml.Controls;
#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

using NativeHandle = ObjCRuntime.NativeHandle;

namespace Microsoft.UI.Xaml
{
	[Register("UnoAppDelegate")]
	public partial class Application : NSApplicationDelegate
	{
		private NSUrl[] _launchUrls;

		static partial void InitializePartialStatic()
		{
			ApiExtensibility.Register(typeof(IUnoCorePointerInputSource), host => new MacOSPointerInputSource((Uno.UI.Controls.Window)NativeWindowWrapper.Instance.NativeWindow));
		}

		partial void InitializePartial()
		{
			SetCurrentLanguage();

			SubscribeBackgroundNotifications();
		}

		public Application(NativeHandle handle) : base(handle)
		{

		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) => true;

		static partial void StartPartial(ApplicationInitializationCallback callback)
		{
			callback(new ApplicationInitializationCallbackParams());
		}

		public override void OpenUrls(NSApplication application, NSUrl[] urls)
		{
			if (!_initializationComplete)
			{
				_launchUrls = urls;
			}
			else
			{
				// application is already running, we just try to activate it
				// if passed-in URIs are valid
				TryHandleUrlActivation(urls, ApplicationExecutionState.Running);
			}
		}

		public override void DidFinishLaunching(NSNotification notification)
		{
			InitializationCompleted();
			var handled = false;
			if (_launchUrls != null)
			{
				handled = TryHandleUrlActivation(_launchUrls, ApplicationExecutionState.NotRunning);
			}
			if (!handled)
			{
				var argumentsString = GetCommandLineArgsWithoutExecutable();

				OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, argumentsString));
			}
		}

		/// <summary>
		/// This method enables UI Tests to get the output path
		/// of the current application, in the context of the simulator.
		/// </summary>
		/// <returns>The host path to get the container</returns>
		[Export("getApplicationDataPath")]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public NSString GetWorkingFolder() => new NSString(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

		private bool TryHandleUrlActivation(NSUrl[] urls, ApplicationExecutionState previousState)
		{
			var handled = false;
			foreach (var url in urls)
			{
				if (Uri.TryCreate(url.ToString(), UriKind.Absolute, out var uri))
				{
					OnActivated(new ProtocolActivatedEventArgs(uri, previousState));
					// now the app is certainly running
					previousState = ApplicationExecutionState.Running;
					handled = true;
				}
				else
				{
					this.Log().LogError($"Activation URI {url} could not be parsed");
				}
			}

			// at least one URI must be valid for activation to be handled
			return handled;
		}

		private void SetCurrentLanguage()
		{
			var language = NSLocale.PreferredLanguages.ElementAtOrDefault(0);

			try
			{
				var cultureInfo = CultureInfo.CreateSpecificCulture(language);
				CultureInfo.CurrentUICulture = cultureInfo;
				CultureInfo.CurrentCulture = cultureInfo;
			}
			catch (Exception ex)
			{
				this.Log().Error($"Failed to set culture for language: {language}", ex);
			}
		}

		private void SubscribeBackgroundNotifications()
		{
			NSNotificationCenter.DefaultCenter.AddObserver(NSApplication.ApplicationHiddenNotification, OnEnteredBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(NSApplication.ApplicationShownNotification, OnLeavingBackground);
			NSNotificationCenter.DefaultCenter.AddObserver(NSApplication.ApplicationActivatedNotification, OnActivated);
			NSNotificationCenter.DefaultCenter.AddObserver(NSApplication.ApplicationDeactivatedNotification, OnDeactivated);
		}

		private void OnEnteredBackground(NSNotification notification)
		{
			NativeWindowWrapper.Instance?.OnNativeVisibilityChanged(false);

			RaiseEnteredBackground(null);
		}

		private void OnLeavingBackground(NSNotification notification)
		{
			RaiseResuming();
			RaiseLeavingBackground(() => NativeWindowWrapper.Instance.OnNativeVisibilityChanged(true));
		}

		private void OnActivated(NSNotification notification)
		{
			NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.CodeActivated);
		}

		private void OnDeactivated(NSNotification notification)
		{
			NativeWindowWrapper.Instance.OnNativeActivated(CoreWindowActivationState.Deactivated);
		}
	}
}
