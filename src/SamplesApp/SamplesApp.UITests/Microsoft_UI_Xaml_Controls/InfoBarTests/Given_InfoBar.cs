// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Common;

//#if USING_TAEF
//using WEX.TestExecution;
//using WEX.TestExecution.Markup;
//using WEX.Logging.Interop;
//#else
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
//#endif

using NUnit.Framework;
using SamplesApp.UITests;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace Windows.UI.Xaml.Tests.MUXControls.InteractionTests
{
	[Ignore("Tests are not stabilized yet")]
	public partial class Given_InfoBar : SampleControlUITestBase
	{
		// Tests which are not yet available
		// LayoutTest, CloseTest

		[SetUp]
		public void TestSetup()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.InfoBarTests.InfoBarPage");
		}

		[Test]
		[AutoRetry]
		public void IsClosableTest()
		{
			//using (var setup = new TestSetupHelper("InfoBar Tests"))
			{
				var infoBar = _app.Marked("TestInfoBar");

				var closeButton = FindCloseButton();
				Verify.IsTrue(closeButton.HasResults(), "Close button should be visible by default");

				Uncheck("IsClosableCheckBox");
				//ElementCache.Clear();
				closeButton = FindCloseButton();
				Verify.IsFalse(closeButton.HasResults(), "Close button should not be visible when IsClosable=false");

				Check("IsClosableCheckBox");
				//ElementCache.Clear();
				closeButton = FindCloseButton();
				Verify.IsTrue(closeButton.HasResults(), "Close button should be visible when IsClosable=true");
			}
		}

		[Test]
		[AutoRetry]
		public void AccessibilityViewTest()
		{
			//using (var setup = new TestSetupHelper("InfoBar Tests"))
			{
				var infoBar = _app.Marked("TestInfoBar");
				Verify.IsNotNull(infoBar, "TestInfoBar should be visible by default");

				Log.Comment("Close InfoBar and make sure it can't be found.");
				Uncheck("IsOpenCheckBox");
				//ElementCache.Clear();

				infoBar = _app.Marked("TestInfoBar");
				Verify.IsFalse(infoBar.HasResults(), "TestInfoBar should not be in the accessible tree");

				infoBar = _app.Marked("DefaultInfoBar");
				Verify.IsFalse(infoBar.HasResults(), "By default, Infobar should not be visible");
			}
		}

		private QueryEx FindCloseButton()
		{
			return _app.Marked("Close");
		}

		private void Check(string checkboxName)
		{
			var checkBox = _app.Marked(checkboxName);
			checkBox.FastTap();
			Log.Comment("Checked " + checkboxName + " checkbox");
			//Wait.ForIdle();
		}

		private void Uncheck(string checkboxName)
		{
			var checkBox = _app.Marked(checkboxName);
			checkBox.FastTap();
			Log.Comment("Unchecked " + checkboxName + " checkbox");
			//Wait.ForIdle();
		}
	}
}
