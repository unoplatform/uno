// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransitionProviderTests.cs, tag winui3/release/1.8.4

#pragma warning disable CS0618 // IdleSynchronizer.Wait is obsolete

using Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using MUXControlsTestApp.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.UI.Xaml.Media;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	public class ItemCollectionTransitionProviderTests : MUXApiTestBase
	{
		[TestMethod]
		[Ignore("Task 35797826: ElementAnimatorTests.ValidateElementAnimator test disabled - TransitionManager timing needs investigation")]
		public void ValidateItemCollectionTransitionProvider()
		{
			ItemsRepeater repeater = null;
			ItemCollectionTransitionProviderDerived transitionProvider = null;
			var data = new ObservableCollection<string>(Enumerable.Range(0, 10).Select(i => string.Format("Item #{0}", i)));
			var renderingEvent = new ManualResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				var elementFactory = new RecyclingElementFactory();
				elementFactory.RecyclePool = new RecyclePool();
				elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
						  <TextBlock Text='{Binding}' Height='50' />
					  </DataTemplate>");

				CompositionTarget.Rendering += (sender, args) =>
				{
					renderingEvent.Set();
				};

				repeater = new ItemsRepeater()
				{
					ItemsSource = data,
					ItemTemplate = elementFactory,
				};

				Content = new ItemsRepeaterScrollHost()
				{
					Width = 400,
					Height = 800,
					ScrollViewer = new ScrollViewer
					{
						Content = repeater
					}
				};
			});

			IdleSynchronizer.Wait();
			Verify.IsTrue(renderingEvent.WaitOne(), "Waiting for rendering event");

			List<CallInfo> addCalls = new();
			List<CallInfo> removeCalls = new();
			List<CallInfo> moveCalls = new();
			RunOnUIThread.Execute(() =>
			{
				transitionProvider = new ItemCollectionTransitionProviderDerived()
				{
					ShouldAnimateFunc = (ItemCollectionTransition transition) => true,
					StartTransitionsFunc = (IList<ItemCollectionTransition> transitions) =>
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

				renderingEvent.Reset();
				data.Insert(0, "new item");
				data.RemoveAt(2);
			});

			Verify.IsTrue(renderingEvent.WaitOne(), "Waiting for rendering event");
			IdleSynchronizer.Wait();

			Verify.AreEqual(1, addCalls.Count);
			var call = addCalls[0];
			Verify.AreEqual(0, call.Index);
			Verify.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd, call.Transition.Triggers);

			Verify.AreEqual(1, removeCalls.Count);
			call = removeCalls[0];
			Verify.AreEqual(-1, call.Index); // not in the repeater anymore
			Verify.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeRemove, call.Transition.Triggers);

			Verify.AreEqual(1, moveCalls.Count);
			call = moveCalls[0];
			Verify.AreEqual(1, call.Index);
			Verify.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd | ItemCollectionTransitionTriggers.CollectionChangeRemove, call.Transition.Triggers);
			Verify.AreEqual(0, call.Transition.OldBounds.Y);
			Verify.AreEqual(50, call.Transition.NewBounds.Y);

			addCalls.Clear();
			removeCalls.Clear();
			moveCalls.Clear();

			// Hookup just for show animations and validate.
			RunOnUIThread.Execute(() =>
			{
				transitionProvider.ShouldAnimateFunc = (ItemCollectionTransition transition) => transition.Operation == ItemCollectionTransitionOperation.Add;

				renderingEvent.Reset();
				data.Insert(0, "new item");
				data.RemoveAt(2);
			});

			Verify.IsTrue(renderingEvent.WaitOne(), "Waiting for rendering event");
			IdleSynchronizer.Wait();

			Verify.AreEqual(1, addCalls.Count);
			call = addCalls[0];
			Verify.AreEqual(0, call.Index);
			Verify.AreEqual(ItemCollectionTransitionTriggers.CollectionChangeAdd, call.Transition.Triggers);

			Verify.AreEqual(0, removeCalls.Count);
			Verify.AreEqual(0, moveCalls.Count);
		}

		struct CallInfo
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
		};
	}
}
