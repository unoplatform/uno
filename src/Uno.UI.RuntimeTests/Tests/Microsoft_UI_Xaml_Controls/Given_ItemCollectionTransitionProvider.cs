// MUX Reference Repeater/APITests/ItemCollectionTransitionProviderTests.cs, tag winui3/release/1.6-stable

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public partial class Given_ItemCollectionTransitionProvider
{
	/// <summary>
	/// Validates that the transition provider receives the expected add/remove/move calls
	/// when items are inserted and removed from the repeater's data source.
	/// Port of ValidateItemCollectionTransitionProvider from WinUI.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Items_Changed_TransitionProvider_Receives_Correct_Calls()
	{
		var data = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => $"Item #{i}"));

		var dataTemplate = (DataTemplate)XamlReader.Load(
			@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
				<TextBlock Text='{Binding}' Height='50' />
			</DataTemplate>");

		var repeater = new ItemsRepeater()
		{
			ItemsSource = data,
			ItemTemplate = dataTemplate,
		};

		TestServices.WindowHelper.WindowContent = new ItemsRepeaterScrollHost()
		{
			Width = 400,
			Height = 800,
			ScrollViewer = new ScrollViewer
			{
				Content = repeater
			}
		};

		await TestServices.WindowHelper.WaitForLoaded(repeater);
		await TestServices.WindowHelper.WaitForIdle();

		var addCalls = new List<CallInfo>();
		var removeCalls = new List<CallInfo>();
		var moveCalls = new List<CallInfo>();

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
							addCalls.Add(new CallInfo(repeater.GetElementIndex(progress.Element), transition));
							break;
						case ItemCollectionTransitionOperation.Remove:
							removeCalls.Add(new CallInfo(repeater.GetElementIndex(progress.Element), transition));
							break;
						case ItemCollectionTransitionOperation.Move:
							moveCalls.Add(new CallInfo(repeater.GetElementIndex(progress.Element), transition));
							break;
					}

					progress.Complete();
				}
			}
		};

		repeater.ItemTransitionProvider = transitionProvider;

		data.Insert(0, "new item");
		data.RemoveAt(2);

		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, addCalls.Count, "Expected 1 add call");
		var call = addCalls[0];
		Assert.AreEqual(0, call.Index, "Add call should be at index 0");
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd, call.Transition.Triggers);

		Assert.AreEqual(1, removeCalls.Count, "Expected 1 remove call");
		call = removeCalls[0];
		Assert.AreEqual(-1, call.Index, "Removed item should no longer be in the repeater");
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeRemove, call.Transition.Triggers);

		Assert.AreEqual(1, moveCalls.Count, "Expected 1 move call");
		call = moveCalls[0];
		Assert.AreEqual(1, call.Index, "Moved item should be at index 1");
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd | ItemCollectionTransitionTriggers.CollectionChangeRemove, call.Transition.Triggers);
		Assert.AreEqual(0, call.Transition.OldBounds.Y, "Old Y bound should be 0");
		Assert.AreEqual(50, call.Transition.NewBounds.Y, "New Y bound should be 50 (one item height)");

		addCalls.Clear();
		removeCalls.Clear();
		moveCalls.Clear();

		// Validate filtering: only animate Add operations.
		transitionProvider.ShouldAnimateFunc = t => t.Operation == ItemCollectionTransitionOperation.Add;

		data.Insert(0, "new item");
		data.RemoveAt(2);

		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(1, addCalls.Count, "Expected 1 add call in filtered scenario");
		call = addCalls[0];
		Assert.AreEqual(0, call.Index);
		Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd, call.Transition.Triggers);

		Assert.AreEqual(0, removeCalls.Count, "Remove should not be animated when filtered");
		Assert.AreEqual(0, moveCalls.Count, "Move should not be animated when filtered");
	}

	/// <summary>
	/// Validates HasStarted transitions through Start() and that TransitionCompleted fires on Complete().
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Progress_Complete_TransitionCompleted_Is_Raised()
	{
		var data = new ObservableCollection<string>(Enumerable.Range(0, 3).Select(i => $"Item #{i}"));

		var dataTemplate = (DataTemplate)XamlReader.Load(
			@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
				<TextBlock Text='{Binding}' Height='50' />
			</DataTemplate>");

		var repeater = new ItemsRepeater()
		{
			ItemsSource = data,
			ItemTemplate = dataTemplate,
		};

		TestServices.WindowHelper.WindowContent = new ItemsRepeaterScrollHost()
		{
			Width = 400,
			Height = 800,
			ScrollViewer = new ScrollViewer { Content = repeater }
		};

		await TestServices.WindowHelper.WaitForLoaded(repeater);
		await TestServices.WindowHelper.WaitForIdle();

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
		repeater.ItemTransitionProvider = transitionProvider;

		data.Insert(0, "new item");

		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(capturedTransition, "At least one transition should have been started");
		Assert.IsTrue(completedArgs.Count > 0, "TransitionCompleted should have been raised");
		Assert.AreSame(capturedTransition, completedArgs[0].Transition, "CompletedEventArgs.Transition should match");
		Assert.IsNotNull(completedArgs[0].Element, "CompletedEventArgs.Element should not be null");
	}

	private struct CallInfo
	{
		public CallInfo(int index, ItemCollectionTransition transition)
		{
			Index = index;
			Transition = transition;
		}

		public int Index { get; set; }
		public ItemCollectionTransition Transition { get; set; }

		public override string ToString() =>
			$"Index: {Index} Operation: {Transition.Operation} Triggers: {Transition.Triggers} " +
			$"OldBounds: {Transition.OldBounds} NewBounds: {Transition.NewBounds}";
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
