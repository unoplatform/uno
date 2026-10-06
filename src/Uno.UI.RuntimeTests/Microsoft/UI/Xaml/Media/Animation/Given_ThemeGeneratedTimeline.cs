#if HAS_UNO
using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_ThemeGeneratedTimeline
{
	[TestMethod]
	public async Task When_Begin_New_Then_Stop_Old_Hands_Off_Current_Value()
	{
		var (root, target) = CreateSetup();
		await UITestHelper.Load(root);

		var a = CreateStoryboard(target, isThemeGenerated: true, (TimeSpan.FromSeconds(2), 0.2));
		a.Begin();
		await Task.Delay(700);
		await WindowHelper.WaitForIdle();

		var midValue = target.Opacity;
		Assert.IsTrue(midValue is > 0.25 and < 0.95, $"A should be mid-run, got {midValue}");

		// No explicit From: B starts from the current animated value.
		var b = CreateStoryboard(target, isThemeGenerated: true, (TimeSpan.FromSeconds(10), 0.2));
		b.Begin();
		a.Stop();

		// Before the next animator tick, the slot still holds the hand-off value.
		Assert.AreEqual(midValue, target.Opacity, 0.05, "Opacity snapped right after the hand-off");

		var bitmap = new RenderTargetBitmap();
		await bitmap.RenderAsync(root);
		var pixels = await RawBitmap.From(bitmap, root);
		var center = pixels.GetPixel(pixels.Width / 2, pixels.Height / 2);

		// Black over white: R = 255 * (1 - opacity). A snap to 1 renders black.
		var renderedOpacity = 1 - center.R / 255.0;
		Assert.AreEqual(midValue, renderedOpacity, 0.15, $"Rendered opacity {renderedOpacity} (pixel {center})");

		b.Stop();
		Assert.AreEqual(1.0, target.Opacity, "Stopping the owner should restore the base value");
	}

	[TestMethod]
	public async Task When_KeyTime_Zero_Value_Is_Written_During_Begin()
	{
		var (root, target) = CreateSetup();
		await UITestHelper.Load(root);

		var sb = CreateStoryboard(target, isThemeGenerated: true, (TimeSpan.Zero, 0.4), (TimeSpan.FromSeconds(10), 0.2));
		sb.Begin();

		Assert.AreEqual(0.4, target.Opacity, 0.001);

		sb.Stop();
		Assert.AreEqual(1.0, target.Opacity);
	}

	[TestMethod]
	public async Task When_Non_Owner_Stops_Value_Is_Kept()
	{
		var (root, target) = CreateSetup();
		await UITestHelper.Load(root);

		var a = CreateObjectStoryboard(target, 0.5);
		var b = CreateObjectStoryboard(target, 0.3);

		a.Begin();
		Assert.AreEqual(0.5, target.Opacity, "Object keyframe at 0 should apply during Begin");

		b.Begin();
		Assert.AreEqual(0.3, target.Opacity);

		a.Stop();
		Assert.AreEqual(0.3, target.Opacity, "A no longer owns Opacity and must not clear it");

		b.Stop();
		Assert.AreEqual(1.0, target.Opacity);
	}

	[TestMethod]
	[DataRow(FillBehavior.HoldEnd, 0.2)]
	[DataRow(FillBehavior.Stop, 1.0)]
	public async Task When_Zero_Duration_Ends_With_Final_State(FillBehavior fillBehavior, double expected)
	{
		var (root, target) = CreateSetup();
		await UITestHelper.Load(root);

		var sb = CreateStoryboard(target, isThemeGenerated: true, (TimeSpan.Zero, 0.4), (TimeSpan.FromSeconds(1), 0.2));
		var animation = (DoubleAnimationUsingKeyFrames)sb.Children[0];
		animation.Duration = TimeSpan.Zero;
		animation.FillBehavior = fillBehavior;

		sb.Begin();

		Assert.AreEqual(expected, target.Opacity, 0.001);

		sb.Stop();
	}

	[TestMethod]
	public async Task When_Superseded_While_Running_Old_Stops_Writing()
	{
		var (root, target) = CreateSetup();
		await UITestHelper.Load(root);

		var a = CreateStoryboard(target, isThemeGenerated: true, (TimeSpan.FromSeconds(2), 0.2));
		a.Begin();
		await Task.Delay(200);

		var b = CreateObjectStoryboard(target, 0.5);
		b.Begin();
		Assert.AreEqual(0.5, target.Opacity);

		await Task.Delay(400);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(0.5, target.Opacity, "A still ticks but no longer owns Opacity");

		a.Stop();
		b.Stop();
		Assert.AreEqual(1.0, target.Opacity);
	}

	[TestMethod]
	public async Task When_Retargeted_Old_Registration_Is_Removed()
	{
		var (root, x) = CreateSetup();
		var y = new Border { Width = 10, Height = 10 };
		root.Children.Add(y);
		await UITestHelper.Load(root);

		var t = CreateObjectStoryboard(x, 0.5);
		var tAnimation = t.Children[0];
		t.Begin();
		Assert.AreEqual(0.5, x.Opacity);

		// HoldEnd keeps T registered on X; retargeting must drop that registration.
		Storyboard.SetTarget(tAnimation, y);
		t.Begin();
		Assert.AreEqual(0.5, y.Opacity);

		var u = CreateObjectStoryboard(x, 0.3);
		u.Begin();
		Assert.AreEqual(0.3, x.Opacity);

		t.Stop();
		Assert.AreEqual(1.0, y.Opacity, "T still owns Y and must clear it");

		u.Stop();
		Assert.AreEqual(1.0, x.Opacity);
	}

	[TestMethod]
	public async Task When_Not_Theme_Generated_Begin_Is_Dispatched()
	{
		var (root, target) = CreateSetup();
		await UITestHelper.Load(root);

		var sb = CreateStoryboard(target, isThemeGenerated: false, (TimeSpan.Zero, 0.9));
		var keyFrame = ((DoubleAnimationUsingKeyFrames)sb.Children[0]).KeyFrames[0];
		sb.Begin();

		// Mimics a TemplatedParent binding on DoubleKeyFrame.Value that resolves right after Begin.
		BindingOperations.SetBinding(keyFrame, DoubleKeyFrame.ValueProperty, new Binding { Source = 0.35 });

		await WindowHelper.WaitFor(() => Math.Abs(target.Opacity - 0.35) < 0.001, message: $"Opacity is {target.Opacity}");

		sb.Stop();
	}

	private static (Grid Root, Border Target) CreateSetup()
	{
		var target = new Border
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(Colors.Black),
		};
		var root = new Grid
		{
			Width = 50,
			Height = 50,
			Background = new SolidColorBrush(Colors.White),
			Children = { target },
		};

		return (root, target);
	}

	private static Storyboard CreateStoryboard(DependencyObject target, bool isThemeGenerated, params (TimeSpan KeyTime, double Value)[] keyFrames)
	{
		DoubleAnimationUsingKeyFrames animation = new() { IsThemeGenerated = isThemeGenerated };
		foreach (var (keyTime, value) in keyFrames)
		{
			animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = keyTime, Value = value });
		}

		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));

		return new Storyboard { Children = { animation } };
	}

	private static Storyboard CreateObjectStoryboard(DependencyObject target, double value)
	{
		ObjectAnimationUsingKeyFrames animation = new() { IsThemeGenerated = true };
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame { KeyTime = TimeSpan.Zero, Value = value });

		Storyboard.SetTarget(animation, target);
		Storyboard.SetTargetProperty(animation, nameof(UIElement.Opacity));

		return new Storyboard { Children = { animation } };
	}
}
#endif
