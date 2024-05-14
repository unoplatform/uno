using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
		var border = (Border)root.FindName("SUT_BackgroundBorder");
		var redButton = (Button)root.FindName("RedButton");
		var greenButton = (Button)root.FindName("GreenButton");
		Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);

		((ButtonAutomationPeer)redButton.GetAutomationPeer()).Invoke();
		await Task.Delay(1000);
		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)border.Background).Color);

		((ButtonAutomationPeer)greenButton.GetAutomationPeer()).Invoke();
		await Task.Delay(1000);
		Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);

		((ButtonAutomationPeer)redButton.GetAutomationPeer()).Invoke();
		await Task.Delay(1000);
		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)border.Background).Color);
	}
}
