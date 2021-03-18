using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml
{
	[TestFixture]
	public class AppXamlResourcesTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Bound_To_GlobalThemedResources()
		{
			Run("UITests.Shared.Windows_UI_Xaml.ThemeResources.AppXamlDefinedResources");

			_app.WaitForElement("Border01");

			Assert.AreEqual("Should be Yellow: [Color: 000000FF;000000FF;000000FF;00000000]", _app.GetText("result01"));
			Assert.AreEqual("Should be Purple: [Color: 000000FF;00000080;00000000;00000080]", _app.GetText("result02"));
		}
	}
}
