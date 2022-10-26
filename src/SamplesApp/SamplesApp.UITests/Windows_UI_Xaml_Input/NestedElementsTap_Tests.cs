using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Input;

[TestFixture]
public partial class NestedElementsTap_Tests : SampleControlUITestBase
{
	[Test]
	[AutoRetry]
	public void Button_Inside_Border_Tap()
	{
		Run("UITests.Shared.Windows_UI_Input.PointersTests.Button_Inside_Border_Click_Event");
		var button = _app.Marked("MyButton");
		Assert.AreEqual(string.Empty, (string)button.GetDependencyPropertyValue("Tag"));
		button.FastTap();
		Assert.AreEqual("Hit MyButton.OnTapped\r\n", (string)button.GetDependencyPropertyValue("Tag"));
	}
}
