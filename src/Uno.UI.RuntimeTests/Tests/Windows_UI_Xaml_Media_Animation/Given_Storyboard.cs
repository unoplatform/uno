using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_Storyboard
{
	[TestMethod]
	public async Task When_Empty_Storyboard_Completes()
	{
		var storyboard = new Storyboard();
		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => completed);
	}

	[TestMethod]
	public async Task When_Empty_Storyboard_Completed_Attached_After()
	{
		var storyboard = new Storyboard();
		bool completed = false;
		storyboard.Begin();
		storyboard.Completed += (s, e) => completed = true;
		await TestServices.WindowHelper.WaitFor(() => completed);
	}

	[TestMethod]
	public async Task When_Empty_Storyboard_Stopped()
	{
		var storyboard = new Storyboard();
		storyboard.Completed += (s, e) => Assert.Fail("Storyboard should not trigger Completed");
		storyboard.Begin();
		storyboard.Stop();
		Assert.AreEqual(ClockState.Stopped, storyboard.GetCurrentState());
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public async Task When_Empty_Storyboard_Paused()
	{
		var storyboard = new Storyboard();
		var completed = false;
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();
		storyboard.Pause();
		Assert.AreEqual(ClockState.Active, storyboard.GetCurrentState());
		await TestServices.WindowHelper.WaitFor(() => completed);
	}

	#region Storyboard AutoReverse Tests

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_AutoReverse_WithSingleAnimation()
	{
		// Storyboard AutoReverse with a single DoubleAnimation
		var target = new TextBlock() { Text = "Test Storyboard AutoReverse" };
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForLoaded(target);

		var transform = new TranslateTransform();
		target.RenderTransform = transform;

		var animation = new DoubleAnimation()
		{
			From = 0,
			To = 100,
			Duration = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
			// No AutoReverse on animation
		};
		Storyboard.SetTarget(animation, transform);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard()
		{
			AutoReverse = true // Storyboard reverses
		};
		storyboard.Children.Add(animation);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();

		// Wait for completion (300ms forward + 300ms reverse = 600ms)
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

		// Final value should be back at start (~0)
		var finalValue = transform.X;
		Assert.IsTrue(Math.Abs(finalValue) < 10,
			$"Final value should be close to 0 after Storyboard AutoReverse, got {finalValue}");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_AutoReverse_WithMultipleAnimations()
	{
		var target = new TextBlock() { Text = "Test Multiple Animations" };
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForLoaded(target);

		var transform = new TranslateTransform();
		target.RenderTransform = transform;

		var animX = new DoubleAnimation()
		{
			From = 0,
			To = 100,
			Duration = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
		};
		Storyboard.SetTarget(animX, transform);
		Storyboard.SetTargetProperty(animX, nameof(TranslateTransform.X));

		var animY = new DoubleAnimation()
		{
			From = 0,
			To = 50,
			Duration = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
		};
		Storyboard.SetTarget(animY, transform);
		Storyboard.SetTargetProperty(animY, nameof(TranslateTransform.Y));

		var storyboard = new Storyboard()
		{
			AutoReverse = true
		};
		storyboard.Children.Add(animX);
		storyboard.Children.Add(animY);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();

		// Wait for completion (300ms forward + 300ms reverse = 600ms)
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

		// Both should be back at start
		Assert.IsTrue(Math.Abs(transform.X) < 10,
			$"X should be close to 0 after Storyboard AutoReverse, got {transform.X}");
		Assert.IsTrue(Math.Abs(transform.Y) < 10,
			$"Y should be close to 0 after Storyboard AutoReverse, got {transform.Y}");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_AutoReverse_WithMixedAnimationTypes()
	{
		// Test AutoReverse with different animation types in the same Storyboard
		var target = new Border()
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Red)
		};
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForLoaded(target);

		var transform = new TranslateTransform();
		target.RenderTransform = transform;

		// DoubleAnimation for position
		var positionAnim = new DoubleAnimation()
		{
			From = 0,
			To = 100,
			Duration = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
		};
		Storyboard.SetTarget(positionAnim, transform);
		Storyboard.SetTargetProperty(positionAnim, nameof(TranslateTransform.X));

		// ColorAnimation for background
		var colorAnim = new ColorAnimation()
		{
			From = Colors.Red,
			To = Colors.Blue,
			Duration = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
		};
		Storyboard.SetTarget(colorAnim, target.Background);
		Storyboard.SetTargetProperty(colorAnim, nameof(SolidColorBrush.Color));

		var storyboard = new Storyboard()
		{
			AutoReverse = true
		};
		storyboard.Children.Add(positionAnim);
		storyboard.Children.Add(colorAnim);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();

		// Wait for completion (300ms forward + 300ms reverse = 600ms)
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

		// Both should be back at start
		Assert.IsTrue(Math.Abs(transform.X) < 10,
			$"X should be close to 0 after Storyboard AutoReverse, got {transform.X}");

		var brush = (SolidColorBrush)target.Background;
		// Color should be close to Red (255,0,0) - allow some tolerance
		Assert.IsTrue(brush.Color.R > 200 && brush.Color.B < 55,
			$"Color should be close to Red after Storyboard AutoReverse, got R={brush.Color.R}, B={brush.Color.B}");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_AutoReverse_WithKeyFrameAnimation()
	{
		// Storyboard AutoReverse with keyframe child (animation does not have AutoReverse)
		var target = new TextBlock() { Text = "Test Storyboard AutoReverse with KeyFrames" };
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForLoaded(target);

		var transform = new TranslateTransform();
		target.RenderTransform = transform;

		var animation = new DoubleAnimationUsingKeyFrames()
		{
			Duration = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
			// No AutoReverse on animation
		};
		animation.KeyFrames.Add(new LinearDoubleKeyFrame()
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)),
			Value = 50
		});
		animation.KeyFrames.Add(new LinearDoubleKeyFrame()
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = 100
		});
		Storyboard.SetTarget(animation, transform);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard()
		{
			AutoReverse = true // Storyboard reverses
		};
		storyboard.Children.Add(animation);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();

		// Wait for completion (300ms forward + 300ms reverse = 600ms)
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

		// Final value should be back at start (~0)
		var finalValue = transform.X;
		Assert.IsTrue(Math.Abs(finalValue) < 10,
			$"Final value should be close to 0 after Storyboard AutoReverse with keyframes, got {finalValue}");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
	public async Task When_AutoReverse_OnBoth_Storyboard_And_Animation()
	{
		// Animation has AutoReverse, Storyboard also has AutoReverse
		// Tests complex nested behavior
		var target = new TextBlock() { Text = "Test Different Durations" };
		TestServices.WindowHelper.WindowContent = target;
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForLoaded(target);

		var transform = new TranslateTransform();
		target.RenderTransform = transform;

		var animation = new DoubleAnimation()
		{
			From = 0,
			To = 100,
			Duration = TimeSpan.FromMilliseconds(150),
			AutoReverse = true, // Animation AutoReverses
			FillBehavior = FillBehavior.HoldEnd,
		};
		Storyboard.SetTarget(animation, transform);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard()
		{
			AutoReverse = true // Storyboard also AutoReverses
		};
		storyboard.Children.Add(animation);

		bool completed = false;
		storyboard.Completed += (s, e) => completed = true;

		storyboard.Begin();

		// Animation: 150ms forward + 150ms reverse = 300ms
		// Storyboard AutoReverse: 300ms again = 600ms total
		await TestServices.WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

		// Final value should be 0
		var finalValue = transform.X;
		Assert.IsTrue(Math.Abs(finalValue) < 10,
			$"Final value should be close to 0 with nested AutoReverse, got {finalValue}");
	}

	#endregion
}
