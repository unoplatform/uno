#nullable enable

using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_ImplicitAnimationCollection
{
	[TestMethod]
	public void When_Animation_Has_No_Target_Then_Insert_Throws()
	{
		var compositor = CompositionTarget.GetCompositorForCurrentThread();
		var collection = compositor.CreateImplicitAnimationCollection();
		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.InsertExpressionKeyFrame(1, "this.FinalValue");

		Assert.ThrowsExactly<ArgumentException>(() => collection["Opacity"] = animation);
	}

	[TestMethod]
	public void When_Insert_And_Remove()
	{
		var compositor = CompositionTarget.GetCompositorForCurrentThread();
		var collection = compositor.CreateImplicitAnimationCollection();
		var group = compositor.CreateAnimationGroup();

		collection["Offset"] = CreateHoldThenJumpAnimation(compositor);
		collection["Opacity"] = group;

		Assert.AreEqual(2, collection.Count);
		Assert.IsTrue(collection.ContainsKey("Offset"));
		Assert.AreSame(group, collection["Opacity"]);

		Assert.IsTrue(collection.Remove("Offset"));
		Assert.IsFalse(collection.ContainsKey("Offset"));
	}

	[TestMethod]
	public async Task When_Offset_Assigned_Then_Implicit_Animation_Runs()
	{
		var (host, visual) = await Given_CompositionAnimationGroup.LoadSpriteVisual();
		var compositor = visual.Compositor;

		var implicitAnimations = compositor.CreateImplicitAnimationCollection();
		implicitAnimations["Offset"] = CreateHoldThenJumpAnimation(compositor);
		visual.ImplicitAnimations = implicitAnimations;

		try
		{
			var completed = new TaskCompletionSource();
			var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
			batch.Completed += (_, _) => completed.TrySetResult();
			visual.Offset = new Vector3(50, 0, 0);
			batch.End();

			// The animation holds this.StartingValue, so the visual is still drawn at its old position.
			var during = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(during, 5, 5, Colors.Red, tolerance: 5);
			ImageAssert.DoesNotHaveColorAt(during, 55, 5, Colors.Red, tolerance: 5);

			await TestServices.WindowHelper.WaitFor(() => completed.Task.IsCompleted, timeoutMS: 5000);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(new Vector3(50, 0, 0), visual.Offset);
			var after = await UITestHelper.ScreenShot(host);
			ImageAssert.DoesNotHaveColorAt(after, 5, 5, Colors.Red, tolerance: 5);
			ImageAssert.HasColorAt(after, 55, 5, Colors.Red, tolerance: 5);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Implicit_Group_Then_FinalValue_Of_Other_Property_Is_Current_Value()
	{
		var (host, visual) = await Given_CompositionAnimationGroup.LoadSpriteVisual();
		var compositor = visual.Compositor;
		visual.Opacity = 0.75f;

		// Triggered by Offset, but animates Opacity: this.FinalValue resolves to Opacity's own value.
		var opacity = compositor.CreateScalarKeyFrameAnimation();
		opacity.InsertExpressionKeyFrame(1, "this.FinalValue");
		opacity.Duration = TimeSpan.FromMilliseconds(100);
		opacity.Target = nameof(Visual.Opacity);

		var group = compositor.CreateAnimationGroup();
		group.Add(opacity);

		var implicitAnimations = compositor.CreateImplicitAnimationCollection();
		implicitAnimations["Offset"] = group;
		visual.ImplicitAnimations = implicitAnimations;

		try
		{
			await Given_CompositionAnimationGroup.RunInBatch(compositor, () => visual.Offset = new Vector3(20, 0, 0));

			Assert.AreEqual(new Vector3(20, 0, 0), visual.Offset);
			Assert.AreEqual(0.75f, visual.Opacity, 0.001f);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Layout_Moves_Element_Then_Offset_Implicit_Animation_Runs()
	{
		var spacer = new Border { Width = 20, Height = 0 };
		var target = new Border { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left, Background = new SolidColorBrush(Colors.Red) };
		var host = new StackPanel { Width = 100, Height = 100, Children = { spacer, target } };
		await UITestHelper.Load(host);

		var visual = ElementCompositionPreview.GetElementVisual(target);
		var implicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
		implicitAnimations["Offset"] = CreateHoldThenJumpAnimation(visual.Compositor);
		visual.ImplicitAnimations = implicitAnimations;

		try
		{
			spacer.Height = 50;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(50, target.ActualOffset.Y, "Layout moves the element right away.");

			// ... while its visual is still drawn at the old position.
			var during = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(during, 10, 10, Colors.Red, tolerance: 5);
			ImageAssert.DoesNotHaveColorAt(during, 10, 60, Colors.Red, tolerance: 5);

			var deadline = DateTime.UtcNow.AddSeconds(5);
			while (true)
			{
				await TestServices.WindowHelper.WaitForIdle();
				var after = await UITestHelper.ScreenShot(host);
				var pixel = after.GetPixel(10, 60);
				if (pixel.R > 250 && pixel.G < 5 && pixel.B < 5)
				{
					ImageAssert.DoesNotHaveColorAt(after, 10, 10, Colors.Red, tolerance: 5);
					break;
				}

				Assert.IsTrue(DateTime.UtcNow < deadline, "The element never reached its new layout position.");
				await Task.Delay(100);
			}
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	// Stays at the starting value for most of its second, then jumps to the assigned value: lets a test observe
	// that the animation runs without depending on frame timing.
	internal static Vector3KeyFrameAnimation CreateHoldThenJumpAnimation(Compositor compositor)
	{
		var animation = compositor.CreateVector3KeyFrameAnimation();
		animation.InsertExpressionKeyFrame(0, "this.StartingValue");
		animation.InsertExpressionKeyFrame(0.99f, "this.StartingValue");
		animation.InsertExpressionKeyFrame(1, "this.FinalValue");
		animation.Duration = TimeSpan.FromSeconds(1);
		animation.Target = nameof(Visual.Offset);
		return animation;
	}
}
