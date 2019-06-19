using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Uno.UITest;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests
{
	public class AppInitializer
	{
		public const string UITestPlatform = "UITEST_PLATFORM";
		private const string DriverPath = @"..\..\..\..\SamplesApp.Wasm.UITests\node_modules\chromedriver\lib\chromedriver";

		public static IApp StartApp()
		{
			switch (Xamarin.UITest.TestEnvironment.Platform)
			{
				//case Xamarin.UITest.TestPlatform.TestCloudiOS:
				//	return ConfigureApp
				//		.iOS
				//		.StartApp(Xamarin.UITest.Configuration.AppDataMode.Clear);

				//case TestPlatform.TestCloudAndroid:
				//	return ConfigureApp
				//		.Android
				//		.StartApp(Xamarin.UITest.Configuration.AppDataMode.Clear);

				default:
					var localPlatform = GetLocalPlatform();
					switch (GetLocalPlatform())
					{
						//case Platform.Android:
						//	return ConfigureApp
						//		.Android
						//		.Debug()
						//		.EnableLocalScreenshots()
						//		.InstalledApp("com.nventive.uno.samples")
						//		.StartApp();

						//case Platform.iOS:
						//	return ConfigureApp
						//		.iOS
						//		.Debug()
						//		.EnableLocalScreenshots()
						//		.InstalledApp("com.nventive.uno.samples")
						//		.StartApp();

						case Platform.Browser:
							return Uno.UITest.Selenium.ConfigureApp
								.WebAssembly
								.Uri(new Uri(Constants.DefaultUri))
								.ChromeDriverLocation(Path.Combine(TestContext.CurrentContext.TestDirectory, DriverPath.Replace('\\', Path.DirectorySeparatorChar)))
								.ScreenShotsPath(TestContext.CurrentContext.TestDirectory)
#if DEBUG
								.Headless(false)
#endif
								.StartApp();


						default:
							throw new Exception($"Platform {localPlatform} is not enabled.");
					}
			}
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
