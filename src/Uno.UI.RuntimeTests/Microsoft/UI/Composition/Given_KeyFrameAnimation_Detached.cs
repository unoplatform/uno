using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
public partial class Given_KeyFrameAnimation_Detached
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25054")]
	public async Task When_Ancestor_Removed_Then_Animation_Stops_Requesting_Frames()
	{
#if __SKIA__
		var (panel, child) = CreateTree();
		await UITestHelper.Load(panel);
		var compositor = ElementCompositionPreview.GetElementVisual(child).Compositor;

		try
		{
			StartInfiniteOpacityAnimation(child);
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(compositor.IsAnimating, "The animation should run while its visual is in the tree.");

			// The child keeps its parent: only the panel leaves the tree.
			TestServices.WindowHelper.WindowContent = null;

			await WaitForNotAnimating(compositor);
			Assert.IsFalse(compositor.IsAnimating, "A detached visual's animation must not keep the frame loop running.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25054")]
	public async Task When_Ancestor_Readded_Then_Animation_Resumes()
	{
#if __SKIA__
		var (panel, child) = CreateTree();
		await UITestHelper.Load(panel);
		var compositor = ElementCompositionPreview.GetElementVisual(child).Compositor;

		try
		{
			StartInfiniteOpacityAnimation(child);
			await TestServices.WindowHelper.WaitForIdle();

			TestServices.WindowHelper.WindowContent = null;
			await WaitForNotAnimating(compositor);

			await UITestHelper.Load(panel);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(compositor.IsAnimating, "Re-attaching the subtree should resume its animations.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
#else
		await Task.CompletedTask;
#endif
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25054")]
	public async Task When_Ancestor_Removed_Then_Detached_Visual_Is_Collectable()
	{
#if __SKIA__
		var (panel, child) = CreateTree();
		await UITestHelper.Load(panel);
		var compositor = ElementCompositionPreview.GetElementVisual(child).Compositor;
		var weakVisual = StartAndGetWeakVisual(child);

		TestServices.WindowHelper.WindowContent = null;
		panel = null;
		child = null;

		await WaitForNotAnimating(compositor);

		for (var i = 0; i < 5 && weakVisual.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await TestServices.WindowHelper.WaitForIdle();
		}

		Assert.IsFalse(weakVisual.IsAlive, "The compositor must not keep a detached animated visual alive.");
#else
		await Task.CompletedTask;
#endif
	}

	private static (StackPanel Panel, Border Child) CreateTree()
	{
		var child = new Border { Width = 50, Height = 50, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red) };
		var panel = new StackPanel { Width = 100, Height = 100 };
		panel.Children.Add(child);
		return (panel, child);
	}

	private static void StartInfiniteOpacityAnimation(Border child)
	{
		var visual = ElementCompositionPreview.GetElementVisual(child);
		var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(0f, 1f);
		animation.InsertKeyFrame(1f, 0.5f);
		animation.Duration = TimeSpan.FromMilliseconds(500);
		animation.IterationBehavior = AnimationIterationBehavior.Forever;
		visual.StartAnimation("Opacity", animation);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference StartAndGetWeakVisual(Border child)
	{
		StartInfiniteOpacityAnimation(child);
		return new WeakReference(ElementCompositionPreview.GetElementVisual(child));
	}

#if __SKIA__
	private static async Task WaitForNotAnimating(Compositor compositor)
	{
		// A few frames: the detach is observed on the next rendered frame.
		for (var i = 0; i < 10 && compositor.IsAnimating; i++)
		{
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(20);
		}
	}
#endif
}
