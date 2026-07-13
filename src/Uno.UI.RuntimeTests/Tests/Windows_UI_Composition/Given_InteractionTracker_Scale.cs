#nullable enable

using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23688")]
public class Given_InteractionTracker_Scale
{
	[TestMethod]
#if !HAS_COMPOSITION_API
	[Ignore("Composition APIs are not supported on this platform.")]
#endif
	public async Task When_TryUpdateScale_AtOrigin()
	{
		var border = new Border
		{
			Width = 50,
			Height = 50,
		};

		try
		{
			await UITestHelper.Load(border);

			var owner = new TrackerOwner();
			var tracker = InteractionTracker.CreateWithOwner(
				ElementCompositionPreview.GetElementVisual(border).Compositor,
				owner);
			tracker.MinPosition = new Vector3(-1000.0f);
			tracker.MaxPosition = new Vector3(1000.0f);
			tracker.MinScale = 0.5f;
			tracker.MaxScale = 4.0f;

			var requestId = tracker.TryUpdateScale(2.0f, Vector3.Zero);
			var args = await owner.ValuesChangedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.AreEqual(requestId, args.RequestId);
			Assert.AreEqual(Vector3.Zero, args.Position);
			Assert.AreEqual(2.0f, args.Scale);
			Assert.AreEqual(Vector3.Zero, tracker.Position);
			Assert.AreEqual(2.0f, tracker.Scale);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !HAS_COMPOSITION_API
	[Ignore("Composition APIs are not supported on this platform.")]
#endif
	public async Task When_TryUpdateScale_WithCenterPoint()
	{
		var border = new Border
		{
			Width = 50,
			Height = 50,
		};

		try
		{
			await UITestHelper.Load(border);

			var owner = new TrackerOwner();
			var tracker = InteractionTracker.CreateWithOwner(
				ElementCompositionPreview.GetElementVisual(border).Compositor,
				owner);
			tracker.MinPosition = new Vector3(-1000.0f);
			tracker.MaxPosition = new Vector3(1000.0f);
			tracker.MinScale = 0.5f;
			tracker.MaxScale = 4.0f;

			var centerPoint = new Vector3(10.0f, 20.0f, 0.0f);
			var requestId = tracker.TryUpdateScale(2.0f, centerPoint);
			var args = await owner.ValuesChangedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.AreEqual(requestId, args.RequestId);
			Assert.AreEqual(centerPoint, args.Position);
			Assert.AreEqual(2.0f, args.Scale);
			Assert.AreEqual(centerPoint, tracker.Position);
			Assert.AreEqual(2.0f, tracker.Scale);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private sealed class TrackerOwner : IInteractionTrackerOwner
	{
		public TaskCompletionSource<InteractionTrackerValuesChangedArgs> ValuesChangedCompletion { get; } =
			new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args)
		{
		}

		public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
		{
		}

		public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args)
		{
		}

		public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
		{
		}

		public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args)
		{
		}

		public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
		{
			ValuesChangedCompletion.TrySetResult(args);
		}
	}
}
