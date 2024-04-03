using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
public class BindingTests
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Binding_Setter_Value_In_Style()
	{
		var SUT = new BindingToSetterValuePage();
		await UITestHelper.Load(SUT);

		assertBorder(SUT.borderXBind, "Hello");
		assertBorder(SUT.borderBinding, null);

		void assertBorder(Border border, string expectedSetterValue)
		{
			var styleXBind = border.Style;
			var setter = (Setter)styleXBind.Setters.Single();
			Assert.AreEqual(AutomationProperties.AutomationIdProperty, setter.Property);
			Assert.AreEqual(expectedSetterValue, setter.Value);
		}
	}
}
