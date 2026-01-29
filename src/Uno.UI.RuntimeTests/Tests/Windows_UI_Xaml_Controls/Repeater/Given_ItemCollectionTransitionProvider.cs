// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProviderTests.cs, tag winui3/release/1.8.4

#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater
{
	[TestClass]
	public class Given_ItemCollectionTransitionProvider
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TransitionProvider_Set()
		{
			var data = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => $"Item #{i}"));
			var renderingFired = false;

			var elementFactory = new RecyclingElementFactory();
			elementFactory.RecyclePool = new RecyclePool();
			elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<TextBlock Text='{Binding}' Height='50' />
				</DataTemplate>");

			CompositionTarget.Rendering += (s, e) => renderingFired = true;

			var repeater = new ItemsRepeater
			{
				ItemsSource = data,
				ItemTemplate = elementFactory,
			};

			var scrollHost = new ItemsRepeaterScrollHost
			{
				Width = 400,
				Height = 800,
				ScrollViewer = new ScrollViewer
				{
					Content = repeater
				}
			};

			TestServices.WindowHelper.WindowContent = scrollHost;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(renderingFired, "Rendering should have fired");
		}

		[TestMethod]
		[RunsOnUIThread]
		[Ignore("TransitionManager timing needs more investigation - transitions may be queued after flags are reset")]
		public async Task When_TransitionProvider_TracksCalls()
		{
			var data = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => $"Item #{i}"));

			var elementFactory = new RecyclingElementFactory();
			elementFactory.RecyclePool = new RecyclePool();
			elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<TextBlock Text='{Binding}' Height='50' />
				</DataTemplate>");

			var repeater = new ItemsRepeater
			{
				ItemsSource = data,
				ItemTemplate = elementFactory,
			};

			var scrollHost = new ItemsRepeaterScrollHost
			{
				Width = 400,
				Height = 800,
				ScrollViewer = new ScrollViewer
				{
					Content = repeater
				}
			};

			TestServices.WindowHelper.WindowContent = scrollHost;
			await TestServices.WindowHelper.WaitForIdle();

			var addCalls = new List<CallInfo>();
			var removeCalls = new List<CallInfo>();
			var moveCalls = new List<CallInfo>();

			var transitionProvider = new ItemCollectionTransitionProviderDerived
			{
				ShouldAnimateFunc = (transition) => true,
				StartTransitionsFunc = (transitions) =>
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
			await TestServices.WindowHelper.WaitForIdle();

			data.Insert(0, "new item");
			data.RemoveAt(2);
			await TestServices.WindowHelper.WaitForIdle();

			// Verify add call
			Assert.AreEqual(1, addCalls.Count, "Should have 1 add call");
			Assert.AreEqual(0, addCalls[0].Index, "Add call should be at index 0");
			Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd, addCalls[0].Transition.Triggers);

			// Verify remove call
			Assert.AreEqual(1, removeCalls.Count, "Should have 1 remove call");
			Assert.AreEqual(-1, removeCalls[0].Index, "Remove call element should not be in repeater");
			Assert.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeRemove, removeCalls[0].Transition.Triggers);

			// Verify move call
			Assert.AreEqual(1, moveCalls.Count, "Should have 1 move call");
			Assert.AreEqual(1, moveCalls[0].Index, "Move call should be at index 1");
			Assert.AreEqual(
				ItemCollectionTransitionTriggers.CollectionChangeAdd | ItemCollectionTransitionTriggers.CollectionChangeRemove,
				moveCalls[0].Transition.Triggers);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ShouldAnimate_ReturnsFalse()
		{
			var data = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => $"Item #{i}"));

			var elementFactory = new RecyclingElementFactory();
			elementFactory.RecyclePool = new RecyclePool();
			elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
				@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
					<TextBlock Text='{Binding}' Height='50' />
				</DataTemplate>");

			var repeater = new ItemsRepeater
			{
				ItemsSource = data,
				ItemTemplate = elementFactory,
			};

			var scrollHost = new ItemsRepeaterScrollHost
			{
				Width = 400,
				Height = 800,
				ScrollViewer = new ScrollViewer
				{
					Content = repeater
				}
			};

			TestServices.WindowHelper.WindowContent = scrollHost;
			await TestServices.WindowHelper.WaitForIdle();

			var startTransitionsCalled = false;
			var transitionProvider = new ItemCollectionTransitionProviderDerived
			{
				ShouldAnimateFunc = (transition) => false, // Don't animate
				StartTransitionsFunc = (transitions) =>
				{
					startTransitionsCalled = true;
				}
			};

			repeater.ItemTransitionProvider = transitionProvider;
			await TestServices.WindowHelper.WaitForIdle();

			data.Insert(0, "new item");
			await TestServices.WindowHelper.WaitForIdle();

			// StartTransitions should NOT be called when ShouldAnimate returns false
			Assert.IsFalse(startTransitionsCalled, "StartTransitions should not be called when ShouldAnimate returns false");
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

			public override string ToString()
			{
				return $"Index: {Index} Operation: {Transition.Operation} Triggers: {Transition.Triggers} OldBounds: {Transition.OldBounds} NewBounds {Transition.NewBounds}";
			}
		}

		private class ItemCollectionTransitionProviderDerived : ItemCollectionTransitionProvider
		{
			public Func<ItemCollectionTransition, bool> ShouldAnimateFunc { get; set; }
			public Action<IList<ItemCollectionTransition>> StartTransitionsFunc { get; set; }

			protected override bool ShouldAnimateCore(ItemCollectionTransition transition)
			{
				return ShouldAnimateFunc?.Invoke(transition) ?? false;
			}

			protected override void StartTransitions(IList<ItemCollectionTransition> transitions)
			{
				StartTransitionsFunc?.Invoke(transitions);
			}
		}
	}
}
#endif
