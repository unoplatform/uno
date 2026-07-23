using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.UITests;
using Uno.UITest.Helpers.Queries;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

[TestClass]
[RunsOnUIThread]
public class Given_XBind_UITest : SampleControlUITestBase
{
	[TestMethod]
	public async Task When_Functions()
	{
		try
		{
			await RunAsync("UITests.Shared.Windows_UI_Xaml.xBindTests.xBind_Functions");

			// Static property / static function bindings.
			await App.WaitForDependencyPropertyValueAsync(App.Marked("_StaticPropertyIntValue"), "Text", "42");
			Assert.AreEqual("42", App.Marked("_StaticPropertyIntValue").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("value 43", App.Marked("_StaticPropertyStringValue").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("VALUE 43", App.Marked("_StaticPropertyStringValueFunction").GetDependencyPropertyValue<string>("Text"));

			// Element-to-element function bindings. slider1/slider2 Value coerce up to their Minimum (20 and 22).
			Assert.AreEqual("440", App.Marked("_MemberFunctionMultiple_Bind_OneWay").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("42", App.Marked("_SaticFunctionMultiple_Bind_OneWay").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("slider1: 20, slider2:22", App.Marked("_SystemFunction_Bind_OneWay").GetDependencyPropertyValue<string>("Text"));

			// DataTemplate x:Bind functions (initial value).
			await App.WaitForDependencyPropertyValueAsync(App.Marked("_DataTemplate_MyProperty"), "Text", "Initial");
			Assert.AreEqual("Initial", App.Marked("_DataTemplate_MyProperty").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("INITIAL", App.Marked("_DataTemplate_MyProperty_Function").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("Formatted Initial", App.Marked("_DataTemplate_MyProperty_Formatted").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("INITIAL", App.Marked("_DataTemplate_MyProperty_Function_OneWay").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("Formatted Initial", App.Marked("_DataTemplate_MyProperty_Formatted_OneWay").GetDependencyPropertyValue<string>("Text"));

			// Invoke the "Update" button; it mutates the source, so the OneWay bindings should follow.
			// (Invoked via the automation peer rather than a coordinate tap so the assertion doesn't depend on the button being on-screen.)
			var button = (Button)App.Query(App.Marked("updateTemplateButton")).Single().Element;
			((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button)).Invoke();
			await WindowHelper.WaitForIdle();

			await App.WaitForDependencyPropertyValueAsync(App.Marked("_DataTemplate_MyProperty_Function_OneWay"), "Text", "NEW UPDATE");
			Assert.AreEqual("NEW UPDATE", App.Marked("_DataTemplate_MyProperty_Function_OneWay").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("Formatted new update", App.Marked("_DataTemplate_MyProperty_Formatted_OneWay").GetDependencyPropertyValue<string>("Text"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
