using System;
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
