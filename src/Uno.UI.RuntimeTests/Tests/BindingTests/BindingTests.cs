using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class BindingTests
{
	[TestMethod]
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

	[TestMethod]
	public async Task When_BindingShouldBeAppliedOnPropertyChangedEvent()
	{
		var SUT = new BindingShouldBeAppliedOnPropertyChangedEvent();
		await UITestHelper.Load(SUT);

		var dc = (BindingShouldBeAppliedOnPropertyChangedEventVM)SUT.DataContext;
		var converter = (BindingShouldBeAppliedOnPropertyChangedEventConverter)SUT.Resources["MyConverter"];

		Assert.AreEqual(1, converter.ConvertCount);
		Assert.AreEqual("0", SUT.myTb.Text);

		dc.Increment();

		Assert.AreEqual(2, converter.ConvertCount);
		Assert.AreEqual("1", SUT.myTb.Text);
	}

	[TestMethod]
	[UnoWorkItem("https://github.com/unoplatform/uno/issues/16520")]
	public async Task When_XBind_In_Window()
	{
		var SUT = new XBindInWindow();
		SUT.Activate();
		try
		{
			Assert.AreEqual(0, SUT.ClickCount);
			SUT.MyButton.AutomationPeerClick();
			Assert.AreEqual(1, SUT.ClickCount);
		}
		finally
		{
			SUT.Close();
		}
	}
}
