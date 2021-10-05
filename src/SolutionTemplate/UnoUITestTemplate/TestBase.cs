using System;
using System.IO;
using NUnit.Framework;
using Uno.UITest;
using Uno.UITest.Selenium;
using Uno.UITests.Helpers;

namespace UnoUITestsLibrary
{
    public class TestBase
    {
        private IApp _app;

        static TestBase()
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

        protected IApp App
        {
            get => _app;
            private set
            {
                _app = value;
                Uno.UITest.Helpers.Queries.Helpers.App = value;
            }
        }

        [SetUp]
        public void SetUpTest()
        {
            App = AppInitializer.AttachToApp();
        }

        [TearDown]
        public void TearDownTest()
        {
            TakeScreenshot("teardown");
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
                var destFileName = Path
                    .Combine(Path.GetDirectoryName(fileInfo.FullName), title + Path.GetExtension(fileInfo.Name));

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

            return fileInfo;
        }

    }
}
