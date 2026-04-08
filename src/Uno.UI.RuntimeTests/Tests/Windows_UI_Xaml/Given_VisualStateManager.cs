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
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Private.Infrastructure;
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public Task SelectorItem_MultiSelectState_GV() => SelectorItem_MultiSelectState_Impl<GridView>();

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/339")]
	public async Task When_VisualState_In_UserControl_No_Trigger()
	{
		var SUT = new VisualStateUserControlWithoutTrigger
		{
			Mode = ButtonMode.Task
		};

		Assert.IsTrue(SUT.IsTaskState);

		await UITestHelper.Load(SUT);

		Assert.IsTrue(SUT.IsTaskState);
		Assert.IsTrue(SUT.IsTaskTextVisible);
		Assert.IsFalse(SUT.IsMessageTextVisible);
		Assert.IsTrue(SUT.LastGoToStateResult);

		SUT.Mode = ButtonMode.Message;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.IsMessageState);
		Assert.IsTrue(SUT.IsMessageTextVisible);
		Assert.IsFalse(SUT.IsTaskTextVisible);
		Assert.IsTrue(SUT.LastGoToStateResult);

		SUT = new VisualStateUserControlWithoutTrigger
		{
			Mode = ButtonMode.Message
		};

		Assert.IsTrue(SUT.IsMessageState);

		await UITestHelper.Load(SUT);

		SUT.Mode = ButtonMode.Message;
		Assert.IsTrue(SUT.IsMessageState);
		Assert.IsTrue(SUT.IsMessageTextVisible);
		Assert.IsFalse(SUT.IsTaskTextVisible);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/339")]
	public async Task When_VisualState_In_UserControl_With_Trigger_Min()
	{
		var SUT = new VisualStateUserControlWithTrigger
		{
			Mode = ButtonMode.Task
		};

		Assert.IsTrue(SUT.IsTaskState);

		await UITestHelper.Load(SUT);

		// By default, the trigger activates for window width >= 1 == always

		Assert.IsTrue(SUT.IsTriggerState);

		// We can override the trigger

		SUT.Mode = ButtonMode.Message;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.IsMessageState);
		Assert.IsTrue(SUT.IsMessageTextVisible);
		Assert.IsFalse(SUT.IsTriggerState);
		Assert.IsTrue(SUT.LastGoToStateResult);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/kahua-private/issues/339")]
	public async Task When_VisualState_In_UserControl_With_Trigger_Max()
	{
		var SUT = new VisualStateUserControlWithTrigger
		{
			Mode = ButtonMode.Task
		};

		Assert.IsTrue(SUT.IsTaskState);

		SUT.SetTriggerSize(100000); // Too large window size, so the trigger won't be active

		await UITestHelper.Load(SUT);

		Assert.IsTrue(SUT.IsTaskState);
		Assert.IsFalse(SUT.IsTriggerState);

		// We can still set another state

		SUT.Mode = ButtonMode.Message;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(SUT.IsMessageState);
		Assert.IsTrue(SUT.IsMessageTextVisible);
		Assert.IsFalse(SUT.IsTriggerState);
		Assert.IsTrue(SUT.LastGoToStateResult);
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

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/7033")]
	public async Task When_VisualState_Has_Storyboard_Then_PropertyChanges_Applied()
	{
		// Issue #7033: Visual state is changed but Storyboard animation is not applying.
		// The VisualStateChanging/Changed events fire, but the animated properties
		// don't actually change (Android: stays same, GTK: jumps immediately).

		// Build control tree programmatically since XamlReader has issues with Duration
		var border = new Border
		{
			Name = "TestBorder",
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Windows.UI.Colors.Black)
		};

		var colorAnimation = new Microsoft.UI.Xaml.Media.Animation.ColorAnimation
		{
			To = Windows.UI.Colors.Red,
			Duration = new Duration(TimeSpan.FromMilliseconds(50)),
		};
		Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetName(colorAnimation, "TestBorder");
		Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(colorAnimation, "(Border.Background).(SolidColorBrush.Color)");

		var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
		storyboard.Children.Add(colorAnimation);

		var redState = new VisualState { Name = "RedState" };
		redState.Storyboard = storyboard;

		var defaultState = new VisualState { Name = "DefaultState" };

		var group = new VisualStateGroup();
		group.States.Add(defaultState);
		group.States.Add(redState);

		var grid = new Grid();
		grid.Children.Add(border);
		VisualStateManager.GetVisualStateGroups(grid).Add(group);

		var host = new ContentControl { Content = grid };

		TestServices.WindowHelper.WindowContent = host;
		await TestServices.WindowHelper.WaitForLoaded(host);
		await TestServices.WindowHelper.WaitForIdle();

		var initialBrush = border.Background as SolidColorBrush;
		Assert.IsNotNull(initialBrush);
		Assert.AreEqual(Windows.UI.Colors.Black, initialBrush.Color, "Initial color should be Black");

		// Trigger visual state change via storyboard directly
		storyboard.Begin();

		// Wait for animation to complete
		await Task.Delay(200);
		await TestServices.WindowHelper.WaitForIdle();

		// The color should have changed to Red
		var finalBrush = border.Background as SolidColorBrush;
		Assert.IsNotNull(finalBrush, "Background brush should still be a SolidColorBrush");
		Assert.AreEqual(Windows.UI.Colors.Red, finalBrush.Color,
			"After Storyboard animation completes, the Border background color should be Red");
	}
}
