// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using MUXControlsTestApp.Utilities;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using System.Threading;
using Common;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using VirtualizingLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.VirtualizingLayout;
using ItemsRepeater = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsRepeater;
using ElementFactory = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ElementFactory;
using RecyclingElementFactory = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RecyclingElementFactory;
using RecyclePool = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RecyclePool;
using StackLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.StackLayout;
using FlowLayout = Microsoft/* UWP don't rename */.UI.Xaml.Controls.FlowLayout;
using ItemsRepeaterScrollHost = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsRepeaterScrollHost;
using AnimationContext = Microsoft/* UWP don't rename */.UI.Xaml.Controls.AnimationContext;
using System.Collections.Generic;
using Uno.UI.RuntimeTests;
using Private.Infrastructure;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public class FlowLayoutCollectionChangeTests : MUXApiTestBase
	{
		[TestMethod]
		public void ValidateInserts()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList());
				var repeater = SetupRepeater(dataSource);

				Log.Comment("Insert in realized range: Inserting 3 items at index 4");
				dataSource.Insert(index: 4, count: 3, reset: false);
				repeater.UpdateLayout();

				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.IsGreaterThan(realized, 0);

				Log.Comment("Insert before realized range: Inserting 3 items at index 0");
				dataSource.Insert(index: 0, count: 3, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.IsGreaterThan(realized, 0);

				Log.Comment("Insert after realized range: Inserting 3 items at index 10");
				dataSource.Insert(index: 10, count: 3, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.IsGreaterThan(realized, 0);
			});
		}

		[TestMethod]
		public void ValidateSentinelsDuringInserts()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList());
				int getElementCallCount = 0;
				int recycleElementCallCount = 0;

				var elementFactory = new RecyclingElementFactoryDerived()
				{
					Templates = { { "key", SharedHelpers.GetDataTemplate("<TextBlock Text='{Binding}' Height='100' />") } },
					RecyclePool = new RecyclePool(),
					GetElementFunc = (int index, UIElement owner, UIElement elementFromBase) =>
					{
						getElementCallCount++;
						return elementFromBase;
					},
					ClearElementFunc = (UIElement element, UIElement owner) => recycleElementCallCount++,
					ValidateElementIndices = false
				};

				var repeater = SetupRepeater(dataSource, elementFactory);

				getElementCallCount = 0;
				recycleElementCallCount = 0;
				Log.Comment("Insert in realized range: Inserting 100 items at index 1");
				dataSource.Insert(index: 1, count: 100, reset: false);
				repeater.UpdateLayout();

				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.IsGreaterThan(realized, 0);

				Verify.IsLessThan(getElementCallCount, 6);
				Verify.IsLessThan(recycleElementCallCount, 6);
			});
		}

		[TestMethod]
		[Ignore("UNO: Test does not pass yet with Uno (The EffectiveViewport is updated to late) https://github.com/unoplatform/uno/issues/4529")]
		public async Task CanRemoveItemsStartingBeforeRealizedRangeAsync()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(Enumerable.Range(0, 20).ToList()));
			ScrollViewer scrollViewer = null;
			ItemsRepeater repeater = null;
			var viewChangedEvent = new UnoManualResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				repeater = SetupRepeater(dataSource, ref scrollViewer);
				scrollViewer.ViewChanged += (sender, args) =>
				{
					if (!args.IsIntermediate)
					{
						viewChangedEvent.Set();
					}
				};

				scrollViewer.ChangeView(null, 600, null, true);
			});

			Verify.IsTrue(await viewChangedEvent.WaitOne(DefaultWaitTime), "Waiting for ViewChanged.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Remove before realized range: start:(0)beforeView end:(1)beforeView.");
				dataSource.Remove(index: 0, count: 2, reset: false);
				repeater.UpdateLayout();

				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);

				Log.Comment("Remove before realized range: start:(0)beforeView end:(4)inview.");
				dataSource.Remove(index: 0, count: 5, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);

				Log.Comment("Insert before realized range: Inserting 10 items at index 0");
				dataSource.Insert(index: 0, count: 10, reset: false);
				repeater.UpdateLayout();
				VerifyRealizedRange(repeater, dataSource);

				Log.Comment("Insert after realized range: Inserting 10 items at index 19");
				dataSource.Insert(index: 19, count: 10, reset: false);
				repeater.UpdateLayout();
				VerifyRealizedRange(repeater, dataSource);

				Log.Comment("Remove before realized range: start:(5)beforeView end:(25)afterView.");
				dataSource.Remove(index: 5, count: 20, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.IsLessThanOrEqual(2, realized);
			});
		}

		[TestMethod]
#if __WASM__ || __IOS__ || __ANDROID__ || __SKIA__ || __MACOS__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
		public void CanRemoveItemsStartingInRealizedRange()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList());
				var repeater = SetupRepeater(dataSource);

				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);

				Log.Comment("Remove in realized range: start:(1)InView end:(3)InView.");
				dataSource.Remove(index: 1, count: 3, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);

				Log.Comment("Remove in realized range: start:(1)InView end:(6)InView.");
				dataSource.Remove(index: 1, count: 6, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(1, realized);
			});
		}

		[TestMethod]
		[TestProperty("Bug", "19259478")]
#if __WASM__ || __IOS__ || __ANDROID__ || __SKIA__ || __MACOS__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
		public void CanRemoveAndInsertItemsInRealizedRange()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSource(Enumerable.Range(0, 3).ToList());
				var repeater = SetupRepeater(dataSource);

				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);

				Log.Comment("Remove index 1 in realized range");
				dataSource.Remove(index: 1, count: 1, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(2, realized);

				Log.Comment("Add 5 items at index 1");
				dataSource.Insert(index: 1, count: 5, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);
			});
		}

		[TestMethod]
#if __WASM__ || __IOS__ || __ANDROID__ || __SKIA__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
		public void CanRemoveItemsAfterRealizedRange()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList());
				var repeater = SetupRepeater(dataSource);

				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);

				Log.Comment("Remove after realized range: start:(8)afterView end:(9)afterView.");
				dataSource.Remove(index: 8, count: 2, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);
			});
		}

		[TestMethod]
#if __WASM__ || __IOS__ || __ANDROID__ || __MACOS__
		[Ignore("Fails on WASM, iOS, Android and macOS https://github.com/unoplatform/uno/issues/4529")]
#endif
#if __SKIA__
		[Ignore("https://github.com/unoplatform/uno/issues/7271")]
#endif
		public async Task CanReplaceSingleItem()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList()));
			ScrollViewer scrollViewer = null;
			ItemsRepeater repeater = null;
			var viewChangedEvent = new UnoManualResetEvent(false);
			int elementsCleared = 0;
			int elementsPrepared = 0;

			RunOnUIThread.Execute(() =>
			{
				repeater = SetupRepeater(dataSource, ref scrollViewer);
				scrollViewer.ViewChanged += (sender, args) =>
				{
					if (!args.IsIntermediate)
					{
						viewChangedEvent.Set();
					}
				};

				repeater.ElementPrepared += (sender, args) => { elementsPrepared++; };
				repeater.ElementClearing += (sender, args) => { elementsCleared++; };

				scrollViewer.ChangeView(null, 200, null, true);
			});

			Verify.IsTrue(await viewChangedEvent.WaitOne(DefaultWaitTime), "Waiting for ViewChanged.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);

				Log.Comment("Replace before realized range.");
				dataSource.Replace(index: 0, oldCount: 1, newCount: 1, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);

				Log.Comment("Replace in realized range.");
				elementsPrepared = 0;
				elementsCleared = 0;
				dataSource.Replace(index: 2, oldCount: 1, newCount: 1, reset: false);
				repeater.UpdateLayout();
				Verify.AreEqual(1, elementsPrepared);
				Verify.AreEqual(1, elementsCleared);

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);

				Log.Comment("Replace after realized range");
				dataSource.Replace(index: 8, oldCount: 1, newCount: 1, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);
			});
		}

		[TestMethod]
#if __WASM__ || __IOS__ || __ANDROID__ || __MACOS__
		[Ignore("Fails on WASM, iOS, Android and macOS https://github.com/unoplatform/uno/issues/4529")]
#endif
#if __SKIA__
		[Ignore("https://github.com/unoplatform/uno/issues/7271")]
#endif
		public async Task CanMoveItem()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList()));
			ScrollViewer scrollViewer = null;
			ItemsRepeater repeater = null;
			var viewChangedEvent = new UnoManualResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				repeater = SetupRepeater(dataSource, ref scrollViewer);
				scrollViewer.ViewChanged += (sender, args) =>
				{
					if (!args.IsIntermediate)
					{
						viewChangedEvent.Set();
					}
				};
				scrollViewer.ChangeView(null, 400, null, true);
			});

			Verify.IsTrue(await viewChangedEvent.WaitOne(DefaultWaitTime), "Waiting for ViewChanged.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);

				Log.Comment("Move before realized range.");
				dataSource.Move(oldIndex: 0, newIndex: 1, count: 2, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);

				Log.Comment("Move in realized range.");
				dataSource.Move(oldIndex: 3, newIndex: 5, count: 2, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);

				Log.Comment("Move after realized range");
				dataSource.Move(oldIndex: 7, newIndex: 8, count: 2, reset: false);
				repeater.UpdateLayout();

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(4, realized);
			});
		}

#if false
		[TestMethod]
		public async Task VerifyElement0OwnershipInUniformGridLayout()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(new List<int>()));
			ItemsRepeater repeater = null;
			int elementsCleared = 0;
			int elementsPrepared = 0;

			RunOnUIThread.Execute(() =>
			{
				repeater = SetupRepeater(dataSource, new UniformGridLayout());
				repeater.ElementPrepared += (sender, args) => { elementsPrepared++; };
				repeater.ElementClearing += (sender, args) => { elementsCleared++; };
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Add two item");
				dataSource.Insert(index: 0, count: 1, reset: false);
				dataSource.Insert(index: 1, count: 1, reset: false);
				repeater.UpdateLayout();
				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(2, realized);

				Log.Comment("replace the first item");
				dataSource.Replace(index: 0, oldCount: 1, newCount: 1, reset: false);
				repeater.UpdateLayout();
				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(2, realized);

				Log.Comment("Remove two items");
				dataSource.Remove(index: 1, count: 1, reset: false);
				dataSource.Remove(index: 0, count: 1, reset: false);
				repeater.UpdateLayout();
				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(0, realized);

				Verify.AreEqual(elementsPrepared, elementsCleared);
			});
		}
#endif

		[TestMethod]
		[Ignore("Failing for now https://github.com/unoplatform/uno/issues/4529")]
		[RequiresFullWindow]
		public async Task EnsureReplaceOfAnchorDoesNotResetAllContainers()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList()));
			ScrollViewer scrollViewer = null;
			ItemsRepeater repeater = null;
			var viewChangedEvent = new UnoManualResetEvent(false);
			int elementsCleared = 0;
			int elementsPrepared = 0;

			RunOnUIThread.Execute(() =>
			{
				repeater = SetupRepeater(dataSource, ref scrollViewer);
				repeater.ElementPrepared += (sender, args) => { elementsPrepared++; };
				repeater.ElementClearing += (sender, args) => { elementsCleared++; };
			});

			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);

				Log.Comment("Replace anchor element 0");
				elementsPrepared = 0;
				elementsCleared = 0;
				dataSource.Replace(index: 0, oldCount: 1, newCount: 1, reset: false);
				repeater.UpdateLayout();
				Verify.AreEqual(1, elementsPrepared);
				Verify.AreEqual(1, elementsCleared);

				realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);
			});
		}

		//[TestMethod]
		public void ReplaceMultipleItems()
		{
			// TODO: Lower prioirty scenario. Tracked by work item: 9738020
			throw new NotImplementedException();
		}

		[TestMethod]
#if __WASM__ || __IOS__ || __ANDROID__ || __SKIA__ || __MACOS__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
		public void ValidateStableResets()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSourceWithUniqueId(Enumerable.Range(0, 3).ToList());
				var repeater = SetupRepeater(dataSource);

				Log.Comment("Reset collection");
				dataSource.GetAtCallCount = 0;
				dataSource.Reset();
				repeater.UpdateLayout();

				// Make sure data was not requested because during a stable reset
				// the elements and associated bound data are reused
				Verify.AreEqual(0, dataSource.GetAtCallCount);
				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);
			});
		}

		[TestMethod]
#if __WASM__ || __IOS__ || __ANDROID__ || __MACOS__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
#if __SKIA__
		[RequiresFullWindow]
#endif
		public void ValidateRegularResets()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSource(Enumerable.Range(0, 3).ToList());
				var repeater = SetupRepeater(dataSource);

				Log.Comment("Reset collection");
				dataSource.GetAtCallCount = 0;
				dataSource.Reset();
				repeater.UpdateLayout();

				// Make sure data was requested because during a normal reset elements (already bound with data) are not reused.
				Verify.AreEqual(3, dataSource.GetAtCallCount);
				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(3, realized);
			});
		}

#if UNO_REFERENCE_API // https://github.com/unoplatform/uno/issues/4529
		[TestMethod]
#if __WASM__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
#if __SKIA__
		[RequiresFullWindow]
#endif
		public void ValidateClear()
		{
			RunOnUIThread.Execute(() =>
			{
				var dataSource = new CustomItemsSource(Enumerable.Range(0, 3).ToList());
				var repeater = SetupRepeater(dataSource, new FlowLayout());

				Log.Comment("Clear the collection");
				dataSource.Clear();
				repeater.UpdateLayout();

				var realized = VerifyRealizedRange(repeater, dataSource);
				Verify.AreEqual(0, realized);
			});
		}
#endif

		private ItemsRepeater SetupRepeater(CustomItemsSource dataSource)
		{
			ScrollViewer sv = null;
			return SetupRepeater(dataSource, GetElementFactory(), ref sv, new StackLayout());
		}

		private ItemsRepeater SetupRepeater(CustomItemsSource dataSource, VirtualizingLayout layout)
		{
			ScrollViewer sv = null;
			return SetupRepeater(dataSource, GetElementFactory(), ref sv, layout);
		}

		private ItemsRepeater SetupRepeater(CustomItemsSource dataSource, ElementFactory elementFactory = null)
		{
			ScrollViewer sv = null;

			return SetupRepeater(dataSource, elementFactory, ref sv, new StackLayout());
		}

		private ItemsRepeater SetupRepeater(CustomItemsSource dataSource, ref ScrollViewer scrollViewer)
		{
			return SetupRepeater(dataSource, GetElementFactory(), ref scrollViewer, new StackLayout());
		}

		private RecyclingElementFactory GetElementFactory()
		{
			var elementFactory = new RecyclingElementFactory();
			elementFactory.RecyclePool = new RecyclePool();
			elementFactory.Templates["Item"] = SharedHelpers.GetDataTemplate(@"<TextBlock Text='{Binding}' Height='100' />");
			return elementFactory;
		}

		private ItemsRepeater SetupRepeater(CustomItemsSource dataSource, ElementFactory elementFactory, ref ScrollViewer scrollViewer, VirtualizingLayout layout)
		{
			var repeater = new ItemsRepeater()
			{
				ItemsSource = dataSource,
				ItemTemplate = elementFactory,
				Layout = layout,
				VerticalCacheLength = 0,
				HorizontalCacheLength = 0
			};

			scrollViewer = new ScrollViewer
			{
				Content = repeater
			};

			Content = new ItemsRepeaterScrollHost()
			{
				Width = 200,
				Height = 200,
				ScrollViewer = scrollViewer
			};

			Content.UpdateLayout();
			if (dataSource.Count > 0)
			{
				int realized = VerifyRealizedRange(repeater, dataSource);
				Verify.IsGreaterThan(realized, 0);
			}

			return repeater;
		}
		private int VerifyRealizedRange(ItemsRepeater repeater, CustomItemsSource dataSource)
		{
			int numRealized = 0;
			// trace
			Log.Comment("index:ItemsSourceView:Item");
			for (int i = 0; i < dataSource.Inner.Count; i++)
			{
				var element = repeater.TryGetElement(i);
				if (element != null)
				{
					var data = ((TextBlock)element).Text;
					Log.Comment("{0} {1}:[{2}]", i, dataSource.GetAt(i), data);
					numRealized++;
				}
				else
				{
					Log.Comment(i + ":[null]");
				}
			}

			// verify
			for (int i = 0; i < dataSource.Inner.Count; i++)
			{
				var element = repeater.TryGetElement(i);
				if (element != null)
				{
					var data = ((TextBlock)element).Text;
					Verify.AreEqual(dataSource.GetAt(i).ToString(), data);
				}
			}

			return numRealized;
		}

		private int DefaultWaitTime = 2000;
	}
}
