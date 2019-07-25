using System;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

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
			// Start the app only once, so the tests runs don't restart it
			// and gain some time for the tests.
			AppInitializer.ColdStartApp();
		}


		[SetUp]
		public void BeforeEachTest()
		{
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

			_app = AppInitializer.AttachToApp();

			Helpers.App = _app;
		}

		private Platform[] GetActivePlatforms()
		{
			if(TestContext.CurrentContext.Test.Properties["ActivePlatforms"].FirstOrDefault() is Platform[] platforms)
			{
				if(platforms.Length != 0)
				{
					return platforms;
				}
			}
			else
			{
				if(Type.GetType(TestContext.CurrentContext.Test.ClassName) is Type classType)
				{
					if(classType.GetCustomAttributes(typeof(ActivePlatformsAttribute), false) is ActivePlatformsAttribute[] attributes)
					{
						if(
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

		protected void Run(string metadataName)
		{
			_app.WaitForElement("sampleControl");

			var testRunId = _app.InvokeGeneric("browser:SampleRunner|RunTest", metadataName);

			_app.WaitFor(() =>
			{
				var result = _app.InvokeGeneric("browser:SampleRunner|IsTestDone", testRunId).ToString();
				return bool.TryParse(result, out var testDone) && testDone;
			});

			_app.Screenshot(metadataName.Replace(".", "_"));
		}
	}
}
