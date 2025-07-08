using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using SamplesApp.UITests;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public partial class Given_VisualStateManager
{
	[TestMethod]
	public async Task When_Transition_Modifies_SubProperty_Of_Property_Set_By_Previous_State()
	{
		var root = new When_Transition_Modifies_SubProperty();
		await UITestHelper.Load(root);
		var control = (Control)root.FindName("control");
		var border = (Border)root.FindName("SUT_BackgroundBorder");
		Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);

		VisualStateManager.GoToState(control, "Red", true);
		await Task.Delay(1000);
		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)border.Background).Color);

		VisualStateManager.GoToState(control, "Green", true);
		await Task.Delay(1000);
		Assert.AreEqual(Microsoft.UI.Colors.Green, ((SolidColorBrush)border.Background).Color);

		VisualStateManager.GoToState(control, "Red", true);
		await Task.Delay(1000);
		Assert.AreEqual(Microsoft.UI.Colors.Red, ((SolidColorBrush)border.Background).Color);
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

#if HAS_UNO
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/19364")]
	public async Task When_StateTriggers_Evaluated_Before_First_Layout()
	{
		MyUserControl uc = new MyUserControl();
		VisualStateManager.SetVisualStateGroups(uc, new List<VisualStateGroup>
		{
			new VisualStateGroup()
			{
				States =
				{
					new VisualState
					{
						Name = "MyVisualState1",
					},
					new VisualState
					{
						Name = "MyVisualState2",
						StateTriggers =
						{
							new AdaptiveTrigger()
							{
								MinWindowWidth = 1
							}
						}
					}
				}
			}
		});

		var contentControl = new ContentControl
		{
			Content = "0",
			ContentTemplate = new DataTemplate(() =>
			{
				return uc;
			})
		};

		await UITestHelper.Load(contentControl, control => control.IsLoaded);
		Assert.AreEqual("MyVisualState2", uc.VisualStateOnFirstMeasure?.Name);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/20708")]
	public async Task When_Custom_StateTriggers_Initial_State()
	{
		var SUT = new When_Custom_StateTriggers_Initial_State();
		await UITestHelper.Load(SUT);
		Assert.AreEqual(50, ((Rectangle)SUT.FindName("rect")).Height);
	}

	private partial class MyUserControl : UserControl
	{
		private bool _firstMeasure = true;
		public VisualState VisualStateOnFirstMeasure { get; set; }

		protected override Size MeasureOverride(Size availableSize)
		{
			if (_firstMeasure)
			{
				_firstMeasure = false;
				VisualStateOnFirstMeasure = VisualStateManager.GetVisualStateGroups(this)[0].CurrentState;
			}
			return base.MeasureOverride(availableSize);
		}
	}
#endif
}
