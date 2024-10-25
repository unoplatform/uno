using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
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

#if __SKIA__ && HAS_UNO_WINUI
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
#endif

	[TestMethod]
	public async Task When_TargetNullValueThemeResource()
	{
		var SUT = new TargetNullValueThemeResource();
		await UITestHelper.Load(SUT);

		var myBtn = SUT.myBtn;
		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual(Windows.UI.Colors.Green, ((SolidColorBrush)myBtn.Foreground).Color);
		}

		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
	}

	[TestMethod]
	public async Task When_FallbackValueThemeResource_NoDataContext()
	{
		var SUT = new FallbackValueThemeResource();
		await UITestHelper.Load(SUT);

		var myBtn = SUT.myBtn;
		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);

		using (ThemeHelper.UseDarkTheme())
		{
#if WINAPPSDK
			Assert.AreEqual(Windows.UI.Colors.Green, ((SolidColorBrush)myBtn.Foreground).Color);
#else
			// WRONG behavior!
			Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
#endif
		}

		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
	}

	[TestMethod]
	public async Task When_FallbackValueThemeResource_WithDataContext()
	{
		var SUT = new FallbackValueThemeResource();
		await UITestHelper.Load(SUT);

		var myBtn = SUT.myBtn;
		myBtn.DataContext = "Hello";
		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);

		using (ThemeHelper.UseDarkTheme())
		{
			Assert.AreEqual(Windows.UI.Colors.Green, ((SolidColorBrush)myBtn.Foreground).Color);
		}

		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)myBtn.Foreground).Color);
	}
}
