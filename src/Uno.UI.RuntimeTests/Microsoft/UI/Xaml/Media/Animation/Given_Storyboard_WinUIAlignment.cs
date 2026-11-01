using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Private.Infrastructure;
using Colors = Microsoft.UI.Colors;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

/// <summary>
/// Storyboard behaviors of WinUI's timing engine (CTimeManager / CStoryboard / CAnimation), validated on native WinUI too.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_Storyboard_WinUIAlignment
{
	[TestMethod]
	public async Task When_Animation_Starts_While_Another_Runs_Then_Picks_Up_Current_Value()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var first = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 2000);
		first.Begin();
		await TestServices.WindowHelper.WaitFor(() => translate.X > 20, message: "the first animation should progress");

		var second = CreateAnimation(translate, nameof(TranslateTransform.X), to: -100, durationMs: 2000);
		var valueAtHandoff = translate.X;
		second.Begin();

		await TestServices.WindowHelper.WaitFor(() => translate.X < valueAtHandoff - 5, message: "the second animation should run");

		// The second animation started from where the first was (handoff), not from the base value 0.
		Assert.IsTrue(translate.X > 0, $"Expected the handoff to start near {valueAtHandoff}, got {translate.X}");

		first.Stop();
		second.Stop();
	}

	[TestMethod]
	public async Task When_Older_Storyboard_Stops_Then_Newer_Animation_Value_Is_Kept()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var first = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 0);
		first.Begin();
		await TestServices.WindowHelper.WaitFor(() => translate.X == 100, message: "the first animation should fill at 100");

		var second = CreateAnimation(translate, nameof(TranslateTransform.X), to: 50, durationMs: 0);
		second.Begin();
		await TestServices.WindowHelper.WaitFor(() => translate.X == 50, message: "the second animation should fill at 50");

		// The second animation took control of the property: stopping the first must not restore the base value.
		first.Stop();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(50, translate.X);

		second.Stop();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(0, translate.X);
	}

	[TestMethod]
	public async Task When_Two_Animations_Of_A_Storyboard_Target_The_Same_Property_Then_Begin_Throws()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = new Storyboard();
		storyboard.Children.Add(CreateDoubleAnimation(translate, nameof(TranslateTransform.X), to: 10, durationMs: 100));
		storyboard.Children.Add(CreateDoubleAnimation(translate, nameof(TranslateTransform.X), to: 20, durationMs: 100));

		AssertThrows(() => storyboard.Begin());
	}

	[TestMethod]
	public async Task When_TargetProperty_Changes_While_Running_Then_Throws()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 2000);
		storyboard.Begin();

		AssertThrows(() => Storyboard.SetTargetProperty(storyboard.Children[0], nameof(TranslateTransform.Y)));

		storyboard.Stop();

		// Once stopped, the timeline can be retargeted.
		Storyboard.SetTargetProperty(storyboard.Children[0], nameof(TranslateTransform.Y));
	}

	[TestMethod]
	public async Task When_Children_Change_While_Running_Then_Throws()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 2000);
		storyboard.Begin();

		AssertThrows(() => storyboard.Children.Add(CreateDoubleAnimation(translate, nameof(TranslateTransform.Y), to: 10, durationMs: 100)));

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Object_Key_Frames_Change_While_Running_Then_Add_Throws_And_Removal_Is_Ignored()
	{
		var border = await LoadBorder();

		var objectAnimation = new ObjectAnimationUsingKeyFrames();
		objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.FromMilliseconds(2000), Value = Visibility.Collapsed });
		Storyboard.SetTarget(objectAnimation, border);
		Storyboard.SetTargetProperty(objectAnimation, nameof(UIElement.Visibility));

		var storyboard = new Storyboard { Children = { objectAnimation } };
		storyboard.Begin();

		AssertThrows(() => objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.FromMilliseconds(500), Value = Visibility.Visible }));
		AssertThrows(() => objectAnimation.KeyFrames.Clear());

		// CTimingCollection::RemoveAt doesn't fail, it ignores the removal.
		objectAnimation.KeyFrames.RemoveAt(0);
		Assert.AreEqual(1, objectAnimation.KeyFrames.Count);

		storyboard.Stop();

		// Once the storyboard stops, the key frames can change again.
		objectAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.FromMilliseconds(500), Value = Visibility.Visible });
		objectAnimation.KeyFrames.RemoveAt(0);
		Assert.AreEqual(1, objectAnimation.KeyFrames.Count);
	}

	[TestMethod]
	public async Task When_Double_Key_Frames_Change_While_Running_Then_Add_Throws_And_Removal_Is_Ignored()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var doubleAnimation = new DoubleAnimationUsingKeyFrames();
		doubleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(2000), Value = 100 });
		Storyboard.SetTarget(doubleAnimation, translate);
		Storyboard.SetTargetProperty(doubleAnimation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard { Children = { doubleAnimation } };
		storyboard.Begin();

		AssertThrows(() => doubleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(500), Value = 50 }));

		doubleAnimation.KeyFrames.RemoveAt(0);
		Assert.AreEqual(1, doubleAnimation.KeyFrames.Count);

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Timeline_Is_Already_In_Another_Storyboard_Then_Add_Throws()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var animation = CreateDoubleAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 100);
		var first = new Storyboard { Children = { animation } };
		var second = new Storyboard();

		AssertThrows(() => second.Children.Add(animation));

		// Once removed from its storyboard, it can be added to another one.
		first.Children.Remove(animation);
		second.Children.Add(animation);
		Assert.AreEqual(1, second.Children.Count);
	}

	[TestMethod]
	public async Task When_Child_Is_Removed_While_Running_Then_Removal_Is_Ignored()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 2000);
		storyboard.Begin();

		storyboard.Children.RemoveAt(0);
		Assert.AreEqual(1, storyboard.Children.Count);

		storyboard.Stop();

		storyboard.Children.RemoveAt(0);
		Assert.AreEqual(0, storyboard.Children.Count);
	}

	[TestMethod]
	public async Task When_Nested_Storyboard_Is_Controlled_Directly_Then_Throws()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var nested = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 2000);
		var root = new Storyboard { Children = { nested } };
		root.Begin();

		AssertThrows(() => nested.Begin());
		AssertThrows(() => nested.Pause());
		AssertThrows(() => nested.Seek(TimeSpan.FromMilliseconds(10)));

		root.Stop();
	}

	[TestMethod]
	public async Task When_Dependent_Animation_Not_Enabled_Then_Value_Is_Unchanged()
	{
		var border = await LoadBorder();

		var storyboard = CreateAnimation(border, nameof(FrameworkElement.Width), to: 80, durationMs: 200);
		var completed = false;
		storyboard.Completed += (_, _) => completed = true;
		storyboard.Begin();

		await Task.Delay(500);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(50, border.Width, "A dependent animation without EnableDependentAnimation must not run");
		Assert.IsTrue(completed, "The storyboard still completes");

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Dependent_Animation_Enabled_Then_Value_Is_Animated()
	{
		var border = await LoadBorder();

		var storyboard = CreateAnimation(border, nameof(FrameworkElement.Width), to: 80, durationMs: 200, enableDependentAnimation: true);
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => border.Width == 80, message: "the enabled dependent animation should reach 80");

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Dependent_Animations_Are_Disallowed_Then_Enabled_Animation_Does_Not_Run()
	{
		var border = await LoadBorder();

		var storyboard = CreateAnimation(border, nameof(FrameworkElement.Width), to: 80, durationMs: 200, enableDependentAnimation: true);
		Timeline.AllowDependentAnimations = false;
		try
		{
			storyboard.Begin();

			await Task.Delay(500);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(50, border.Width);
		}
		finally
		{
			Timeline.AllowDependentAnimations = true;
			storyboard.Stop();
		}
	}

	[TestMethod]
	public async Task When_Dependent_Animation_Has_Zero_Duration_Then_Value_Is_Applied()
	{
		var border = await LoadBorder();

		// Zero-duration animations act as setters and are always allowed to run.
		var storyboard = CreateAnimation(border, nameof(FrameworkElement.Width), to: 80, durationMs: 0);
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => border.Width == 80, message: "the zero-duration animation should apply 80");

		storyboard.Stop();
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(50, border.Width);
	}

	[TestMethod]
	public async Task When_ObjectAnimation_Targets_Dependent_Property_Then_Value_Is_Applied()
	{
		var border = await LoadBorder();

		var animation = new ObjectAnimationUsingKeyFrames
		{
			KeyFrames =
			{
				new DiscreteObjectKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = new Thickness(7) },
			},
		};
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, nameof(FrameworkElement.Margin));
		var storyboard = new Storyboard { Children = { animation } };

		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => border.Margin == new Thickness(7), message: "object animations always run");

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Completed_Is_Raised_Then_Final_Value_Is_Applied()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 300);
		double? valueInCompleted = null;
		storyboard.Completed += (_, _) => valueInCompleted = translate.X;
		storyboard.Begin();

		await TestServices.WindowHelper.WaitFor(() => valueInCompleted is not null, message: "Completed should be raised");

		Assert.AreEqual(100, valueInCompleted);
		Assert.AreEqual(ClockState.Filling, storyboard.GetCurrentState());

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_SkipToFill_Before_Begin_Then_Nothing_Happens()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 1000);
		storyboard.SkipToFill();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0, translate.X);
		Assert.AreEqual(ClockState.Stopped, storyboard.GetCurrentState());
	}

	[TestMethod]
	public async Task When_SkipToFill_After_Begin_Then_Value_Is_Final_Synchronously()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 1000);
		storyboard.Begin();
		storyboard.SkipToFill();

		Assert.AreEqual(100, translate.X);
		Assert.AreEqual(ClockState.Filling, storyboard.GetCurrentState());

		storyboard.Stop();
	}

	[TestMethod]
	public void When_SpeedRatio_Is_Not_Positive_Then_Throws()
	{
		var animation = new DoubleAnimation();

		AssertThrows(() => animation.SpeedRatio = 0);
		AssertThrows(() => animation.SpeedRatio = -1);

		animation.SpeedRatio = 2;
		Assert.AreEqual(2, animation.SpeedRatio);
	}

	[TestMethod]
	public void When_Timeline_Is_Created_Then_Defaults_Match()
	{
		var animation = new DoubleAnimation();

		Assert.AreEqual(1.0, animation.SpeedRatio);
		Assert.IsFalse(animation.AutoReverse);
		Assert.IsNull(animation.BeginTime);
		Assert.AreEqual(Duration.Automatic, animation.Duration);
		Assert.AreEqual(FillBehavior.HoldEnd, animation.FillBehavior);
		Assert.AreEqual(RepeatBehaviorType.Count, animation.RepeatBehavior.Type);
		Assert.AreEqual(1.0, animation.RepeatBehavior.Count);
		Assert.IsFalse(animation.EnableDependentAnimation);
	}

	[TestMethod]
	public async Task When_Unresolvable_TargetName_Then_Begin_Throws()
	{
		var border = await LoadBorder();

		var animation = new DoubleAnimation { To = 0.5, Duration = TimeSpan.FromMilliseconds(100) };
		Storyboard.SetTargetName(animation, "DoesNotExist");
		Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));
		var storyboard = new Storyboard { Children = { animation } };
		border.Resources["sb"] = storyboard;

		AssertThrows(() => storyboard.Begin());
	}

	[TestMethod]
	public async Task When_Incompatible_Property_Type_Then_Begin_Throws()
	{
		var border = await LoadBorder();

		var animation = new ColorAnimation { To = Colors.Blue, Duration = TimeSpan.FromMilliseconds(100) };
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));
		var storyboard = new Storyboard { Children = { animation } };

		AssertThrows(() => storyboard.Begin());
	}

	[TestMethod]
	public async Task When_GetCurrentTime_After_SkipToFill_Then_Is_Duration()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 400);
		storyboard.Begin();
		storyboard.SkipToFill();

		Assert.AreEqual(400, storyboard.GetCurrentTime().TotalMilliseconds, 1);

		storyboard.Stop();
		Assert.AreEqual(TimeSpan.Zero, storyboard.GetCurrentTime());
	}

	[TestMethod]
	public async Task When_Storyboard_SpeedRatio_Then_Children_Complete_Sooner()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 400);
		storyboard.SpeedRatio = 4;
		storyboard.Begin();
		storyboard.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(50));

		// At 50ms of storyboard time, the child is at 200ms of its 400ms.
		Assert.AreEqual(50, translate.X, 1);

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Storyboard_AutoReverse_Then_Children_Return()
	{
		var (_, translate) = await LoadTranslatedBorder();

		var storyboard = CreateAnimation(translate, nameof(TranslateTransform.X), to: 100, durationMs: 400);
		storyboard.AutoReverse = true;
		storyboard.Begin();

		storyboard.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(600));
		Assert.AreEqual(50, translate.X, 1);

		storyboard.SkipToFill();
		Assert.AreEqual(0, translate.X, 0.01);

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_Transition_Has_GeneratedDuration_Then_Independent_Property_Is_Interpolated()
	{
		var (control, target) = await LoadGeneratedTransitionControl();

		VisualStateManager.GoToState(control, "First", false);
		await TestServices.WindowHelper.WaitFor(() => target.Opacity == 1, message: "the first state should apply its animation");

		VisualStateManager.GoToState(control, "Second", true);

		var sawIntermediateValue = false;
		var sw = Stopwatch.StartNew();
		while (sw.ElapsedMilliseconds < 3000 && target.Opacity != 0.2)
		{
			if (target.Opacity is > 0.25 and < 0.95)
			{
				sawIntermediateValue = true;
			}

			await Task.Delay(16);
		}

		Assert.AreEqual(0.2, target.Opacity, 0.001, "the generated animation should end on the value of the target state");
		Assert.IsTrue(sawIntermediateValue, "the generated animation should interpolate the independent Opacity over GeneratedDuration");
	}

	[TestMethod]
	public async Task When_Transition_Has_GeneratedDuration_Then_State_Changes_After_It()
	{
		var (control, _) = await LoadGeneratedTransitionControl();

		VisualStateManager.GoToState(control, "First", false);
		await TestServices.WindowHelper.WaitForIdle();

		var group = VisualStateManager.GetVisualStateGroups((FrameworkElement)VisualTreeHelper.GetChild(control, 0))[0];
		var changedAfter = default(TimeSpan?);
		var sw = Stopwatch.StartNew();
		group.CurrentStateChanged += (_, _) => changedAfter = sw.Elapsed;

		VisualStateManager.GoToState(control, "Second", true);
		await TestServices.WindowHelper.WaitFor(() => changedAfter is not null, timeoutMS: 3000, message: "the state change should complete");

		Assert.IsTrue(changedAfter >= TimeSpan.FromMilliseconds(400), $"The state should change once the 500ms generated transition completes, it changed after {changedAfter}");
	}

	private static async Task<(Control control, Border target)> LoadGeneratedTransitionControl()
	{
		var control = (ContentControl)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
			<ContentControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<ContentControl.Template>
					<ControlTemplate TargetType="ContentControl">
						<Grid>
							<VisualStateManager.VisualStateGroups>
								<VisualStateGroup x:Name="States">
									<VisualStateGroup.Transitions>
										<VisualTransition From="First" To="Second" GeneratedDuration="0:0:0.5" />
									</VisualStateGroup.Transitions>
									<VisualState x:Name="First">
										<Storyboard>
											<DoubleAnimation Storyboard.TargetName="Target" Storyboard.TargetProperty="Opacity" To="1" Duration="0" />
										</Storyboard>
									</VisualState>
									<VisualState x:Name="Second">
										<Storyboard>
											<DoubleAnimation Storyboard.TargetName="Target" Storyboard.TargetProperty="Opacity" To="0.2" Duration="0" />
										</Storyboard>
									</VisualState>
								</VisualStateGroup>
							</VisualStateManager.VisualStateGroups>
							<Border x:Name="Target" Width="50" Height="50" Background="Pink" />
						</Grid>
					</ControlTemplate>
				</ContentControl.Template>
			</ContentControl>
			""");

		TestServices.WindowHelper.WindowContent = control;
		await TestServices.WindowHelper.WaitForLoaded(control);

		var target = (Border)((FrameworkElement)VisualTreeHelper.GetChild(control, 0)).FindName("Target");
		return (control, target);
	}

	[TestMethod]
	public async Task When_Transition_Animates_A_Property_Set_By_The_Target_State_Then_Setter_Value_Is_Kept()
	{
		var control = (ContentControl)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
			<ContentControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
							xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				<ContentControl.Template>
					<ControlTemplate TargetType="ContentControl">
						<Grid>
							<VisualStateManager.VisualStateGroups>
								<VisualStateGroup x:Name="States">
									<VisualStateGroup.Transitions>
										<VisualTransition From="First" To="Second">
											<Storyboard>
												<DoubleAnimation Storyboard.TargetName="Target" Storyboard.TargetProperty="Opacity" To="0.8" Duration="0:0:0.15" />
											</Storyboard>
										</VisualTransition>
									</VisualStateGroup.Transitions>
									<VisualState x:Name="First">
										<Storyboard>
											<DoubleAnimation Storyboard.TargetName="Target" Storyboard.TargetProperty="Opacity" To="0.5" Duration="0" />
										</Storyboard>
									</VisualState>
									<VisualState x:Name="Second">
										<VisualState.Setters>
											<Setter Target="Target.Opacity" Value="0.2" />
										</VisualState.Setters>
									</VisualState>
								</VisualStateGroup>
							</VisualStateManager.VisualStateGroups>
							<Border x:Name="Target" Width="50" Height="50" Background="Pink" />
						</Grid>
					</ControlTemplate>
				</ContentControl.Template>
			</ContentControl>
			""");

		TestServices.WindowHelper.WindowContent = control;
		await TestServices.WindowHelper.WaitForLoaded(control);

		var target = (Border)((FrameworkElement)VisualTreeHelper.GetChild(control, 0)).FindName("Target");

		VisualStateManager.GoToState(control, "First", false);
		await TestServices.WindowHelper.WaitFor(() => target.Opacity == 0.5, message: "the first state should apply its animation");

		VisualStateManager.GoToState(control, "Second", true);

		// The transition is stopped once the setter is applied; stopping it must not clear the setter's value.
		await TestServices.WindowHelper.WaitFor(() => Math.Abs(target.Opacity - 0.2) < 0.001, message: "the setter of the target state should win after the transition");
		await Task.Delay(200);
		Assert.AreEqual(0.2, target.Opacity, 0.001);
	}

	private static async Task<Border> LoadBorder()
	{
		var border = new Border { Width = 50, Height = 50, Background = new SolidColorBrush(Colors.Pink) };
		TestServices.WindowHelper.WindowContent = border;
		await TestServices.WindowHelper.WaitForLoaded(border);
		return border;
	}

	private static async Task<(Border border, TranslateTransform translate)> LoadTranslatedBorder()
	{
		var translate = new TranslateTransform();
		var border = await LoadBorder();
		border.RenderTransform = translate;
		await TestServices.WindowHelper.WaitForIdle();
		return (border, translate);
	}

	private static DoubleAnimation CreateDoubleAnimation(DependencyObject target, string property, double to, int durationMs, bool enableDependentAnimation = false)
	{
		var animation = new DoubleAnimation
		{
			To = to,
			Duration = TimeSpan.FromMilliseconds(durationMs),
			EnableDependentAnimation = enableDependentAnimation,
		};
		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, property);
		return animation;
	}

	private static Storyboard CreateAnimation(DependencyObject target, string property, double to, int durationMs, bool enableDependentAnimation = false)
		=> new Storyboard { Children = { CreateDoubleAnimation(target, property, to, durationMs, enableDependentAnimation) } };

	private static void AssertThrows(Action action)
	{
		try
		{
			action();
		}
		catch (Exception)
		{
			// WinUI reports these as COMException (E_NER_INVALID_OPERATION / E_INVALIDARG), Uno as .NET exceptions.
			return;
		}

		Assert.Fail("An exception was expected.");
	}
}
