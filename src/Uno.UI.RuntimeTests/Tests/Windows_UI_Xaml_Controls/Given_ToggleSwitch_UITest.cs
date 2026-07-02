using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_ToggleSwitch_UITest
{
	// Migrated from SamplesApp.UITests ToggleSwitch_HeaderTest: the ToggleSwitch.Header content is
	// realized in the visual tree with the expected text.
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Header_Content_Is_Set()
	{
		var headerText = new TextBlock { Text = "Test ToggleSwitch Header" };
		var toggleSwitch = new ToggleSwitch { Header = headerText };

		try
		{
			await UITestHelper.Load(toggleSwitch);
			await WindowHelper.WaitForLoaded(headerText);

			Assert.IsTrue(headerText.IsLoaded);
			Assert.AreEqual("Test ToggleSwitch Header", headerText.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}
}
