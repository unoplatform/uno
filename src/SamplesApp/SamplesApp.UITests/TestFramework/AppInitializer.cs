using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Uno.UITest;
using Uno.UITest.Helpers.Queries;
using Uno.UITest.Xamarin.Extensions;

namespace SamplesApp.UITests
{
	public class AppInitializer
	{
		public const string UITestPlatform = "UNO_UITEST_PLATFORM";
		public const string UITEST_IOSBUNDLE_PATH = "UNO_UITEST_IOSBUNDLE_PATH";
		public const string UITEST_ANDROIDAPK_PATH = "UNO_UITEST_ANDROIDAPK_PATH";
		public const string UITEST_SCREENSHOT_PATH = "UNO_UITEST_SCREENSHOT_PATH";

		private const string DriverPath = @"..\..\..\..\SamplesApp.Wasm.UITests\node_modules\chromedriver\lib\chromedriver";
		private static IApp _currentApp;

		public static IApp ColdStartApp()
			=> Xamarin.UITest.TestEnvironment.Platform == Xamarin.UITest.TestPlatform.Local
				? StartApp(alreadyRunningApp: false)
				: null;

		public static IApp AttachToApp()
			=> StartApp(alreadyRunningApp: true);

		private static IApp StartApp(bool alreadyRunningApp)
		{
			switch (Xamarin.UITest.TestEnvironment.Platform)
			{
				case Xamarin.UITest.TestPlatform.TestCloudiOS:
					return Xamarin.UITest.ConfigureApp
						.iOS
						.StartApp(Xamarin.UITest.Configuration.AppDataMode.Clear)
						.ToUnoApp();

				case Xamarin.UITest.TestPlatform.TestCloudAndroid:
					return Xamarin.UITest.ConfigureApp
						.Android
						.StartApp(Xamarin.UITest.Configuration.AppDataMode.Clear)
						.ToUnoApp();

				default:
					var localPlatform = GetLocalPlatform();
					switch (GetLocalPlatform())
					{
						case Platform.Android:
							return CreateAndroidApp(alreadyRunningApp);

						case Platform.iOS:
							return CreateiOSApp(alreadyRunningApp);

						case Platform.Browser:
							if (alreadyRunningApp)
							{
								return CreateBrowserApp(alreadyRunningApp);
							}
							else
							{
								// Skip cold app start, there's no notion of reuse active browser yet.
								return null;
							}

						default:
							throw new Exception($"Platform {localPlatform} is not enabled.");
					}
			}
		}

		private static IApp CreateBrowserApp(bool alreadyRunningApp)
		{
			if(_currentApp != null)
			{
				if (!alreadyRunningApp)
				{
					_currentApp.Dispose();
				}
				else
				{
					return _currentApp;
				}
			}

			return _currentApp = Uno.UITest.Selenium.ConfigureApp
				.WebAssembly
				.Uri(new Uri(Constants.DefaultUri))
				.ChromeDriverLocation(Path.Combine(TestContext.CurrentContext.TestDirectory, DriverPath.Replace('\\', Path.DirectorySeparatorChar)))
				.ScreenShotsPath(TestContext.CurrentContext.TestDirectory)
#if DEBUG
				.Headless(false)
#endif
				.StartApp();
		}

		private static IApp CreateAndroidApp(bool alreadyRunningApp)
		{
#if DEBUG
			// To set in case of Xamarin.UITest errors
			//
			Environment.SetEnvironmentVariable("ANDROID_HOME", @"C:\Program Files (x86)\Android\android-sdk");
			Environment.SetEnvironmentVariable("JAVA_HOME", @"C:\Program Files\Android\Jdk\microsoft_dist_openjdk_1.8.0.25");
#endif

			var androidConfig = Xamarin.UITest.ConfigureApp
				.Android
				.Debug()
				.EnableLocalScreenshots();

			if (GetAndroidApkPath() is string bundlePath)
			{
				androidConfig = androidConfig.ApkFile(bundlePath);
			}
			else
			{
				androidConfig = androidConfig.InstalledApp(Constants.AndroidAppName);
			}

			var app = alreadyRunningApp
				? androidConfig.ConnectToApp()
				: androidConfig.StartApp();

			ApplyScreenShotPath();

			return app.ToUnoApp();
		}

		private static void ApplyScreenShotPath()
		{
			var value = Environment.GetEnvironmentVariable(UITEST_SCREENSHOT_PATH);
			if (!string.IsNullOrWhiteSpace(value))
			{
				Environment.CurrentDirectory = value;
			}
			else
			{
				Environment.CurrentDirectory = Path.GetDirectoryName(new Uri(typeof(AppInitializer).Assembly.Location).LocalPath);
			}
		}

		private static IApp CreateiOSApp(bool alreadyRunningApp)
		{
			var iOSConfig = Xamarin.UITest.ConfigureApp
				.iOS
				.Debug()
				.EnableLocalScreenshots();

			if (GetiOSAppBundlePath() is string bundlePath)
			{
				iOSConfig = iOSConfig.AppBundle(bundlePath);
			}
			else
			{
				iOSConfig = iOSConfig.InstalledApp(Constants.AndroidAppName);
			}

			var app = alreadyRunningApp
				? iOSConfig.ConnectToApp()
				: iOSConfig.StartApp();

			ApplyScreenShotPath();

			return app.ToUnoApp();
		}

		private static string GetAndroidApkPath()
		{
			var value = Environment.GetEnvironmentVariable(UITEST_ANDROIDAPK_PATH);
			return string.IsNullOrWhiteSpace(value) ? null : value;
		}

		private static string GetiOSAppBundlePath()
		{
			var value = Environment.GetEnvironmentVariable(UITEST_IOSBUNDLE_PATH);
			return string.IsNullOrWhiteSpace(value) ? null : value;
		}

		public static Platform GetLocalPlatform()
		{
			var uitestPlatform = Environment.GetEnvironmentVariable(UITestPlatform);
			if (!Enum.TryParse(uitestPlatform, out Platform retVal))
			{
				retVal = Constants.CurrentPlatform;
			}

			return retVal;
		}
	}
}
