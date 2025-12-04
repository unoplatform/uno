using Foundation;
using System;
using System.Linq;
using UIKit;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel;
using ObjCRuntime;
using Windows.Globalization;
using Windows.Graphics.Display;
using Uno.Extensions;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Globalization;
using System.Threading;
using Uno.UI.Xaml.Controls;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace Microsoft.UI.Xaml
{
	[Register("UnoAppDelegate")]
	public partial class Application : UIApplicationDelegate
	{
		private bool _preventSecondaryActivationHandling;

		partial void InitializePartial()
		{
			InitializeSystemTheme();

			SetCurrentLanguage();
		}

		public Application(NativeHandle handle) : base(handle)
		{
		}

		static partial void StartPartial(Func<ApplicationInitializationCallbackParams, Application> callback)
		{
			return callback(new ApplicationInitializationCallbackParams());
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
			var handled = false;
			if (launchOptions != null)
			{
				if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUrlKey, out var urlObject))
				{
					_preventSecondaryActivationHandling = true;
					var url = (NSUrl)urlObject;
					if (TryParseUri(url, out var uri))
					{
						OnActivated(new ProtocolActivatedEventArgs(uri, ApplicationExecutionState.NotRunning));
						handled = true;
					}
				}
#if !__TVOS__
				else if (launchOptions.TryGetValue(UIApplication.LaunchOptionsShortcutItemKey, out var shortcutItemObject))
				{
					_preventSecondaryActivationHandling = true;
					var shortcutItem = (UIApplicationShortcutItem)shortcutItemObject;
					OnLaunched(new LaunchActivatedEventArgs(ActivationKind.Launch, shortcutItem.Type));
					handled = true;
				}
#endif
				else if (
					TryGetUserActivityFromLaunchOptions(launchOptions, out var userActivity) &&
					userActivity.ActivityType == NSUserActivityType.BrowsingWeb)
				{
					_preventSecondaryActivationHandling = true;
					if (TryParseUri(userActivity.WebPageUrl, out var uri))
					{
						OnActivated(new ProtocolActivatedEventArgs(uri, ApplicationExecutionState.NotRunning));
						handled = true;
					}
				}
			}

			// default to normal launch
			if (!handled)
			{
				OnLaunched(new LaunchActivatedEventArgs());
			}
			return true;
		}

		public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler) =>
			TryHandleUniversalLinkFromUserActivity(userActivity);

		public override void UserActivityUpdated(UIApplication application, NSUserActivity userActivity) =>
			TryHandleUniversalLinkFromUserActivity(userActivity);

		public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
		{
			// If the application was not running, URL was already handled by FinishedLaunching
			if (!_preventSecondaryActivationHandling)
			{
				if (TryParseUri(url, out var uri))
				{
					OnActivated(new ProtocolActivatedEventArgs(uri, ApplicationExecutionState.Running));
				}
			}
			_preventSecondaryActivationHandling = false;
			return true;
		}

		private DateTimeOffset GetSuspendingOffset() => DateTimeOffset.Now.AddSeconds(10);

		/// <summary>
		/// This method enables UI Tests to get the output path
		/// of the current application, in the context of the simulator.
		/// </summary>
		/// <returns>The host path to get the container</returns>
		[Export("getApplicationDataPath")]
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		public NSString GetWorkingFolder() => new NSString(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

		private bool TryHandleUniversalLinkFromUserActivity(NSUserActivity userActivity)
		{
			// If the application was not running, universal link was already handled by FinishedLaunching
			if (_preventSecondaryActivationHandling)
			{
				_preventSecondaryActivationHandling = false;
				return true;
			}

			if (userActivity.ActivityType == NSUserActivityType.BrowsingWeb)
			{
				if (TryParseUri(userActivity.WebPageUrl, out var uri))
				{
					OnActivated(new ProtocolActivatedEventArgs(uri, ApplicationExecutionState.Running));
					return true;
				}
			}

			return false;
		}

		private bool TryParseUri(NSUrl url, out Uri uri)
		{
			if (Uri.TryCreate(url.ToString(), UriKind.Absolute, out uri))
			{
				return true;
			}
			else
			{
				this.Log().LogError($"Activation URI {url} could not be parsed");
				return false;
			}
		}

		private bool TryGetUserActivityFromLaunchOptions(NSDictionary launchOptions, out NSUserActivity userActivity)
		{
			userActivity = null;

			if (launchOptions.TryGetValue(UIApplication.LaunchOptionsUserActivityDictionaryKey, out var userActivityObject) &&
				userActivityObject is NSDictionary userActivityDictionary)
			{
				userActivity = userActivityDictionary.Values.OfType<NSUserActivity>().FirstOrDefault();
			}

			return userActivity != null;
		}

		private void SetCurrentLanguage()
		{
			// net6.0-iOS does not automatically set the thread and culture info
			// https://github.com/xamarin/xamarin-macios/issues/14740
			var language = NSLocale.PreferredLanguages.ElementAtOrDefault(0);

			try
			{
				var cultureInfo = CultureInfo.CreateSpecificCulture(language);
				CultureInfo.CurrentUICulture = cultureInfo;
				CultureInfo.CurrentCulture = cultureInfo;
				Thread.CurrentThread.CurrentCulture = cultureInfo;
				Thread.CurrentThread.CurrentUICulture = cultureInfo;
			}
			catch (Exception ex)
			{
				this.Log().Error($"Failed to set current culture for language: {language}", ex);
			}
		}
	}
}
