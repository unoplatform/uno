using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ToggleButton
{
#if HAS_UNO
	[TestMethod]
	public async Task When_Recycling()
	{
		using var _ = FeatureConfigurationHelper.UseTemplatePooling();

		var toggleButton = new ToggleButton() { IsChecked = true };
		toggleButton.OnTemplateRecycled();

		Assert.IsFalse(toggleButton.IsChecked);

		toggleButton.IsTemplateRecyclingEnabled = false;

		toggleButton.IsChecked = true;
		toggleButton.OnTemplateRecycled();

		Assert.IsTrue(toggleButton.IsChecked);
	}
#endif
}
