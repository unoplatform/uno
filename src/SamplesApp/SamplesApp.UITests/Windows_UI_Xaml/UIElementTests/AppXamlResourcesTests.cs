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
	public partial class AppXamlResourcesTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Bound_To_GlobalThemedResources()
		{
			Run("UITests.Shared.Windows_UI_Xaml.ThemeResources.AppXamlDefinedResources");

			_app.WaitForElement("Border01");

			Assert.AreEqual("Should be Yellow: [#FFFFFF00]", _app.GetText("result01"));
			Assert.AreEqual("Should be Purple: [#FF800080]", _app.GetText("result02"));
		}
	}
}
