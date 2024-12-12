using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.Extensions;
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

	[TestMethod]
	public async Task SelectorItem_SelectedState()
	{
		var items = Enumerable.Range(0, 3).ToArray();
		var setup = new GridView
		{
			ItemsSource = items,
			SelectedItem = items.Last(),
		};
		await UITestHelper.Load(setup);

		var container2 = setup.ContainerFromIndex(2) as GridViewItem ?? throw new Exception("Failed to retrieve container at index 2");

		// check if the visual-state is set
		var states = VisualStateHelper.GetCurrentVisualStateName(container2).ToArray();
		Assert.IsTrue(states.Contains("Selected"), $"container2 is not in 'Selected' state: states={states.JoinBy(",")}");
	}

	[TestMethod]
	public Task SelectorItem_MultiSelectState_GV() => SelectorItem_MultiSelectState_Impl<GridView>();

	[TestMethod]
	public Task SelectorItem_MultiSelectState_LV() => SelectorItem_MultiSelectState_Impl<ListView>();

	public async Task SelectorItem_MultiSelectState_Impl<T>() where T : ListViewBase, new()
	{
		var items = Enumerable.Range(0, 3).ToArray();
		var setup = new T
		{
			ItemsSource = items,
			SelectionMode = ListViewSelectionMode.Multiple,
		};
		await UITestHelper.Load(setup);

		var container2 = setup.ContainerFromIndex(2) as SelectorItem ?? throw new Exception("Failed to retrieve container at index 2");

		// check if the visual-state is set
		var states = VisualStateHelper.GetCurrentVisualStateName(container2).ToArray();
		Assert.IsTrue(states.Contains("MultiSelectEnabled"), $"container2 is not in 'MultiSelectEnabled' state: states={states.JoinBy(",")}");
	}
}
