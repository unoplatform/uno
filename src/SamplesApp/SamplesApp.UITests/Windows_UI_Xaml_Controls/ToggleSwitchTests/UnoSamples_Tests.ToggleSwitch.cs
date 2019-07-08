using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ToggleSwitchTests
{
	[TestFixture]
	public partial class ToggleSwitch_Tests : SampleControlUITestBase
	{
		[Test]
		[Ignore("Not available yet")]
		public void ToggleSwitch_TemplateReuseTest()
		{
			Run("Uno.UI.Samples.Content.UITests.ToggleSwitchControl.ToggleSwitch_TemplateReuse");

			var toggleSwitchGroup = _app.Marked("toggleSwitchGroup");

			_app.WaitForElement(toggleSwitchGroup);

			var separatedToggleSwitch = _app.Marked("separatedToggleSwitch");
			var unloadButton = _app.Marked("unload");
			var reloadButton = _app.Marked("reload");

			// Assert inital state 
			Assert.AreEqual("False", toggleSwitchGroup.GetDependencyPropertyValue("IsOn")?.ToString());
			Assert.AreEqual("False", separatedToggleSwitch.GetDependencyPropertyValue("IsOn")?.ToString());

			// Toggle group and unload/reload to cause templateReuse on lone toggleSwitch
			toggleSwitchGroup.Tap();
			_app.Wait(2);

			unloadButton.Tap();
			_app.Wait(2);

			reloadButton.Tap();
			_app.Wait(2);

			//Assert final state
			Assert.AreEqual("True", toggleSwitchGroup.GetDependencyPropertyValue("IsOn")?.ToString());
			Assert.AreEqual("False", separatedToggleSwitch.GetDependencyPropertyValue("IsOn")?.ToString());
		}
	}
}
