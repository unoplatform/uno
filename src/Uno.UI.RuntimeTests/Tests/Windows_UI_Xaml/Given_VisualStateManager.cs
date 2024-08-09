using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_VisualStateManager
{
	[TestMethod]
	public async Task When_Transition_Modifies_SubProperty_Of_Property_Set_By_Previous_State()
	{
		var root = new When_Transition_Modifies_SubProperty();
		await UITestHelper.Load(root);
		var control = (Control)root.FindName("control");
		var border = (Border)root.FindName("SUT_BackgroundBorder");
		Assert.AreEqual(Windows.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);

		VisualStateManager.GoToState(control, "Red", true);
		await Task.Delay(1000);
		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)border.Background).Color);

		VisualStateManager.GoToState(control, "Green", true);
		await Task.Delay(1000);
		Assert.AreEqual(Windows.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);

		VisualStateManager.GoToState(control, "Red", true);
		await Task.Delay(1000);
		Assert.AreEqual(Windows.UI.Colors.Red, ((SolidColorBrush)border.Background).Color);
	}
}
