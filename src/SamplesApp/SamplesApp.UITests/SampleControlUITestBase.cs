using System;
using System.Drawing;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests
{
	public class SampleControlUITestBase
	{
		protected IApp _app;
		private static int _totalTestFixtureCount;
		private double? _scaling;

		public SampleControlUITestBase()
		{
		}

		[OneTimeSetUp]
		public void SingleSetup()
		{
			ValidateAppMode();
		}

		static SampleControlUITestBase()
		{
			AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
			AppInitializer.TestEnvironment.WebAssemblyDefaultUri = Constants.WebAssemblyDefaultUri;
			AppInitializer.TestEnvironment.iOSAppName = Constants.iOSAppName;
			AppInitializer.TestEnvironment.AndroidAppName = Constants.AndroidAppName;
			AppInitializer.TestEnvironment.iOSDeviceNameOrId = Constants.iOSDeviceNameOrId;
			AppInitializer.TestEnvironment.CurrentPlatform = Constants.CurrentPlatform;

#if DEBUG
			AppInitializer.TestEnvironment.WebAssemblyHeadless = false;
#endif

			// Start the app only once, so the tests runs don't restart it
			// and gain some time for the tests.
			AppInitializer.ColdStartApp();
		}

		public void ValidateAppMode()
		{
			if(GetCurrentFixtureAttributes<TestAppModeAttribute>().FirstOrDefault() is TestAppModeAttribute testAppMode)
			{
				if(
					_totalTestFixtureCount != 0
					&& testAppMode.CleanEnvironment
					&& testAppMode.Platform == AppInitializer.GetLocalPlatform()
				)
				{
					// If this is not the first run, and the fixture requested a clean environment, request a cold start.
					// If this is the first run, as the app is cold-started during the type constructor, we can skip this.
					_app = AppInitializer.ColdStartApp();
				}
			}

			_totalTestFixtureCount++;
		}

		[SetUp]
		[AutoRetry]
		public void BeforeEachTest()
		{
			ValidateAutoRetry();

			// Check if the test needs to be ignore or not
			// If nothing specified, it is considered as a global test
			var platforms = GetActivePlatforms();
			if (platforms.Length != 0)
			{
				// Otherwise, we need to define on which platform the test is running and compare it with targeted platform
				var shouldIgnore = false;
				var currentPlatform = AppInitializer.GetLocalPlatform();

				if (_app is Uno.UITest.Xamarin.XamarinApp xa)
				{
					if (Xamarin.UITest.TestEnvironment.Platform == Xamarin.UITest.TestPlatform.Local)
					{
						shouldIgnore = !platforms.Contains(currentPlatform);
					}
					else
					{
						var testCloudPlatform = Xamarin.UITest.TestEnvironment.Platform == Xamarin.UITest.TestPlatform.TestCloudiOS
							? Platform.iOS
							: Platform.Android;

						shouldIgnore = !platforms.Contains(testCloudPlatform);
					}
				}
				else
				{
					shouldIgnore = !platforms.Contains(currentPlatform);
				}

				if (shouldIgnore)
				{
					var list = string.Join(", ", platforms.Select(p => p.ToString()));

					Assert.Ignore($"This test is ignored on this platform (runs on {list})");
				}
			}

			var app = AppInitializer.AttachToApp();
			_app = app ?? _app;

			Helpers.App = _app;
		}

		[TearDown]
		public void AfterEachTest()
		{
			if (
				TestContext.CurrentContext.Result.Outcome != ResultState.Success
				&& TestContext.CurrentContext.Result.Outcome != ResultState.Skipped
				&& TestContext.CurrentContext.Result.Outcome != ResultState.Ignored
			)
			{
				TakeScreenshot($"{TestContext.CurrentContext.Test.Name} - Tear down on error", ignoreInSnapshotCompare: true);
			}
		}

		public FileInfo TakeScreenshot(string stepName, bool? ignoreInSnapshotCompare = null)
			=> TakeScreenshot(
				stepName,
				ignoreInSnapshotCompare != null
					? new ScreenshotOptions { IgnoreInSnapshotCompare = ignoreInSnapshotCompare.Value }
					: null
			);

		public FileInfo TakeScreenshot(string stepName, ScreenshotOptions options)
		{
			if(_app == null)
			{
				Console.WriteLine($"Skipping TakeScreenshot _app is not available");
				return null;
			}

			var title = $"{TestContext.CurrentContext.Test.Name}_{stepName}"
				.Replace(" ", "_")
				.Replace(".", "_");

			var fileInfo = _app.Screenshot(title);

			var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
			if (fileNameWithoutExt != title)
			{
				var destFileName = Path
					.Combine(Path.GetDirectoryName(fileInfo.FullName), title + Path.GetExtension(fileInfo.Name))
					.GetNormalizedLongPath();

				if (File.Exists(destFileName))
				{
					File.Delete(destFileName);
				}

				File.Move(fileInfo.FullName, destFileName);

				TestContext.AddTestAttachment(destFileName, stepName);

				fileInfo = new FileInfo(destFileName);
			}
			else
			{
				TestContext.AddTestAttachment(fileInfo.FullName, stepName);
			}

			if(options != null)
			{
				SetOptions(fileInfo, options);
			}

			return fileInfo;
		}

		public void SetOptions(FileInfo screenshot, ScreenshotOptions options)
		{
			var fileName = Path
				.Combine(screenshot.DirectoryName, Path.GetFileNameWithoutExtension(screenshot.FullName) + ".metadata")
				.GetNormalizedLongPath();

			File.WriteAllText(fileName, $"IgnoreInSnapshotCompare={options.IgnoreInSnapshotCompare}");
		}

		private static void ValidateAutoRetry()
		{
			if (GetCurrentTestAttributes<AutoRetryAttribute>().Length == 0)
			{
				Assert.Fail($"The AutoRetryAttribute is not defined for this test");
			}
		}

		private static T[] GetCurrentFixtureAttributes<T>() where T : Attribute
		{
			var testType = Type.GetType(TestContext.CurrentContext.Test.ClassName);
			return testType?.GetCustomAttributes(typeof(T), true) is T[] array ? array : new T[0];
		}

		private static T[] GetCurrentTestAttributes<T>() where T : Attribute
		{
			var testType = Type.GetType(TestContext.CurrentContext.Test.ClassName);
			var methodInfo = testType?.GetMethod(TestContext.CurrentContext.Test.MethodName);
			return methodInfo?.GetCustomAttributes(typeof(T), true) is T[] array ? array : new T[0];
		}

		private Platform[] GetActivePlatforms()
		{
			if (TestContext.CurrentContext.Test.Properties["ActivePlatforms"].FirstOrDefault() is Platform[] platforms)
			{
				if (platforms.Length != 0)
				{
					return platforms;
				}
			}
			else
			{
				if (Type.GetType(TestContext.CurrentContext.Test.ClassName) is Type classType)
				{
					if (classType.GetCustomAttributes(typeof(ActivePlatformsAttribute), false) is ActivePlatformsAttribute[] attributes)
					{
						if (
							attributes.Length != 0
							&& attributes[0]
								.Properties["ActivePlatforms"]
								.OfType<object>()
								.FirstOrDefault() is Platform[] platforms2)
						{
							return platforms2;
						}
					}
				}
			}

			return Array.Empty<Platform>();
		}

		protected void Run(string metadataName, bool waitForSampleControl = true, bool skipInitialScreenshot = false, int sampleLoadTimeout = 5)
		{
			if (waitForSampleControl)
			{
				var sampleControlQuery = AppInitializer.GetLocalPlatform() == Platform.Browser
					? new QueryEx(q => q.Marked("sampleControl"))
					: new QueryEx(q => q.All().Marked("sampleControl"));

				_app.WaitForElement(sampleControlQuery, timeout: TimeSpan.FromSeconds(sampleLoadTimeout));
			}

			var testRunId = _app.InvokeGeneric("browser:SampleRunner|RunTest", metadataName);

			_app.WaitFor(() =>
			{
				var result = _app.InvokeGeneric("browser:SampleRunner|IsTestDone", testRunId).ToString();
				return bool.TryParse(result, out var testDone) && testDone;
			}, retryFrequency: TimeSpan.FromMilliseconds(50));

			if (!skipInitialScreenshot)
			{
				TakeScreenshot(metadataName.Replace(".", "_"));
			}
		}

		internal IAppRect GetScreenDimensions()
		{
			if (AppInitializer.GetLocalPlatform() == Platform.Browser)
			{
				var sampleControl = _app.Marked("sampleControl");

				return _app.WaitForElement(sampleControl).First().Rect;
			}
			else
			{
				return _app.GetScreenDimensions();
			}
		}

		internal double GetDisplayScreenScaling()
		{
			var scalingRaw = _app.InvokeGeneric("browser:SampleRunner|GetDisplayScreenScaling", "0");

			if (_scaling == null)
			{
				if (double.TryParse(scalingRaw?.ToString(), out var scaling))
				{
					Console.WriteLine($"Display Scaling: {scaling}");
					_scaling = scaling / 100;
				}
				else
				{
					_scaling = 1;
				}
			}

			return _scaling.Value;
		}
	}
}
