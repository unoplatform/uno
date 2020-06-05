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
using System.Globalization;
using Uno.Extensions;
using Uno.Logging;
using System.Linq;
using Microsoft.Extensions.Logging;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace Windows.UI.Xaml
{
	[Register("UnoAppDelegate")]
	public partial class Application : NSApplicationDelegate
	{
		private NSUrl[] _launchUrls = null;

		public Application()
		{
			Current = this;
			SetCurrentLanguage();
			ResourceHelper.ResourcesService = new ResourcesService(new[] { NSBundle.MainBundle });
		}

		public Application(IntPtr handle) : base(handle)
		{

		}

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
				OnLaunched(new LaunchActivatedEventArgs());
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

		/// <summary>
		/// Based on <see cref="https://forums.developer.apple.com/thread/118974" />
		/// </summary>
		/// <returns>System theme</returns>
		private ApplicationTheme GetDefaultSystemTheme()
		{
			const string AutoSwitchKey = "AppleInterfaceStyleSwitchesAutomatically";
			var autoChange = NSUserDefaults.StandardUserDefaults[AutoSwitchKey];
			if (autoChange != null)
			{
				var autoChangeEnabled = NSUserDefaults.StandardUserDefaults.BoolForKey(AutoSwitchKey);
				if (autoChangeEnabled)
				{
					if (NSUserDefaults.StandardUserDefaults["AppleInterfaceStyle"] == null)
					{
						return ApplicationTheme.Dark;
					}
					else
					{
						return ApplicationTheme.Light;
					}
				}
			}
			if (NSUserDefaults.StandardUserDefaults["AppleInterfaceStyle"] == null)
			{
				return ApplicationTheme.Light;
			}
			else
			{
				return ApplicationTheme.Dark;
			}
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

		partial void ObserveSystemThemeChanges()
		{			
			NSUserDefaults.StandardUserDefaults.AddObserver(
				"AppleInterfaceStyle",
				NSKeyValueObservingOptions.New,
				_ => Application.Current.OnSystemThemeChanged());
		}
	}
}
