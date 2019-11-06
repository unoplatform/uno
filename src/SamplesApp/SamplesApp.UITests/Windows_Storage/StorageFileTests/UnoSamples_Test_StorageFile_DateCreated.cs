using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_Storage.StorageFileTests
{
	[TestFixture]
	class UnoSamples_Test_StorageFile_DateCreated : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void StorageFile_DateCreated()
		{
			Run("UITests.Shared.Windows_Storage.StorageFileTests.StorageFile_DateCreated");

			_app.WaitForElement(_app.Marked("sfdButtonCreate"));

			TakeScreenshot("Initial");

			var resultMessage = _app.Marked("sfdResult");

			// step 1: create file
			_app.Tap(_app.Marked("sfdButtonCreate"));

			_app.WaitForText(resultMessage, "created");  // timeout here means: something impossible?

			// step 2: test date just after create
			_app.Tap(_app.Marked("sfdButtonTest"));
			_app.WaitForText(resultMessage, "success");   // timeout here means: date is wrong
			TakeScreenshot("AfterFirstCheck");

			// step 3: test date several seconds later
			_app.Wait(TimeSpan.FromSeconds(30));
			_app.Tap(_app.Marked("sfdButtonTest"));
			_app.WaitForText(resultMessage, "success");   // timeout here means: DateCreated returns other date (maybe current date)

			TakeScreenshot("AfterSuccess");

		}
	}
}
