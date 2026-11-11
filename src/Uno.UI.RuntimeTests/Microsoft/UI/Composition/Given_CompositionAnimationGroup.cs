#nullable enable

using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_CompositionAnimationGroup
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25791")]
	public void When_Add_And_Remove()
	{
		var compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();
		var group = compositor.CreateAnimationGroup();
		var opacity = CreateOpacityAnimation(compositor, 0.5f);
		var scale = CreateScaleAnimation(compositor, new Vector3(2, 2, 1));

		group.Add(opacity);
		group.Add(scale);

		Assert.AreEqual(2, group.Count);
		CollectionAssert.AreEquivalent(new CompositionAnimation[] { opacity, scale }, group.ToArray());

		group.Remove(opacity);
		Assert.AreEqual(1, group.Count);
		Assert.AreSame(scale, group.Single());

		group.RemoveAll();
		Assert.AreEqual(0, group.Count);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25791")]
	public async Task When_StartAnimationGroup_Then_Every_Member_Runs()
	{
		var (host, visual) = await LoadSpriteVisual();
		var compositor = visual.Compositor;

		var group = compositor.CreateAnimationGroup();
		group.Add(CreateOpacityAnimation(compositor, 0.5f));
		group.Add(CreateScaleAnimation(compositor, new Vector3(2, 2, 1)));

		try
		{
			await RunInBatch(compositor, () => visual.StartAnimationGroup(group));

			Assert.AreEqual(0.5f, visual.Opacity, 0.001f);
			Assert.AreEqual(new Vector3(2, 2, 1), visual.Scale);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Same_Group_Started_On_Two_Visuals()
	{
		var (host, visual1) = await LoadSpriteVisual();
		var compositor = visual1.Compositor;
		var visual2 = compositor.CreateSpriteVisual();
		visual2.Size = new Vector2(10, 10);
		((ContainerVisual)visual1.Parent!).Children.InsertAtTop(visual2);

		var group = compositor.CreateAnimationGroup();
		group.Add(CreateOpacityAnimation(compositor, 0.25f));

		try
		{
			await RunInBatch(compositor, () =>
			{
				visual1.StartAnimationGroup(group);
				visual2.StartAnimationGroup(group);
			});

			Assert.AreEqual(0.25f, visual1.Opacity, 0.001f);
			Assert.AreEqual(0.25f, visual2.Opacity, 0.001f);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_StopAnimationGroup_Then_Members_Stop()
	{
		var (host, visual) = await LoadSpriteVisual();
		var compositor = visual.Compositor;

		var opacity = CreateOpacityAnimation(compositor, 0.5f);
		opacity.Duration = TimeSpan.FromSeconds(10);
		var group = compositor.CreateAnimationGroup();
		group.Add(opacity);

		try
		{
			// The batch completes when the group's animations stop, which only StopAnimationGroup does before 10s.
			await RunInBatch(compositor, () => visual.StartAnimationGroup(group), afterEnd: () => visual.StopAnimationGroup(group));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Member_Has_No_Target_Then_Start_Throws()
	{
		var compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();
		var visual = compositor.CreateSpriteVisual();
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(1, 0.5f);
		animation.Duration = TimeSpan.FromMilliseconds(100);

		var group = compositor.CreateAnimationGroup();
		group.Add(animation);

		Assert.ThrowsExactly<ArgumentException>(() => visual.StartAnimationGroup(group));
	}

	internal static ScalarKeyFrameAnimation CreateOpacityAnimation(Compositor compositor, float to)
	{
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(1, to);
		animation.Duration = TimeSpan.FromMilliseconds(100);
		animation.Target = nameof(Visual.Opacity);
		return animation;
	}

	internal static Vector3KeyFrameAnimation CreateScaleAnimation(Compositor compositor, Vector3 to)
	{
		var animation = compositor.CreateVector3KeyFrameAnimation();
		animation.InsertKeyFrame(1, to);
		animation.Duration = TimeSpan.FromMilliseconds(100);
		animation.Target = nameof(Visual.Scale);
		return animation;
	}

	internal static async Task<(Border Host, SpriteVisual Visual)> LoadSpriteVisual()
	{
		var host = new Border { Width = 100, Height = 100 };
		await UITestHelper.Load(host);

		var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
		var visual = compositor.CreateSpriteVisual();
		visual.Size = new Vector2(10, 10);
		visual.Brush = compositor.CreateColorBrush(Microsoft.UI.Colors.Red);
		ElementCompositionPreview.SetElementChildVisual(host, visual);

		return (host, visual);
	}

	internal static async Task RunInBatch(Compositor compositor, Action action, Action? afterEnd = null)
	{
		var completed = new TaskCompletionSource();
		var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
		batch.Completed += (_, _) => completed.TrySetResult();

		action();
		batch.End();
		afterEnd?.Invoke();

		await TestServices.WindowHelper.WaitFor(() => completed.Task.IsCompleted, timeoutMS: 5000, message: "The animation batch never completed.");
	}
}
