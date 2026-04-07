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

	// Repro tests for https://github.com/unoplatform/uno/issues/2042
	[TestClass]
	[RunsOnUIThread]
	public class Given_VisualStateManager_Issue2042
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/2042")]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_VisualState_Has_Both_Setter_And_Storyboard_Setter_Is_Applied()
		{
			// Issue: VisualState with both Setters and a Storyboard only executes the Storyboard on iOS/Android.
			// The Setter values are ignored on those platforms.
			// Expected: Both Setter and Storyboard should be applied when transitioning to the state.

			var vsm = (Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<UserControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				             Width="300" Height="300">
				    <Grid>
				        <VisualStateManager.VisualStateGroups>
				            <VisualStateGroup x:Name="TestGroup">
				                <VisualState x:Name="Normal" />
				                <VisualState x:Name="ActiveState">
				                    <VisualState.Setters>
				                        <Setter Target="TestBorder.Tag" Value="SetterApplied" />
				                    </VisualState.Setters>
				                    <Storyboard>
				                        <ObjectAnimationUsingKeyFrames
				                            Storyboard.TargetName="TestBorder"
				                            Storyboard.TargetProperty="Background">
				                            <DiscreteObjectKeyFrame KeyTime="0:0:0">
				                                <DiscreteObjectKeyFrame.Value>
				                                    <SolidColorBrush Color="Red" />
				                                </DiscreteObjectKeyFrame.Value>
				                            </DiscreteObjectKeyFrame>
				                        </ObjectAnimationUsingKeyFrames>
				                    </Storyboard>
				                </VisualState>
				            </VisualStateGroup>
				        </VisualStateManager.VisualStateGroups>
				        <Border x:Name="TestBorder" Width="100" Height="100">
				            <Border.Background>
				                <SolidColorBrush Color="Blue" />
				            </Border.Background>
				        </Border>
				    </Grid>
				</UserControl>
				""")) as UserControl;

			await UITestHelper.Load(vsm);
			await UITestHelper.WaitForIdle();

			var testBorder = vsm.FindName("TestBorder") as Border;
			Assert.IsNotNull(testBorder);

			// Transition to the active state (no transitions to avoid timing issues)
			VisualStateManager.GoToState(vsm, "ActiveState", false);
			await UITestHelper.WaitForIdle();

			// Check that the Setter was applied (Tag should be "SetterApplied")
			// On iOS/Android, only the Storyboard runs and the Setter is ignored.
			Assert.AreEqual("SetterApplied", testBorder.Tag?.ToString(),
				$"Expected Tag to be 'SetterApplied' from the VisualState Setter, but got '{testBorder.Tag}'. " +
				$"On iOS/Android, only the Storyboard is applied and Setters are ignored when both are present.");

			// Check that the Storyboard was also applied (Background should be Red)
			var bg = testBorder.Background as SolidColorBrush;
			Assert.IsNotNull(bg, "Expected Background to be a SolidColorBrush from the Storyboard.");
			Assert.AreEqual(Windows.UI.Colors.Red, bg.Color,
				$"Expected Background to be Red from the Storyboard animation, but got {bg.Color}.");
		}
	}
}
