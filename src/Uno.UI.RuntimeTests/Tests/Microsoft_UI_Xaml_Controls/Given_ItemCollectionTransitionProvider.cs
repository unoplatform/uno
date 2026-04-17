// MUX Reference Repeater/APITests/ItemCollectionTransitionProviderTests.cs, tag winui3/release/1.6-stable

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public partial class Given_ItemCollectionTransitionProvider
{
	/// <summary>
	/// Validates that the transition provider's StartTransitions callback receives the expected
	/// add/remove/move transitions when QueueTransition is called directly.
	/// Port of ValidateItemCollectionTransitionProvider from WinUI, adjusted to call QueueTransition
	/// directly since ItemsRepeater integration is not yet wired up.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Items_Changed_TransitionProvider_Receives_Correct_Calls()
	{
		var addCalls = new List<ItemCollectionTransition>();
		var removeCalls = new List<ItemCollectionTransition>();
		var moveCalls = new List<ItemCollectionTransition>();

		var transitionProvider = new ItemCollectionTransitionProviderDerived
		{
			ShouldAnimateFunc = _ => true,
			StartTransitionsFunc = transitions =>
			{
				foreach (var transition in transitions)
				{
					var progress = transition.Start();

					switch (transition.Operation)
					{
						case ItemCollectionTransitionOperation.Add:
							addCalls.Add(transition);
							break;
						case ItemCollectionTransitionOperation.Remove:
							removeCalls.Add(transition);
							break;
						case ItemCollectionTransitionOperation.Move:
							moveCalls.Add(transition);
							break;
					}

					progress.Complete();
				}
			}
		};

		// Queue transitions directly — ItemsRepeater integration is not yet wired up in Uno.
		var addTransition = new ItemCollectionTransition(
			transitionProvider,
			new TextBlock(),
			ItemCollectionTransitionOperation.Add,
			ItemCollectionTransitionTriggers.CollectionChangeAdd);

		var removeTransition = new ItemCollectionTransition(
			transitionProvider,
			new TextBlock(),
			ItemCollectionTransitionOperation.Remove,
			ItemCollectionTransitionTriggers.CollectionChangeRemove);

		var moveTransition = new ItemCollectionTransition(
			transitionProvider,
			new TextBlock(),
			ItemCollectionTransitionTriggers.CollectionChangeAdd | ItemCollectionTransitionTriggers.CollectionChangeRemove,
			oldBounds: new Rect(0, 0, 100, 50),
			newBounds: new Rect(0, 50, 100, 50));

		transitionProvider.QueueTransition(addTransition);
		transitionProvider.QueueTransition(removeTransition);
		transitionProvider.QueueTransition(moveTransition);

		// QueueTransition subscribes to CompositionTarget.Rendering; wait for that frame to fire.
		await WaitForRenderingFrame();

		Assert.AreEqual(1, addCalls.Count, "Expected 1 add call");
		Assert.AreEqual(ItemCollectionTransitionOperation.Add, addCalls[0].Operation);
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd, addCalls[0].Triggers);

		Assert.AreEqual(1, removeCalls.Count, "Expected 1 remove call");
		Assert.AreEqual(ItemCollectionTransitionOperation.Remove, removeCalls[0].Operation);
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeRemove, removeCalls[0].Triggers);

		Assert.AreEqual(1, moveCalls.Count, "Expected 1 move call");
		Assert.AreEqual(ItemCollectionTransitionOperation.Move, moveCalls[0].Operation);
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd | ItemCollectionTransitionTriggers.CollectionChangeRemove, moveCalls[0].Triggers);
		Assert.AreEqual(0, moveCalls[0].OldBounds.Y, "Old Y bound should be 0");
		Assert.AreEqual(50, moveCalls[0].NewBounds.Y, "New Y bound should be 50");

		addCalls.Clear();
		removeCalls.Clear();
		moveCalls.Clear();

		// Validate filtering: only animate Add operations.
		transitionProvider.ShouldAnimateFunc = t => t.Operation == ItemCollectionTransitionOperation.Add;

		var addTransition2 = new ItemCollectionTransition(
			transitionProvider,
			new TextBlock(),
			ItemCollectionTransitionOperation.Add,
			ItemCollectionTransitionTriggers.CollectionChangeAdd);

		var removeTransition2 = new ItemCollectionTransition(
			transitionProvider,
			new TextBlock(),
			ItemCollectionTransitionOperation.Remove,
			ItemCollectionTransitionTriggers.CollectionChangeRemove);

		var moveTransition2 = new ItemCollectionTransition(
			transitionProvider,
			new TextBlock(),
			ItemCollectionTransitionTriggers.CollectionChangeAdd | ItemCollectionTransitionTriggers.CollectionChangeRemove,
			oldBounds: new Rect(0, 0, 100, 50),
			newBounds: new Rect(0, 50, 100, 50));

		transitionProvider.QueueTransition(addTransition2);
		transitionProvider.QueueTransition(removeTransition2);
		transitionProvider.QueueTransition(moveTransition2);

		await WaitForRenderingFrame();

		Assert.AreEqual(1, addCalls.Count, "Expected 1 add call in filtered scenario");
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd, addCalls[0].Triggers);

		Assert.AreEqual(0, removeCalls.Count, "Remove should not be animated when filtered");
		Assert.AreEqual(0, moveCalls.Count, "Move should not be animated when filtered");
	}

	/// <summary>
	/// Validates HasStarted transitions through Start() and that TransitionCompleted fires on Complete().
	/// Adjusted to call QueueTransition directly instead of relying on ItemsRepeater integration.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Progress_Complete_TransitionCompleted_Is_Raised()
	{
		ItemCollectionTransition? capturedTransition = null;
		var completedArgs = new List<ItemCollectionTransitionCompletedEventArgs>();

		var transitionProvider = new ItemCollectionTransitionProviderDerived
		{
			ShouldAnimateFunc = _ => true,
			StartTransitionsFunc = transitions =>
			{
				foreach (var transition in transitions)
				{
					capturedTransition = transition;

					Assert.IsFalse(transition.HasStarted, "HasStarted should be false before Start()");
					var progress = transition.Start();
					Assert.IsTrue(transition.HasStarted, "HasStarted should be true after Start()");

					// Repeated Start() should return the same object.
					var progress2 = transition.Start();
					Assert.AreSame(progress, progress2, "Repeated Start() calls should return the same progress object");

					Assert.IsNotNull(progress.Element, "Progress.Element should not be null");
					Assert.AreSame(transition, progress.Transition, "Progress.Transition should match");

					progress.Complete();
				}
			}
		};

		transitionProvider.TransitionCompleted += (s, e) => completedArgs.Add(e);

		var element = new TextBlock();
		var transition = new ItemCollectionTransition(
			transitionProvider,
			element,
			ItemCollectionTransitionOperation.Add,
			ItemCollectionTransitionTriggers.CollectionChangeAdd);

		transitionProvider.QueueTransition(transition);

		// QueueTransition subscribes to CompositionTarget.Rendering; wait for that frame to fire.
		await WaitForRenderingFrame();

		Assert.IsNotNull(capturedTransition, "At least one transition should have been started");
		Assert.IsTrue(completedArgs.Count > 0, "TransitionCompleted should have been raised");
		Assert.AreSame(capturedTransition, completedArgs[0].Transition, "CompletedEventArgs.Transition should match");
		Assert.IsNotNull(completedArgs[0].Element, "CompletedEventArgs.Element should not be null");
	}

	/// <summary>
	/// Waits for the next CompositionTarget.Rendering frame, which is when
	/// ItemCollectionTransitionProvider processes its queued transition batches.
	/// </summary>
	private static Task WaitForRenderingFrame()
	{
		var tcs = new TaskCompletionSource<bool>();
		EventHandler<object>? handler = null;
		handler = (s, e) =>
		{
			CompositionTarget.Rendering -= handler!;
			tcs.TrySetResult(true);
		};
		CompositionTarget.Rendering += handler;
		return tcs.Task;
	}

	private class ItemCollectionTransitionProviderDerived : ItemCollectionTransitionProvider
	{
		public Func<ItemCollectionTransition, bool>? ShouldAnimateFunc { get; set; }
		public Action<IList<ItemCollectionTransition>>? StartTransitionsFunc { get; set; }

		protected override bool ShouldAnimateCore(ItemCollectionTransition transition) =>
			ShouldAnimateFunc?.Invoke(transition) ?? false;

		protected override void StartTransitions(IList<ItemCollectionTransition> transitions) =>
			StartTransitionsFunc?.Invoke(transitions);
	}
}
