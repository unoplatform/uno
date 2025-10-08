using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_Storage_Pickers
{
    [TestFixture]
    public class FileOpenPicker_Tests : SampleControlUITestBase
    {
        [Test]
        [AutoRetry]
        [ActivePlatforms(Platform.Browser)] // File picker is currently only supported in WebAssembly
        public async Task FileOpenPicker_BasicTest()
        {
            try
            {
                // Navigate to the test page
                Run("UITests.Shared.Windows_Storage_Pickers.FileOpenPickerTests", skipInitialScreenshot: true);

                // Wait for the test page to load
                _app.WaitForElement("SelectFileButton");

                // Take a screenshot before opening the picker
                TakeScreenshot("Before opening file picker");

                // Click the button to open the file picker
                _app.FastTap("SelectFileButton");

                // Note: In a real test, you would interact with the native file picker here
                // However, this is not possible in automated UI tests, so we'll just verify the test page
                // has the expected elements and behaves correctly

                // Wait for the result text to be updated
                var resultText = _app.Marked("ResultText");
                _app.WaitForDependencyPropertyValue(resultText, "Text", "File selected successfully");

                // Verify the file name is displayed
                var fileNameText = _app.Marked("FileNameText");
                var fileName = fileNameText.GetDependencyPropertyValue("Text")?.ToString();
                Assert.IsNotNullOrEmpty(fileName, "File name should not be empty");

                // Take a screenshot after the test completes
                TakeScreenshot("After file selection");
            }
            catch (Exception ex)
            {
                // Take a screenshot on failure
                TakeScreenshot("TestFailure");
                throw;
            }
        }
    }
}
