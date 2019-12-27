using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Windows_UI_Xaml_Automation
{
	public class AutomationId_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TestSimple()
		{
			Run("UITests.Shared.Windows_UI.Xaml_Automation.AutomationProperties_AutomationId");

			Query result = q => q.Marked("result");
			_app.WaitForElement(result);

			for (int i = 1; i < 4; i++)
			{
				var itemName = $"Item{i:00}";
				_app.WaitForElement(itemName);
				_app.Tap(itemName);
				_app.WaitForDependencyPropertyValue(result, "Text", $"Item {i:00}");
			}
		}
	}
}
