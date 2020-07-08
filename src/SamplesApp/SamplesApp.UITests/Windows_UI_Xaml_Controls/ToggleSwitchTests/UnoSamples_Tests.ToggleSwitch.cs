using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ToggleSwitchTests
{
	[TestFixture]
	public partial class ToggleSwitch_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void ToggleSwitch_TemplateReuseTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl.ToggleSwitch_TemplateReuse");

			var toggleSwitchGroup = _app.Marked("toggleSwitchGroup");

			_app.WaitForElement(toggleSwitchGroup);

			var separatedToggleSwitch = _app.Marked("separatedToggleSwitch");
			var unloadButton = _app.Marked("unload");
			var reloadButton = _app.Marked("reload");

			// Assert initial state
			Assert.AreEqual("False", toggleSwitchGroup.GetDependencyPropertyValue("IsOn")?.ToString());
			Assert.AreEqual("True", separatedToggleSwitch.GetDependencyPropertyValue("IsOn")?.ToString());

			unloadButton.FastTap();
			reloadButton.FastTap();

			//Assert final state
			Assert.AreEqual("False", toggleSwitchGroup.GetDependencyPropertyValue("IsOn")?.ToString());
			Assert.AreEqual("True", separatedToggleSwitch.GetDependencyPropertyValue("IsOn")?.ToString());
		}

		[Test]
		[AutoRetry]
		public void ToggleSwitch_HeaderTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl.ToggleSwitch_Header");

			var toggleSwitchWithHeader = _app.Marked("toggleSwitchWithHeader");
			_app.WaitForElement(toggleSwitchWithHeader);

			var toggleSwitchHeaderContentTextBlock = _app.Marked("toggleSwitchHeaderContent");
			_app.WaitForElement(toggleSwitchHeaderContentTextBlock);

			Assert.AreEqual("Test ToggleSwitch Header", toggleSwitchHeaderContentTextBlock.GetDependencyPropertyValue("Text").ToString());
		}
	}
}
