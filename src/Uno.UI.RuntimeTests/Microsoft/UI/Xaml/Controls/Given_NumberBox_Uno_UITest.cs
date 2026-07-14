using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UITest.Helpers.Queries;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

// Migrated from SamplesApp.UITests Microsoft_UI_Xaml_Controls.NumberBoxTests.Given_NumberBox.Uno.
[TestClass]
[RunsOnUIThread]
public class Given_NumberBox_Uno_UITest : SampleControlUITestBase
{
	[TestMethod]
	public async Task When_Header_Is_Custom_Content()
	{
		var headerContent = new TextBlock { Text = "This is a NumberBox Header" };
		var numberBox = new NumberBox
		{
			SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
			Header = headerContent
		};

		try
		{
			await UITestHelper.Load(numberBox);

			Assert.AreSame(headerContent, numberBox.Header);
			Assert.AreEqual("This is a NumberBox Header", headerContent.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWasm)] // Screenshots need HAS_RENDER_TARGET_BITMAP, unavailable on native WASM (DOM).
	public async Task When_Description_Is_Custom_Content()
	{
		var descriptionBorder = new Border
		{
			Width = 300,
			Height = 300,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red)
		};
		var numberBox = new NumberBox { Description = descriptionBorder };

		try
		{
			await UITestHelper.Load(numberBox);

			var screenshot = await UITestHelper.ScreenShot(numberBox);
			ImageAssert.HasColorAtChild(screenshot, descriptionBorder, descriptionBorder.ActualWidth / 2, descriptionBorder.ActualHeight / 2, Microsoft.UI.Colors.Red, tolerance: 5);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_Text_Parsed_With_Custom_DecimalFormatter()
	{
		try
		{
			await RunAsync("UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests.NumberBoxPage");

			var numBox = (NumberBox)App.Query(App.Marked("TestNumberBox")).Single().Element;
			Assert.IsTrue(double.IsNaN(numBox.Value));

			// Driven via automation peer / direct property rather than coordinate taps, since
			// these controls sit further down the sample's ScrollViewer and may be scrolled out
			// of the runtime-test host viewport (see Given_XBind_UITest for the same rationale).
			var minCheckBox = (CheckBox)App.Query(App.Marked("MinCheckBox")).Single().Element;
			var maxCheckBox = (CheckBox)App.Query(App.Marked("MaxCheckBox")).Single().Element;
			minCheckBox.IsChecked = true;
			maxCheckBox.IsChecked = true;
			await WindowHelper.WaitForIdle();

			var customFormatterButton = (Button)App.Query(App.Marked("CustomFormatterButton")).Single().Element;
			((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(customFormatterButton)).Invoke();
			await WindowHelper.WaitForIdle();

			// Setting Text directly is the runtime-test equivalent of the legacy UI test's
			// SetDependencyPropertyValue helper; it goes through the same Text DP changed
			// callback that validates/reformats via the custom formatter.
			numBox.Text = "۱٫۷";
			await WindowHelper.WaitForIdle();

			Assert.AreEqual("۱٫۷۰", numBox.Text);
			Assert.AreEqual(1.7, numBox.Value);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
