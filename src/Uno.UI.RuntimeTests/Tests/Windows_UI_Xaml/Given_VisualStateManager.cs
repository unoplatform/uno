using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22055")]
	public async Task When_StateTrigger_Changes_State_Events_Fire_In_Order()
	{
		var grid = new Grid { Width = 100, Height = 100 };

		var narrowTrigger = new AdaptiveTrigger { MinWindowWidth = 0 };
		var wideTrigger = new AdaptiveTrigger { MinWindowWidth = 100000 };

		var narrowState = new VisualState { Name = "NarrowState" };
		narrowState.StateTriggers.Add(narrowTrigger);

		var wideState = new VisualState { Name = "WideState" };
		wideState.StateTriggers.Add(wideTrigger);

		var group = new VisualStateGroup();
		group.States.Add(narrowState);
		group.States.Add(wideState);

		var events = new List<string>();
		group.CurrentStateChanging += (s, e) => events.Add($"CHANGING:{e.OldState?.Name}->{e.NewState?.Name}");
		group.CurrentStateChanged += (s, e) => events.Add($"CHANGED:{e.OldState?.Name}->{e.NewState?.Name}");

		VisualStateManager.SetVisualStateGroups(grid, new List<VisualStateGroup> { group });

		await UITestHelper.Load(grid);
		await TestServices.WindowHelper.WaitForIdle();

		// NarrowState should be active initially (MinWindowWidth=0 always matches)
		Assert.AreEqual("NarrowState", group.CurrentState?.Name, "Initial state should be NarrowState");

		// Clear events from initial state setup
		events.Clear();

		// Now trigger WideState by lowering its threshold so it wins
		wideTrigger.MinWindowWidth = 1;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("WideState", group.CurrentState?.Name, "State should have changed to WideState");

		// Verify both events fired
		Assert.IsTrue(events.Count >= 2, $"Expected at least 2 events, got {events.Count}: {string.Join(", ", events)}");

		// Verify CHANGING fires before CHANGED
		var changingIndex = events.FindIndex(e => e.StartsWith("CHANGING:"));
		var changedIndex = events.FindIndex(e => e.StartsWith("CHANGED:"));
		Assert.IsTrue(changingIndex >= 0, "CurrentStateChanging should have fired");
		Assert.IsTrue(changedIndex >= 0, "CurrentStateChanged should have fired");
		Assert.IsTrue(changingIndex < changedIndex, "CurrentStateChanging should fire before CurrentStateChanged");

		// Verify the events reference correct states
		Assert.AreEqual("CHANGING:NarrowState->WideState", events[changingIndex]);
		Assert.AreEqual("CHANGED:NarrowState->WideState", events[changedIndex]);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22055")]
	public async Task When_StateTrigger_Re_Evaluation_Runs_VisualTransition_And_Applies_Setters()
	{
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue),
			Opacity = 1.0,
		};

		var narrowTrigger = new AdaptiveTrigger { MinWindowWidth = 0 };
		var wideTrigger = new AdaptiveTrigger { MinWindowWidth = 100000 };

		var narrowState = new VisualState { Name = "NarrowState" };
		narrowState.StateTriggers.Add(narrowTrigger);
		narrowState.Setters.Add(new Setter { Target = new TargetPropertyPath(border, new PropertyPath("Opacity")), Value = 1.0 });

		var wideState = new VisualState { Name = "WideState" };
		wideState.StateTriggers.Add(wideTrigger);
		wideState.Setters.Add(new Setter { Target = new TargetPropertyPath(border, new PropertyPath("Opacity")), Value = 1.0 });

		var transitionAnim = new DoubleAnimation
		{
			To = 0.2,
			Duration = new Duration(TimeSpan.FromMilliseconds(400)),
			EnableDependentAnimation = true,
		};
		Storyboard.SetTarget(transitionAnim, border);
		Storyboard.SetTargetProperty(transitionAnim, "Opacity");
		var transitionStoryboard = new Storyboard();
		transitionStoryboard.Children.Add(transitionAnim);

		var transition = new VisualTransition
		{
			From = "NarrowState",
			To = "WideState",
			Storyboard = transitionStoryboard,
		};

		var transitionCompleted = false;
		transitionStoryboard.Completed += (s, e) => transitionCompleted = true;

		var grid = new Grid();
		grid.Children.Add(border);
		var group = new VisualStateGroup();
		group.States.Add(narrowState);
		group.States.Add(wideState);
		group.Transitions.Add(transition);
		VisualStateManager.SetVisualStateGroups(grid, new List<VisualStateGroup> { group });

		await UITestHelper.Load(grid);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("NarrowState", group.CurrentState?.Name, "Should start in NarrowState");
		Assert.IsFalse(transitionCompleted, "Transition should not run on initial evaluation");

		// Trigger the transition
		wideTrigger.MinWindowWidth = 1;

		// During the transition (well before the 400ms duration completes), the animated Opacity
		// should be heading toward 0.2 — i.e. NOT equal to the setter's final value of 1.0.
		await TestServices.WindowHelper.WaitFor(() => border.Opacity < 0.95, timeoutMS: 1000);
		Assert.IsTrue(
			border.Opacity < 0.95,
			$"Opacity should be animating toward 0.2 during the transition, but was {border.Opacity}. " +
			"This indicates the VisualTransition storyboard is not running.");

		// After the transition completes, the setter should have been applied.
		await TestServices.WindowHelper.WaitFor(() => transitionCompleted, timeoutMS: 2000);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("WideState", group.CurrentState?.Name);
		Assert.IsTrue(transitionCompleted, "VisualTransition storyboard should have completed");
		Assert.AreEqual(1.0, border.Opacity, 0.01, "Setter's final Opacity value should be applied after the transition");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22055")]
	public async Task When_StateTrigger_Initial_Evaluation_Does_Not_Run_Transitions()
	{
		// Matches WinUI behavior: the initial trigger evaluation applies the state
		// without running VisualTransitions (useTransitions=false).
		var border = new Border
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue),
			Opacity = 1.0,
		};

		var narrowTrigger = new AdaptiveTrigger { MinWindowWidth = 0 };

		var narrowState = new VisualState { Name = "NarrowState" };
		narrowState.StateTriggers.Add(narrowTrigger);
		narrowState.Setters.Add(new Setter { Target = new TargetPropertyPath(border, new PropertyPath("Opacity")), Value = 1.0 });

		// Transition matching the initial state (From unset, To=NarrowState) with a long duration.
		// If it runs, Opacity would animate toward 0.1 for 2 seconds.
		var transitionAnim = new DoubleAnimation
		{
			To = 0.1,
			Duration = new Duration(TimeSpan.FromSeconds(2)),
			EnableDependentAnimation = true,
		};
		Storyboard.SetTarget(transitionAnim, border);
		Storyboard.SetTargetProperty(transitionAnim, "Opacity");
		var transitionStoryboard = new Storyboard();
		transitionStoryboard.Children.Add(transitionAnim);

		var transition = new VisualTransition
		{
			To = "NarrowState",
			Storyboard = transitionStoryboard,
		};

		var transitionCompleted = false;
		transitionStoryboard.Completed += (s, e) => transitionCompleted = true;

		var grid = new Grid();
		grid.Children.Add(border);
		var group = new VisualStateGroup();
		group.States.Add(narrowState);
		group.Transitions.Add(transition);
		VisualStateManager.SetVisualStateGroups(grid, new List<VisualStateGroup> { group });

		await UITestHelper.Load(grid);
		await TestServices.WindowHelper.WaitForIdle();

		// Shortly after load (well before the 2s transition would complete), Opacity should be
		// the setter value (1.0), NOT animating toward 0.1. If it animated, Opacity would be < 1.0.
		await Task.Delay(200);
		Assert.AreEqual("NarrowState", group.CurrentState?.Name);
		Assert.AreEqual(1.0, border.Opacity, 0.01,
			$"Opacity should be the setter's value (1.0) on initial evaluation, not animating. Actual: {border.Opacity}");
		Assert.IsFalse(transitionCompleted,
			"Transition storyboard should not have been started on initial evaluation.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/22055")]
	public async Task When_StateTrigger_Changes_State_Events_Fire_On_Initial_Load()
	{
		// Matches WinUI: CurrentStateChanging / CurrentStateChanged fire even on initial trigger evaluation.
		var grid = new Grid { Width = 100, Height = 100 };

		var narrowTrigger = new AdaptiveTrigger { MinWindowWidth = 0 };

		var narrowState = new VisualState { Name = "NarrowState" };
		narrowState.StateTriggers.Add(narrowTrigger);

		var group = new VisualStateGroup();
		group.States.Add(narrowState);

		var events = new List<string>();
		group.CurrentStateChanging += (s, e) => events.Add($"CHANGING:{e.OldState?.Name ?? "null"}->{e.NewState?.Name ?? "null"}");
		group.CurrentStateChanged += (s, e) => events.Add($"CHANGED:{e.OldState?.Name ?? "null"}->{e.NewState?.Name ?? "null"}");

		VisualStateManager.SetVisualStateGroups(grid, new List<VisualStateGroup> { group });

		await UITestHelper.Load(grid);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual("NarrowState", group.CurrentState?.Name);

		var changingIndex = events.FindIndex(e => e.StartsWith("CHANGING:null->NarrowState"));
		var changedIndex = events.FindIndex(e => e.StartsWith("CHANGED:null->NarrowState"));
		Assert.IsTrue(changingIndex >= 0, $"CurrentStateChanging should fire on initial evaluation. Events: {string.Join(", ", events)}");
		Assert.IsTrue(changedIndex >= 0, $"CurrentStateChanged should fire on initial evaluation. Events: {string.Join(", ", events)}");
		Assert.IsTrue(changingIndex < changedIndex, "CurrentStateChanging should fire before CurrentStateChanged");
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
