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

		public SampleControlUITestBase()
		{
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
				TakeScreenshot($"{TestContext.CurrentContext.Test.Name} - Tear down on error");
			}
		}

		public FileInfo TakeScreenshot(string stepName)
		{
			var title = $"{TestContext.CurrentContext.Test.Name}_{stepName}"
				.Replace(" ", "_")
				.Replace(".", "_");

			var fileInfo = _app.Screenshot(title);

			var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.Name);
			if (fileNameWithoutExt != title)
			{
				var destFileName = Path.Combine(Path.GetDirectoryName(fileInfo.FullName), title + Path.GetExtension(fileInfo.Name));

				if (File.Exists(destFileName))
				{
					File.Delete(destFileName);
				}

				File.Move(fileInfo.FullName, destFileName);

				TestContext.AddTestAttachment(destFileName, stepName);

				return new FileInfo(destFileName);
			}
			else
			{
				TestContext.AddTestAttachment(fileInfo.FullName, stepName);

				return fileInfo;
			}
		}

		public void AssertScreenshotsAreEqual(FileInfo expected, FileInfo actual, IAppRect rect)
			=> AssertScreenshotsAreEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));

		public void AssertScreenshotsAreEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MaxValue);
			AssertScreenshotsAreEqual(expected, rect.Value, actual, rect.Value);
		}

		public void AssertScreenshotsAreEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect)
			=> AssertScreenshotsAreEqual(
				expected,
				new Rectangle((int)expectedRect.X, (int)expectedRect.Y, (int)expectedRect.Width, (int)expectedRect.Height),
				actual,
				new Rectangle((int)actualRect.X, (int)actualRect.Y, (int)actualRect.Width, (int)actualRect.Height));

		public void AssertScreenshotsAreEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect)
		{
			Assert.AreEqual(expectedRect.Width, actualRect.Width, "Rect Width");
			Assert.AreEqual(expectedRect.Height, actualRect.Height, "Rect Height");

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Screenshot Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Screenshot Height");

				for (var x = 0; x < Math.Min(expectedRect.Width, expectedBitmap.Size.Width); x++)
				for (var y = 0; y < Math.Min(expectedRect.Height, expectedBitmap.Size.Height); y++)
				{
					Assert.AreEqual(
						expectedBitmap.GetPixel(x + expectedRect.X, y + expectedRect.Y),
						actualBitmap.GetPixel(x + actualRect.X, y + actualRect.Y),
						$"Pixel {x},{y}");
				}
			}
		}

		public void AssertScreenshotsAreNotEqual(FileInfo expected, FileInfo actual, IAppRect rect)
			=> AssertScreenshotsAreNotEqual(expected, actual, new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
		public void AssertScreenshotsAreNotEqual(FileInfo expected, FileInfo actual, Rectangle? rect = null)
		{
			rect = rect ?? new Rectangle(0, 0, int.MaxValue, int.MinValue);
			AssertScreenshotsAreNotEqual(expected, rect.Value, actual, rect.Value);
		}

		public void AssertScreenshotsAreNotEqual(FileInfo expected, IAppRect expectedRect, FileInfo actual, IAppRect actualRect)
			=> AssertScreenshotsAreNotEqual(
				expected,
				new Rectangle((int)expectedRect.X, (int)expectedRect.Y, (int)expectedRect.Width, (int)expectedRect.Height),
				actual,
				new Rectangle((int)actualRect.X, (int)actualRect.Y, (int)actualRect.Width, (int)actualRect.Height));

		public void AssertScreenshotsAreNotEqual(FileInfo expected, Rectangle expectedRect, FileInfo actual, Rectangle actualRect)
		{
			Assert.AreEqual(expectedRect.Width, actualRect.Width, "Rect Width");
			Assert.AreEqual(expectedRect.Height, actualRect.Height, "Rect Height");

			using (var expectedBitmap = new Bitmap(expected.FullName))
			using (var actualBitmap = new Bitmap(actual.FullName))
			{
				Assert.AreEqual(expectedBitmap.Size.Width, actualBitmap.Size.Width, "Screenshot Width");
				Assert.AreEqual(expectedBitmap.Size.Height, actualBitmap.Size.Height, "Screenshot Height");

				for (var x = 0; x < Math.Min(expectedRect.Width, expectedBitmap.Size.Width); x++)
				for (var y = 0; y < Math.Min(expectedRect.Height, expectedBitmap.Size.Height); y++)
				{
					if (expectedBitmap.GetPixel(x + expectedRect.X, y + expectedRect.Y) != actualBitmap.GetPixel(x + actualRect.X, y + actualRect.Y))
					{
						return;
					}
				}

				Assert.Fail("Screenshots are equals.");
			}
		}

		private static void ValidateAutoRetry()
		{
			var testType = Type.GetType(TestContext.CurrentContext.Test.ClassName);
			var methodInfo = testType?.GetMethod(TestContext.CurrentContext.Test.MethodName);
			if (methodInfo?.GetCustomAttributes(typeof(AutoRetryAttribute), true).Length == 0 && false)
			{
				Assert.Fail($"The AutoRetryAttribute is not defined for this test");
			}
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

		protected void Run(string metadataName, bool waitForSampleControl = true)
		{
			if (waitForSampleControl)
			{
				_app.WaitForElement("sampleControl");
			}

			var testRunId = _app.InvokeGeneric("browser:SampleRunner|RunTest", metadataName);

			_app.WaitFor(() =>
			{
				var result = _app.InvokeGeneric("browser:SampleRunner|IsTestDone", testRunId).ToString();
				return bool.TryParse(result, out var testDone) && testDone;
			});

			TakeScreenshot(metadataName.Replace(".", "_"));
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

	}
}
