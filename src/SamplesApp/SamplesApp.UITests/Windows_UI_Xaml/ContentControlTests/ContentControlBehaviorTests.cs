using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.ContentControlTests
{
	public class ContentControlBehaviorTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void LoadEmptyContentControl()
		{
			Run("SamplesApp.Windows_UI_Xaml_Controls.ContentControlNoTemplateNoContent");

			_app.WaitForElement("CContentControl");
			TakeScreenshot($"CContentControl");
			TabWaitAndThenScreenshot("bntContentClear");

			void TabWaitAndThenScreenshot(string buttonName)
			{
				_app.Marked(buttonName).Tap();
				TakeScreenshot($"ContentControlNoTemplateNoContent - {buttonName}");
			}

		}
	}
}
