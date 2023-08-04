using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ToggleSwitch
{
#if HAS_UNO
	[TestMethod]
	public async Task When_Recycling()
	{
		using var _ = FeatureConfigurationHelper.UseTemplatePooling();

		var toggleSwitch = new ToggleSwitch() { IsOn = true };
		toggleSwitch.OnTemplateRecycled();

		Assert.IsFalse(toggleSwitch.IsOn);

		toggleSwitch.IsTemplateRecyclingEnabled = false;

		toggleSwitch.IsOn = true;
		toggleSwitch.OnTemplateRecycled();

		Assert.IsTrue(toggleSwitch.IsOn);
	}
#endif
}
