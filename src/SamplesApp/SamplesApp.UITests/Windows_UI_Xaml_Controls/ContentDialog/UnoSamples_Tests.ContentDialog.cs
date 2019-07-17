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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TimePickerTests
{
	[TestFixture]
	public partial class ContentDialog_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Primary()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog1");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert inital state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Primary) + " - Primary Button");

			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Primary");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Primary_Disabled()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog3");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert inital state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Primary) + " - Primary Button");

			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Undefined");

			var secondaryButton = _app.Marked("SecondaryButton");
			_app.WaitForElement(secondaryButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Primary) + " - Secondary Button");

			_app.Tap(secondaryButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Primary) + " - Closed");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Secondary()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog1");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert inital state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var secondaryButton = _app.Marked("SecondaryButton");
			_app.WaitForElement(secondaryButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Secondary) + " - Secondary Button");

			_app.Tap(secondaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Secondary");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_PrimaryCommand()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog4");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");
			var dialogCommand = _app.Marked("commandResult");

			// Assert inital state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var primaryButton = _app.Marked("PrimaryButton");
			_app.WaitForElement(primaryButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Secondary) + " - Primary Button");

			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Primary");
			_app.WaitForDependencyPropertyValue(dialogCommand, "Text", "primaryCommand");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_Close()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog2");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert inital state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var closeButton = _app.Marked("CloseButton");
			_app.WaitForElement(closeButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Secondary) + " - Close Button");

			_app.Tap(closeButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "None");
		}

		[Test]
		[AutoRetry]
		public void Simple_ContentDialog_01_TypeInner()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ContentDialogTests.ContentDialog_Simple");

			var showDialog1 = _app.Marked("showDialog1");

			_app.WaitForElement(showDialog1);

			var dialogResult = _app.Marked("dialogResult");

			// Assert inital state 
			Assert.AreEqual("Undefined", dialogResult.GetDependencyPropertyValue("Text")?.ToString());

			_app.Tap(showDialog1);

			var dialogInnerButton = _app.Marked("dialogInnerButton");
			_app.WaitForElement(dialogInnerButton);

			_app.Screenshot(nameof(Simple_ContentDialog_01_Primary) + " - Secondary Button");

			_app.Tap(dialogInnerButton);

			var buttonClickResult = _app.Marked("buttonClickResult");
			_app.WaitForDependencyPropertyValue(buttonClickResult, "Text", "OnDialogInnerButtonClick");

			var dialogTb = _app.Marked("dialogTb");
			_app.Tap(dialogTb);
			_app.EnterText("This is some text");

			var dialogTextBinding = _app.Marked("dialogTextBinding");
			_app.WaitForDependencyPropertyValue(dialogTextBinding, "Text", "This is some text");

			_app.Screenshot(nameof(Simple_ContentDialog_01_TypeInner) + " - Secondary Button");

			var primaryButton = _app.Marked("SecondaryButton");
			_app.Tap(primaryButton);

			_app.WaitForDependencyPropertyValue(dialogResult, "Text", "Secondary");
		}
	}
}
