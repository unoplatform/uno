using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_ColorAnimation
{
	[TestMethod]
	public async Task When_FromTo_AnimatesColor()
	{
		var brush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Microsoft.UI.Colors.Red,
			To = Microsoft.UI.Colors.Blue,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		var color = brush.Color;
		Assert.AreEqual(Microsoft.UI.Colors.Blue.R, color.R, 2, $"R should be Blue.R, was {color.R}");
		Assert.AreEqual(Microsoft.UI.Colors.Blue.G, color.G, 2, $"G should be Blue.G, was {color.G}");
		Assert.AreEqual(Microsoft.UI.Colors.Blue.B, color.B, 2, $"B should be Blue.B, was {color.B}");
	}

	[TestMethod]
	public async Task When_EasingFunction_Applied()
	{
		var brush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Color.FromArgb(255, 0, 0, 0),
			To = Color.FromArgb(255, 255, 255, 255),
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		// EaseIn starts slow, so at ~30% time the color channels should be less than 30% of 255
		await Task.Delay(150);
		var midColor = brush.Color;

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));
		var finalColor = brush.Color;

		// With QuadraticEase.EaseIn at 30% time: progress = 0.3^2 = 0.09 → 9% of 255 ≈ 23
		Assert.IsTrue(midColor.R < 100,
			$"With EaseIn, mid-point R should be noticeably less than linear 30% (77), was {midColor.R}");

		Assert.AreEqual(255, finalColor.R, 2, $"Final R should be 255, was {finalColor.R}");
		Assert.AreEqual(255, finalColor.G, 2, $"Final G should be 255, was {finalColor.G}");
		Assert.AreEqual(255, finalColor.B, 2, $"Final B should be 255, was {finalColor.B}");
	}

	[TestMethod]
	public async Task When_Default_Duration_Is_One_Second()
	{
		var brush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Microsoft.UI.Colors.Red,
			To = Microsoft.UI.Colors.Blue,
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
		sw.Stop();

		Assert.IsTrue(sw.ElapsedMilliseconds >= 800,
			$"Default 1s animation should take at least 800ms, took {sw.ElapsedMilliseconds}ms");
	}

	[TestMethod]
	public async Task When_AutoReverse_ReturnsToFrom()
	{
		var brush = new SolidColorBrush(Microsoft.UI.Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Microsoft.UI.Colors.Red,
			To = Microsoft.UI.Colors.Blue,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			AutoReverse = true,
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		// After AutoReverse with HoldEnd, value returns to From (Red)
		var color = brush.Color;
		Assert.AreEqual(Microsoft.UI.Colors.Red.R, color.R, 2,
			$"After AutoReverse, R should return to Red.R ({Microsoft.UI.Colors.Red.R}), was {color.R}");
		Assert.AreEqual(Microsoft.UI.Colors.Red.B, color.B, 2,
			$"After AutoReverse, B should return to Red.B ({Microsoft.UI.Colors.Red.B}), was {color.B}");
	}

	[TestMethod]
	public async Task When_FillBehavior_Stop_ClearsValue()
	{
		var originalColor = Microsoft.UI.Colors.Green;
		var brush = new SolidColorBrush(originalColor);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			To = Microsoft.UI.Colors.Yellow,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			FillBehavior = FillBehavior.Stop,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
		await WindowHelper.WaitForIdle();

		// After FillBehavior.Stop, value should revert to local value
		Assert.AreEqual(originalColor.R, brush.Color.R, 2,
			$"After FillBehavior.Stop, should return to Green, was R={brush.Color.R}");
	}

	[TestMethod]
	public async Task When_Stop_While_Filling_ClearsValue()
	{
		var originalColor = Color.FromArgb(255, 50, 100, 150);
		var brush = new SolidColorBrush(originalColor);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			To = Microsoft.UI.Colors.White,
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		var storyboard = animation.ToStoryboard();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

		Assert.AreEqual(255, brush.Color.R, 2, $"Fill R should be 255, was {brush.Color.R}");

		storyboard.Stop();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(originalColor.R, brush.Color.R, 2,
			$"After Stop, R should return to {originalColor.R}, was {brush.Color.R}");
		Assert.AreEqual(originalColor.G, brush.Color.G, 2,
			$"After Stop, G should return to {originalColor.G}, was {brush.Color.G}");
	}
}
