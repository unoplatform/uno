// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common;
using Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests.Common.Mocks;
using MUXControlsTestApp.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
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

#if !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif
using VirtualizingLayout = Microsoft.UI.Xaml.Controls.VirtualizingLayout;
using ItemsRepeater = Microsoft.UI.Xaml.Controls.ItemsRepeater;
using ItemsSourceView = Microsoft.UI.Xaml.Controls.ItemsSourceView;
using ElementFactory = Microsoft.UI.Xaml.Controls.ElementFactory;
using VirtualizingLayoutContext = Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext;
using RecyclingElementFactory = Microsoft.UI.Xaml.Controls.RecyclingElementFactory;
using RecyclePool = Microsoft.UI.Xaml.Controls.RecyclePool;
using StackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using ItemsRepeaterScrollHost = Microsoft.UI.Xaml.Controls.ItemsRepeaterScrollHost;
using Private.Infrastructure;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	public class ViewManagerTests : MUXApiTestBase
	{
		[TestMethod]
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
		public void CanQueryElementFactory()
		{
			RunOnUIThread.Execute(() =>
			{
				var data = new ObservableCollection<string>();
				var dataSource = MockItemsSource.CreateDataSource(data, supportsUniqueIds: false);
				var elementFactory = MockElementFactory.CreateElementFactory(new List<UIElement> { new ContentControl() });
				var repeater = CreateRepeater(dataSource, elementFactory);

				Content = repeater;
				repeater.UpdateLayout();

				// Our layout will query the size though once for every layout pass.
				// The first layout pass is a bit special because we don't have a viewport and
				// we will invalidate measure when we get one after the first arrange pass.
				dataSource.ValidateGetSizeCalls(1); // GetSize calls are cached by ItemsSourceView.
				elementFactory.ValidateRecycleElementCalls();

				data.Add("Item #1");
				repeater.UpdateLayout();

				var item1 = (UIElement)VisualTreeHelper.GetChild(repeater, 0);

				// One GetSize invocation from the layout, another one from the view manager.
				dataSource.ValidateGetSizeCalls(2); // GetSize calls are cached by ItemsSourceView
				dataSource.ValidateGetAtCalls(new MockItemsSource.GetAtCallInfo(0));
				elementFactory.ValidateGetElementCalls(new MockElementFactory.GetElementCallInfo(0, repeater));
				elementFactory.ValidateRecycleElementCalls();
				Verify.AreEqual(item1, repeater.TryGetElement(0));

				data.RemoveAt(0);
				repeater.UpdateLayout();

				dataSource.ValidateGetAtCalls();
				dataSource.ValidateGetSizeCalls(1); // GetSize calls are cached by ItemsSourceView
													// Whenever we get an element from the view generator, we call HasKeyIndexMapping to see if we should
													// store its unique id or not.
				dataSource.ValidateGetItemIdCalls();
				elementFactory.ValidateGetElementCalls();
				elementFactory.ValidateRecycleElementCalls(new MockElementFactory.RecycleElementCallInfo(item1, repeater));
			});
		}

		[TestMethod] // Issue #1018
		[Ignore]
		public async Task CanPinFocusedElements()
		{
			// Setup a grouped repeater scenario with two groups each containing two items.
			var data = new ObservableCollection<ObservableCollection<string>>(Enumerable
				.Range(0, 2)
				.Select(i => new ObservableCollection<string>(Enumerable
					.Range(0, 2)
					.Select(j => string.Format("Item #{0}.{1}", i, j)))));
			List<ContentControl>[] itemElements = null;
			ItemsRepeater[] innerRepeaters = null;
			List<StackPanel> groupElements = null;
			ItemsRepeater rootRepeater = null;
			var gotFocus = new UnoManualResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				itemElements = new[] {
					Enumerable.Range(0, 2).Select(i => new ContentControl()).ToList(),
					Enumerable.Range(0, 2).Select(i => new ContentControl()).ToList()
				};

				itemElements[0][0].GotFocus += delegate { gotFocus.Set(); };

				innerRepeaters = Enumerable.Range(0, 2).Select(i => CreateRepeater(
					MockItemsSource.CreateDataSource(data[i], supportsUniqueIds: false),
					MockElementFactory.CreateElementFactory(itemElements[i]))).ToArray();

				groupElements = Enumerable.Range(0, 2).Select(i =>
				{
					var panel = new StackPanel();
					panel.Children.Add(new ContentControl());
					panel.Children.Add(innerRepeaters[i]);
					return panel;
				}).ToList();

				rootRepeater = CreateRepeater(
				   MockItemsSource.CreateDataSource(data, supportsUniqueIds: false),
				   MockElementFactory.CreateElementFactory(groupElements));
				Content = rootRepeater;
				rootRepeater.UpdateLayout();

				itemElements[0][0].Focus(FocusState.Keyboard);
			});

			Verify.IsTrue(await gotFocus.WaitOne(DefaultWaitTimeInMS), "Waiting for focus event on the first element of the first group.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment("Recycle focused element 0.0 and validate it's still realized because it is pinned.");
				{
					var ctx = (VirtualizingLayoutContext)innerRepeaters[0].Tag;
					ctx.RecycleElement(itemElements[0][0]);
					Verify.AreEqual(0, innerRepeaters[0].GetElementIndex(itemElements[0][0]));
				}

				Log.Comment("Recycle element 0.1 and validate it's no longer realized because it is not pinned.");
				{
					var ctx = (VirtualizingLayoutContext)innerRepeaters[0].Tag;
					ctx.RecycleElement(itemElements[0][1]);
					Verify.AreEqual(-1, innerRepeaters[0].GetElementIndex(itemElements[0][1]));
				}

				Log.Comment("Recycle group 0 and validate it's still realized because one of its items is pinned.");
				{
					var ctx = (VirtualizingLayoutContext)rootRepeater.Tag;
					ctx.RecycleElement(groupElements[0]);
					Verify.AreEqual(0, rootRepeater.GetElementIndex(groupElements[0]));
				}

				itemElements[1][1].GotFocus += delegate { gotFocus.Set(); };
				itemElements[1][1].Focus(FocusState.Keyboard);
			});

			Verify.IsTrue(await gotFocus.WaitOne(DefaultWaitTimeInMS), "Waiting for focus event on the second element of the second group.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				Log.Comment(@"Move focus to item 1.1 and validate item 0.0 and group 0 are recycled because 
				 the only thing keeping them around is the fact that item 0.0 was focus pinned");
				{
					((VirtualizingLayoutContext)rootRepeater.Tag).RecycleElement(groupElements[0]);
					((VirtualizingLayoutContext)innerRepeaters[0].Tag).RecycleElement(itemElements[0][0]);

					Verify.AreEqual(-1, rootRepeater.GetElementIndex(groupElements[0]));
					Verify.AreEqual(-1, innerRepeaters[0].GetElementIndex(itemElements[0][0]));
					Verify.AreEqual(1, innerRepeaters[0].GetElementIndex(itemElements[1][1]));
				}

				Log.Comment(@"Delete item 1.1 from the data. This will force the element to get recycled even if it's pinned.");
				{
					data[1].RemoveAt(1);
					rootRepeater.UpdateLayout();

					Verify.AreEqual(-1, innerRepeaters[1].GetElementIndex(itemElements[1][1]));
				}
			});
		}

		[TestMethod] // Issue 1018
		[Ignore]
		public async Task CanReuseElementsDuringUniqueIdReset()
		{
			var data = new WinRTCollection(Enumerable.Range(0, 2).Select(i => string.Format("Item #{0}", i)));
			List<UIElement> mapping = null;
			ItemsRepeater repeater = null;
			MockElementFactory elementFactory = null;
			ContentControl focusedElement = null;

			RunOnUIThread.Execute(() =>
			{
				mapping = new List<UIElement> { new ContentControl(), new ContentControl() };
				repeater = CreateRepeater(
					MockItemsSource.CreateDataSource(data, supportsUniqueIds: true),
					MockElementFactory.CreateElementFactory(mapping));
				elementFactory = (MockElementFactory)repeater.ItemTemplate;

				Content = repeater;
				repeater.UpdateLayout();

				focusedElement = (ContentControl)repeater.TryGetElement(1);
				focusedElement.Focus(FocusState.Keyboard);
			});
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				elementFactory.ValidateGetElementCalls(
				new MockElementFactory.GetElementCallInfo(0, repeater),
				new MockElementFactory.GetElementCallInfo(1, repeater));
				elementFactory.ValidateRecycleElementCalls();

				data.ResetWith(new[] { data[0], "New item" });

				Verify.AreEqual(0, repeater.GetElementIndex(mapping[0]));
				Verify.AreEqual(1, repeater.GetElementIndex(mapping[1]));
				Verify.IsNull(repeater.TryGetElement(0));
				Verify.IsNull(repeater.TryGetElement(1));

				elementFactory.ValidateGetElementCalls(/* GetElement should not be called */);
				elementFactory.ValidateRecycleElementCalls(/* RecycleElement should not be called */);

				mapping[1] = new ContentControl(); // For "New Item" 

				repeater.UpdateLayout();

				Verify.AreEqual(0, repeater.GetElementIndex(mapping[0]));
				Verify.AreEqual(1, repeater.GetElementIndex(mapping[1]));
				Verify.AreEqual(mapping[0], repeater.TryGetElement(0));
				Verify.AreEqual(mapping[1], repeater.TryGetElement(1));

				elementFactory.ValidateGetElementCalls(
					new MockElementFactory.GetElementCallInfo(1, repeater));
				elementFactory.ValidateRecycleElementCalls(
					new MockElementFactory.RecycleElementCallInfo(focusedElement, repeater));

				// If the focused element survived the reset, we will keep focus on it. If not, we 
				// try to find one based on the index. In this case, the focused element (index 1) 
				// got recycled, and we still have index 1 after the stable reset, so the new index 1 
				// will get focused. Note that recycling the elements to view generator in the case of
				// stable reset happens during the arrange, so by that time we will have pulled elements
				// from the stable reset pool and maybe created some new elements as well.
				int index = repeater.GetElementIndex(focusedElement);
				Log.Comment("focused index " + index);
				Verify.AreEqual(mapping[1], FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			});
		}

		[TestMethod]
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
		public async Task CanChangeFocusAfterUniqueIdReset()
		{
			var data = new WinRTCollection(Enumerable.Range(0, 2).Select(i => string.Format("Item #{0}", i)));
			object dataSource = null;
			RunOnUIThread.Execute(() => dataSource = MockItemsSource.CreateDataSource(data, supportsUniqueIds: true));
			var (repeater, _) = await SetupRepeater(dataSource);
			Control focusedElement = null;

			RunOnUIThread.Execute(() =>
			{
				focusedElement = (Control)repeater.TryGetElement(0);
				focusedElement.Focus(FocusState.Keyboard);
			});

			await TestServices.WindowHelper.WaitForIdle();
			RunOnUIThread.Execute(() =>
			{
				data.Reset();
			});

			await TestServices.WindowHelper.WaitForIdle();
			RunOnUIThread.Execute(() =>
			{
				// Still focused.
				Verify.AreEqual(focusedElement, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));

				// Change focused element.
				focusedElement = (Control)repeater.TryGetElement(1);
				focusedElement.Focus(FocusState.Keyboard);
			});

			await TestServices.WindowHelper.WaitForIdle();
			RunOnUIThread.Execute(() =>
			{
				// Focus is on the new element.
				Verify.AreEqual(focusedElement, FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot));
			});
		}

		[TestMethod]
		[TestProperty("Description", "Verifies the correct items are generated when invoking StartBringIntoView for a disconnected item.")]
		[Ignore("UNO: Requires CompositionPropertySpy.SynchronouslyTickUIThread to drive cache buffer growth deterministically.")]
		public async Task ValidateBringIntoViewOperations()
		{
			await ValidateBringIntoViewOperationsHelper(0.0 /*cacheLengths*/);
			await ValidateBringIntoViewOperationsHelper(2.0 /*cacheLengths*/);
		}

		private async Task ValidateBringIntoViewOperationsHelper(double cacheLengths)
		{
			Log.Comment("\r\nValidateBringIntoViewOperations cacheLengths: " + cacheLengths);

			string strRealizedItemsIndices = null;
			List<int> realizedItemsIndices = null;
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(Enumerable.Range(0, 200).ToList()));

			var (itemsRepeater, _) = await SetupRepeater(dataSource: null, itemContent: @"<TextBlock Text='{Binding}' Height='24'/>");

			RunOnUIThread.Execute(() =>
			{
				realizedItemsIndices = new List<int>();

				itemsRepeater.ElementPrepared += (sender, args) =>
				{
					realizedItemsIndices.Add(args.Index);
				};

				itemsRepeater.ElementClearing += (sender, args) =>
				{
					int index = sender.GetElementIndex(args.Element);
					realizedItemsIndices.Remove(index);
				};

				itemsRepeater.HorizontalCacheLength = cacheLengths;
				itemsRepeater.VerticalCacheLength = cacheLengths;
				itemsRepeater.ItemsSource = dataSource;
			});

			// The viewport size being 200px and realization window increase being 40px per measure pass,
			// make sure there are 2.5 x cacheLengths passes to land on the final realized items.
			// cacheLengths: number of viewports constituting the pre-fetch buffer, once completely built.
			//               Half of it is before the current viewport, and half after.
			// viewport: 200px
			// cacheIncrement: 40px (buffer increment per measure pass on each side)
			// Measure passes needed to fill the pre-fetch buffer: viewport / cacheIncrement / 2 x cacheLengths = 2.5 x cacheLengths

			await TestServices.WindowHelper.WaitForIdle();
			strRealizedItemsIndices = await LogRealizedIndicesAsync(realizedItemsIndices, (int)(2.5 * cacheLengths + 1));
			Verify.AreEqual(
				cacheLengths == 0 ? " 0 1 2 3 4 5 6 7 8 9" : " 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17",
				strRealizedItemsIndices);

			await StartBringIndexIntoViewAsync(itemsRepeater, index: 99);
			strRealizedItemsIndices = await LogRealizedIndicesAsync(realizedItemsIndices, (int)(2.5 * cacheLengths + 1));
			Verify.AreEqual(
				cacheLengths == 0 ? " 99 100 98 97 96 95 94 93 92 91 90" : " 99 100 101 102 103 104 105 106 107 108 109 98 97 96 95 94 93 92 91 90 89 88 87 86 85 84 83 82",
				strRealizedItemsIndices);

			await StartBringIndexIntoViewAsync(itemsRepeater, index: 199);
			strRealizedItemsIndices = await LogRealizedIndicesAsync(realizedItemsIndices, (int)(2.5 * cacheLengths + 1));
			Verify.AreEqual(
				cacheLengths == 0 ? " 199 198 197 196 195 194 193 192 191 190" : " 199 198 197 196 195 194 193 192 191 190 189 188 187 186 185 184 183 182",
				strRealizedItemsIndices);

			await StartBringIndexIntoViewAsync(itemsRepeater, index: 49);
			strRealizedItemsIndices = await LogRealizedIndicesAsync(realizedItemsIndices, (int)(2.5 * cacheLengths + 1));
			Verify.AreEqual(
				cacheLengths == 0 ? " 49 50 51 52 53 54 55 56 57 58 48" : " 49 50 51 52 53 54 55 56 57 58 59 60 61 62 63 64 65 66 48 47 46 45 44 43 42 41 40 39",
				strRealizedItemsIndices);

			await StartBringIndexIntoViewAsync(itemsRepeater, index: 0);
			strRealizedItemsIndices = await LogRealizedIndicesAsync(realizedItemsIndices, (int)(2.5 * cacheLengths + 1));
			Verify.AreEqual(
				cacheLengths == 0 ? " 0 1 2 3 4 5 6 7 8 9" : " 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17",
				strRealizedItemsIndices);
		}

		private async Task StartBringIndexIntoViewAsync(ItemsRepeater itemsRepeater, int index)
		{
			RunOnUIThread.Execute(() =>
			{
				Log.Comment("\r\nItemsRepeater.GetOrCreateElement(" + index + ")");
				var item = itemsRepeater.GetOrCreateElement(index);

				// Make sure the newly created item is measured so that its size is
				// identified and can be used to bring it entirely into view.
				Log.Comment("UIElement.UpdateLayout()");
				item.UpdateLayout();

				Log.Comment("UIElement.StartBringIntoView()");
				item.StartBringIntoView();
			});

			await TestServices.WindowHelper.WaitForIdle();
		}

		private async Task<string> LogRealizedIndicesAsync(List<int> realizedItemsIndices, int ticks)
		{
			string strRealizedItemsIndices = null;

			do
			{
				RunOnUIThread.Execute(() =>
				{
					strRealizedItemsIndices = string.Empty;

					foreach (int i in realizedItemsIndices)
					{
						strRealizedItemsIndices += " " + i;
					}

					Log.Comment("Realized indices:" + strRealizedItemsIndices);
				});

				ticks--;
				// TODO Uno: Replace with CompositionPropertySpy.SynchronouslyTickUIThread when ported.
				await TestServices.WindowHelper.WaitForIdle();
			}
			while (ticks >= 0);

			return strRealizedItemsIndices;
		}

		[TestMethod]
#if __WASM__ || __APPLE_UIKIT__ || __ANDROID__ || __SKIA__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
		public async Task ValidateElementEvents()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(Enumerable.Range(0, 10).ToList()));

			var (repeater, _) = await SetupRepeater(dataSource);

			RunOnUIThread.Execute(() =>
			{
				List<int> preparedIndices = new List<int>();
				List<int> clearedIndices = new List<int>();
				List<KeyValuePair<int, int>> changedIndices = new List<KeyValuePair<int, int>>();

				repeater.ElementPrepared += (sender, args) =>
				{
					preparedIndices.Add(args.Index);
				};

				repeater.ElementClearing += (sender, args) =>
				{
					clearedIndices.Add(sender.GetElementIndex(args.Element));
				};

				repeater.ElementIndexChanged += (sender, args) =>
				{
					changedIndices.Add(new KeyValuePair<int, int>(args.OldIndex, args.NewIndex));
				};

				Log.Comment("Insert in realized range: Inserting 1 item at index 1");
				dataSource.Insert(index: 1, count: 1, reset: false);
				repeater.UpdateLayout();

				Verify.AreEqual(1, preparedIndices.Count);
				Verify.AreEqual(1, preparedIndices[0]);
				Verify.AreEqual(2, changedIndices.Count);
				Verify.IsTrue(changedIndices.Contains(new KeyValuePair<int, int>(1, 2)));
				Verify.IsTrue(changedIndices.Contains(new KeyValuePair<int, int>(2, 3)));
				Verify.AreEqual(1, clearedIndices.Count);
				Verify.AreEqual(3, clearedIndices[0]);

				preparedIndices.Clear();
				clearedIndices.Clear();
				changedIndices.Clear();

				Log.Comment("Remove in realized range: Removing 1 item at index 0");
				dataSource.Remove(index: 0, count: 1, reset: false);
				repeater.UpdateLayout();
				Verify.AreEqual(1, clearedIndices.Count);
				Verify.AreEqual(0, clearedIndices[0]);

				if (PlatformConfiguration.IsOsVersionGreaterThanOrEqual(OSVersion.Redstone5))
				{
					Verify.AreEqual(0, preparedIndices.Count);
				}
				else
				{
					Verify.AreEqual(1, preparedIndices.Count);
					Verify.AreEqual(2, preparedIndices[0]);
				}

				Verify.AreEqual(2, changedIndices.Count);
				Verify.IsTrue(changedIndices.Contains(new KeyValuePair<int, int>(1, 0)));
				Verify.IsTrue(changedIndices.Contains(new KeyValuePair<int, int>(2, 1)));
			});
		}

		// Validate new items get generated when existing ones are removed from the top.
		[TestMethod]
		[Ignore("UNO: Requires CompositionPropertySpy.SynchronouslyTickUIThread to drive vertical cache growth.")]
		public async Task ValidateElementCreationUponRemovals()
		{
			CustomItemsSource dataSource = null;
			List<int> preparedIndices = null;

			RunOnUIThread.Execute(() => dataSource = new CustomItemsSource(Enumerable.Range(0, 20).ToList()));

			var (itemsRepeater, _) = await SetupRepeater(dataSource, @"<TextBlock Text='{Binding}' Height='24'/>");

			RunOnUIThread.Execute(() =>
			{
				itemsRepeater.VerticalCacheLength = 2.0;
			});

			// The viewport size being 200px and realization window increase being 40px per measure pass,
			// make sure there are 2.5 x cacheLengths passes to land on the final realized items.
			// cacheLengths: number of viewports constituting the pre-fetch buffer, once completely built.
			//               Half of it is before the current viewport, and half after.
			// viewport: 200px
			// cacheIncrement: 40px (buffer increment per measure pass on each side)
			// Measure passes needed to fill the pre-fetch buffer: viewport / cacheIncrement / 2 x cacheLengths = 2.5 x cacheLengths
			Log.Comment("Letting the item cache grow to the VerticalCacheLength value.");
			// TODO Uno: Replace with CompositionPropertySpy.SynchronouslyTickUIThread(6) when ported.
			for (int i = 0; i < 6; i++)
			{
				await TestServices.WindowHelper.WaitForIdle();
			}

			RunOnUIThread.Execute(() =>
			{
				preparedIndices = new List<int>();

				itemsRepeater.ElementPrepared += (sender, args) =>
				{
					Log.Comment("ElementPrepared - Index={0}.", args.Index);
					preparedIndices.Add(args.Index);
				};
			});

			for (int iteration = 1; iteration <= 16; iteration++)
			{
				RunOnUIThread.Execute(() =>
				{
					Log.Comment("Removing top item at index 0.");
					dataSource.Remove(index: 0, count: 1, reset: false);
				});

				await TestServices.WindowHelper.WaitForIdle();
			}

			RunOnUIThread.Execute(() =>
			{
				// Removal of top items is expected to cause new item generation.
				Log.Comment("Final preparedIndices.Count={0}.", preparedIndices.Count);
				Verify.IsGreaterThan(preparedIndices.Count, 0);
			});
		}

		[TestMethod]
#if __WASM__ || __APPLE_UIKIT__ || __ANDROID__ || __SKIA__
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
#endif
		public async Task ValidateElementIndexChangedEventOnStableReset()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new CustomItemsSourceWithUniqueId(Enumerable.Range(0, 10).ToList()));

			var (repeater, _) = await SetupRepeater(dataSource);

			RunOnUIThread.Execute(() =>
			{
				List<int> preparedIndices = new List<int>();
				List<int> clearedIndices = new List<int>();
				List<KeyValuePair<int, int>> changedIndices = new List<KeyValuePair<int, int>>();

				repeater.ElementPrepared += (sender, args) =>
				{
					preparedIndices.Add(args.Index);
				};

				repeater.ElementClearing += (sender, args) =>
				{
					clearedIndices.Add(sender.GetElementIndex(args.Element));
				};

				repeater.ElementIndexChanged += (sender, args) =>
				{
					changedIndices.Add(new KeyValuePair<int, int>(args.OldIndex, args.NewIndex));
				};

				Log.Comment("(UniqueId Reset) Insert in realized range: Inserting 1 item at index 1");
				dataSource.Insert(index: 1, count: 1, reset: true, valueStart: 2000);
				repeater.UpdateLayout();

				Verify.AreEqual(1, preparedIndices.Count);
				Verify.AreEqual(1, preparedIndices[0]);
				Verify.AreEqual(1, changedIndices.Count);
				Verify.IsTrue(changedIndices.Contains(new KeyValuePair<int, int>(1, 2)));
				foreach (var ch in changedIndices)
				{
					Log.Comment("Changed " + ch.Key + " " + ch.Value);
				}
				Verify.AreEqual(1, clearedIndices.Count);
				Verify.AreEqual(2, clearedIndices[0]);

				preparedIndices.Clear();
				clearedIndices.Clear();
				changedIndices.Clear();

				Log.Comment("(UniqueId Reset) Remove in realized range: Removing 1 item at index 0");
				dataSource.Remove(index: 0, count: 1, reset: true);
				repeater.UpdateLayout();
				Verify.AreEqual(1, clearedIndices.Count);
				Verify.AreEqual(0, clearedIndices[0]);
				foreach (var ch in changedIndices)
				{
					Log.Comment("Changed " + ch.Key + " " + ch.Value);
				}
				Verify.AreEqual(1, preparedIndices.Count);
				Verify.AreEqual(2, preparedIndices[0]);
				Verify.AreEqual(2, changedIndices.Count);
				Verify.IsTrue(changedIndices.Contains(new KeyValuePair<int, int>(1, 0)));
				Verify.IsTrue(changedIndices.Contains(new KeyValuePair<int, int>(2, 1)));
			});
		}

		[TestMethod]
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
		public async Task ValidateGetElementAtCachingForLayout()
		{
			List<int> data = Enumerable.Range(0, 15).ToList();
			ItemsSourceView dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new InspectingDataSource(data));
			var (repeater, scrollViewer) = await SetupRepeater(dataSource, layout: null);
			bool layoutRan = false;

			RunOnUIThread.Execute(() =>
			{
				var layout = new MockVirtualizingLayout();
				layout.MeasureLayoutFunc = (availableSize, context) =>
				{
					Verify.AreEqual(15, context.ItemCount);
					var element0 = context.GetOrCreateElementAt(0);
					Verify.IsNotNull(element0);
					var element1 = context.GetOrCreateElementAt(1);
					Verify.IsNotNull(element1);
					Verify.AreNotSame(element0, element1);

					var element1again = context.GetOrCreateElementAt(1);
					Verify.AreSame(element1, element1again);

					var element10 = context.GetOrCreateElementAt(10);
					Verify.IsNotNull(element10);
					Verify.AreNotSame(element0, element10);
					Verify.AreNotSame(element1, element10);

					context.RecycleElement(element1);

					var element0New = context.GetOrCreateElementAt(0);
					Verify.AreSame(element0, element0New);

					context.RecycleElement(element10);
					context.RecycleElement(element0);

					layoutRan = true;
					return new Size(10, 10);
				};

				repeater.Layout = layout;
				repeater.UpdateLayout();
				Verify.IsTrue(layoutRan);
			});

			await TestServices.WindowHelper.WaitForIdle();
		}

		// Validate that when we clear containers during panning in flow layouts, we always do it 
		// from the end of the range inwards. This allows us to better track first/last realized indices in 
		// ViewManager. Breaking this can cause a performance regression.
		[TestMethod]
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
		public async Task ValidateElementClearingOrderFromFlowLayout()
		{
			ItemsSourceView dataSource = null;
			RunOnUIThread.Execute(() => dataSource = new InspectingDataSource(Enumerable.Range(0, 15).ToList()));
			var (repeater, scrollViewer) = await SetupRepeater(dataSource, layout: null);
			List<int> clearedIndices = new List<int>();
			var viewChangedEvent = new UnoManualResetEvent(false);

			RunOnUIThread.Execute(() =>
			{
				scrollViewer.ViewChanged += (sender, args) =>
				{
					if (!args.IsIntermediate)
					{
						viewChangedEvent.Set();
					}
				};

				repeater.Layout = new StackLayout();
				repeater.ElementPrepared += (sender, args) =>
				{
					((FrameworkElement)args.Element).Height = 20;
				};
				repeater.UpdateLayout();

				repeater.ElementClearing += (sender, args) =>
				{
					int index = repeater.GetElementIndex(args.Element);
					Log.Comment("Clearing.." + index);
					clearedIndices.Add(index);
				};

				scrollViewer.ChangeView(null, 100.0, null, disableAnimation: true);
			});

			Verify.IsTrue(await viewChangedEvent.WaitOne(DefaultWaitTime), "Waiting for ViewChanged.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				// Validate order is 0, 1, 2, 3
				for (int i = 0; i < 4; i++)
				{
					Verify.AreEqual(i, clearedIndices[i]);
				}

				clearedIndices.Clear();
				viewChangedEvent.Reset();
				scrollViewer.ChangeView(null, 0.0, null, disableAnimation: true);
			});

			Verify.IsTrue(await viewChangedEvent.WaitOne(DefaultWaitTime), "Waiting for ViewChanged.");
			await TestServices.WindowHelper.WaitForIdle();

			RunOnUIThread.Execute(() =>
			{
				// Validate order is backwards 14, 13, 12, 11
				for (int i = 0; i < 4; i++)
				{
					Verify.AreEqual(14 - i, clearedIndices[i]);
				}
			});
		}

		[TestMethod]
		[Ignore("UNO: Test does not pass yet with Uno https://github.com/unoplatform/uno/issues/4529")]
		public async Task CanResetLayoutAfterUniqueIdReset()
		{
			var data = new WinRTCollection(Enumerable.Range(0, 2).Select(i => string.Format("Item #{0}", i)));
			object dataSource = null;
			RunOnUIThread.Execute(() => dataSource = MockItemsSource.CreateDataSource(data, supportsUniqueIds: true));
			var (repeater, _) = await SetupRepeater(dataSource);

			RunOnUIThread.Execute(() =>
			{
				var range = new UIElement[] { repeater.TryGetElement(0), repeater.TryGetElement(1) };
				var clearedElements = new List<UIElement>();

				repeater.ElementClearing += (s, e) =>
				{
					clearedElements.Add(e.Element);
				};

				// The realized elements will be sent to the unique id reset pool.
				// They haven't been cleared yet.
				data.Reset();
				Verify.AreEqual(0, clearedElements.Count);

				// This also cause elements to be sent to the unique id reset pool.
				// We are validating here that we are smart enough not send them there twice.
				// Doing so will cause an exception to be thrown.
				repeater.Layout = null;
				Verify.AreEqual(0, clearedElements.Count);

				repeater.UpdateLayout();

				// Layout runs. The elements in the reset pool are not used.
				// They should be cleared back to the view generator at this point.
				Verify.AreEqual(2, clearedElements.Count);
				Verify.AreEqual(range[0], clearedElements[0]);
				Verify.AreEqual(range[1], clearedElements[1]);
				Verify.IsNull(repeater.TryGetElement(0));
				Verify.IsNull(repeater.TryGetElement(1));
			});
		}

		[TestMethod]
		public void ValidateDataContextDoesNotGetOverwritten()
		{
			const string c_element1DataContext = "Element1_DataContext";

			RunOnUIThread.Execute(() =>
			{
				var data = new List<Button>()
				{
					new Button()
					{
							Content = "Element1_Content",
							DataContext = c_element1DataContext
					}
				};

				var elementFactory = new DataAsElementElementFactory();

				var repeater = new ItemsRepeater()
				{
					ItemsSource = data,
					ItemTemplate = elementFactory
				};

				Content = repeater;

				Content.UpdateLayout();

				// Verify that DataContext is still the same
				var firstElement = repeater.TryGetElement(0) as Button;
				var retrievedDataContextItem1 = firstElement.DataContext as string;
				Verify.IsTrue(retrievedDataContextItem1 == c_element1DataContext);

			});
		}

		[TestMethod]
		public void ValidateDataContextGetsPropagated()
		{
			const string c_element1DataContext = "Element1_DataContext";

			RunOnUIThread.Execute(() =>
			{
				var data = new List<Button>()
				{
					new Button()
					{
							Content = "Element1_Content",
							DataContext = c_element1DataContext
					}
				};

				var elementFactory = new ElementFromElementElementFactory();

				var repeater = new ItemsRepeater()
				{
					ItemsSource = data,
					ItemTemplate = elementFactory
				};

				Content = repeater;

				Content.UpdateLayout();

				// Verify that DataContext of data has propagated to the container
				var firstElement = repeater.TryGetElement(0) as Button;
				var retrievedDataContextItem1 = firstElement.DataContext as string;
				Verify.IsTrue(data[0] == firstElement.Content);
				Verify.IsTrue(retrievedDataContextItem1 == c_element1DataContext);

			});
		}

		[TestMethod] // Issue 1018
		[Ignore]
		public async Task ValidateFocusMoveOnElementCleared()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() =>
			{
				dataSource = new CustomItemsSource(Enumerable.Range(0, 5).ToList());
			});

			var (repeater, _) = await SetupRepeater(dataSource, "<Button Content='{Binding}' Height='10' />");

			// dataSource: 0 1 2 3 4 
			// Index 0 deleted, focus should be on the new element which has index 0
			await SharedHelpers.RunActionsWithWait(
				new Action[]
				{
					() => { MoveFocusToIndex(repeater, 0); },
					() => { dataSource.Remove(0 /* index */, 1 /* count */, false /* reset*/); },
					() => { ValidateCurrentFocus(repeater, 0 /*expectedIndex */, "1" /* expectedContent */); }
				});

			// dataSource: 1 2 3 4 
			// Last element deleted, focus should move to the previous element
			int lastIndex = dataSource.Inner.Count - 1;
			await SharedHelpers.RunActionsWithWait(
				new Action[]
				{
					() => { MoveFocusToIndex(repeater, lastIndex); },
					() => { dataSource.Remove(lastIndex /* index */, 1 /* count */, false /* reset*/); },
					() => { ValidateCurrentFocus(repeater, 2 /*expectedIndex */, "3" /* expectedContent */); }
				});

			// dataSource: 1 2 3 
			// Remove multiple elements
			await SharedHelpers.RunActionsWithWait(
				new Action[]
				{
					() => { MoveFocusToIndex(repeater, 0); },
					() => { dataSource.Remove(0 /* index */, 2 /* count */, false /* reset*/); },
					() => { ValidateCurrentFocus(repeater, 0 /*expectedIndex */, "3" /* expectedContent */); }
				});
		}

		[TestMethod] // Issue 1018
		[Ignore]
		public async Task ValidateFocusMoveOnElementClearedWithUniqueIds()
		{
			CustomItemsSource dataSource = null;
			RunOnUIThread.Execute(() =>
			{
				dataSource = new CustomItemsSourceWithUniqueId(Enumerable.Range(0, 5).ToList());
			});

			var (repeater, _) = await SetupRepeater(dataSource, "<Button Content='{Binding}' Height='10' />");

			// dataSource: 0 1 2 3 4 
			// Index 0 deleted, focus should be on the new element which has index 0
			await SharedHelpers.RunActionsWithWait(
				new Action[]
				{
					() => { MoveFocusToIndex(repeater, 0); },
					() => { dataSource.Remove(0 /* index */, 1 /* count */, true /* reset*/); },
					() => { ValidateCurrentFocus(repeater, 0 /*expectedIndex */, "1" /* expectedContent */); }
				});

			// dataSource: 1 2 3 4 
			// Last element deleted, focus should move to the previous element
			int lastIndex = dataSource.Inner.Count - 1;
			await SharedHelpers.RunActionsWithWait(
				new Action[]
				{
					() => { MoveFocusToIndex(repeater, lastIndex); },
					() => { dataSource.Remove(lastIndex /* index */, 1 /* count */, true /* reset*/); },
					() => { ValidateCurrentFocus(repeater, 2 /*expectedIndex */, "3" /* expectedContent */); }
				});

			// dataSource: 1 2 3 
			// Reset should keep the focused element as long as the unique id matches.
			await SharedHelpers.RunActionsWithWait(
				new Action[]
				{
					() => { MoveFocusToIndex(repeater, 0); },
					() => { dataSource.Reset(); },
					() =>
					{
						int newIndex = dataSource.Inner.IndexOf(1);
						ValidateCurrentFocus(repeater, newIndex /*expectedIndex */, "1" /* expectedContent */);
					}
				});

			// dataSource: 1 2 3 
			// Remove multiple elements
			await SharedHelpers.RunActionsWithWait(
				new Action[]
				{
					() => { MoveFocusToIndex(repeater, 0); },
					() => { dataSource.Remove(0 /* index */, 2 /* count */, true /* reset*/); },
					() => { ValidateCurrentFocus(repeater, 0 /*expectedIndex */, "3" /* expectedContent */); }
				});
		}

		[TestMethod]
		// Why does this test work?
		// When the elements get created from the RecyclingElementFactory, we get already "existing" data templates.
		// However, the reason for the crash in #2384 is that those "empty" data templates actually still had their data context
		// If that data context is not null, that means it did not get cleared when the element was recycled, which is the wrong behavior.
		// To check if the clearing is working correctly, we are checking this inside the ElementFactory's RecycleElement function.
		public void ValidateElementClearingClearsDataContext()
		{
			ItemsRepeater repeater = null;
			MockElementFactory elementFactory = null;
			int elementClearingRaisedCount = 0;
			Log.Comment("Initialize ItemsRepeater");
			RunOnUIThread.Execute(() =>
			{
				elementFactory = new MockElementFactory()
				{
					GetElementFunc = delegate (int index, UIElement owner)
					{
						return new Button() { Content = index };
					},

					ClearElementFunc = delegate (UIElement element, UIElement owner)
					{
						elementClearingRaisedCount++;
						Verify.IsNull((element as FrameworkElement).DataContext);
					}
				};

				repeater = CreateRepeater(Enumerable.Range(0, 100),
					elementFactory);

				repeater.Layout = new StackLayout();

				Content = repeater;
				repeater.UpdateLayout();

				repeater.ItemsSource = null;

				Log.Comment("Verify ItemsRepeater cleared data contexts correctly");
				Verify.IsTrue(elementClearingRaisedCount > 0, "ItemsRepeater should have cleared some elements");
			});
		}


		private void MoveFocusToIndex(ItemsRepeater repeater, int index)
		{
			var element = repeater.TryGetElement(index) as Control;
			element.Focus(FocusState.Programmatic);
		}

		private void ValidateCurrentFocus(ItemsRepeater repeater, int expectedIndex, string expectedContent)
		{
			var currentFocus = FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot) as ContentControl;
			var currentFocusedIndex = repeater.GetElementIndex(currentFocus);
			Log.Comment("expectedIndex: " + expectedIndex + " actual : " + currentFocusedIndex);
			Verify.AreEqual(expectedIndex, currentFocusedIndex);
			Log.Comment("expectedContent: " + expectedContent + " actual : " + currentFocus.Content.ToString());
			Verify.AreEqual(expectedContent, currentFocus.Content.ToString());
		}

		private ItemsRepeater CreateRepeater(object dataSource, object elementFactory)
		{
			var repeater = new ItemsRepeater
			{
				ItemsSource = dataSource,
				ItemTemplate = elementFactory
			};
			repeater.Layout = CreateLayout(repeater);
			return repeater;
		}

		private VirtualizingLayout CreateLayout(ItemsRepeater repeater)
		{
			var layout = new MockVirtualizingLayout();
			var children = new List<UIElement>();

			layout.MeasureLayoutFunc = (availableSize, context) =>
			{
				repeater.Tag = repeater.Tag ?? context;
				children.Clear();
				var itemCount = context.ItemCount;

				for (int i = 0; i < itemCount; ++i)
				{
					var element = context.GetOrCreateElementAt(i);
					element.Measure(availableSize);
					children.Add(element);
				}

				return new Size(10, 10);
			};

			return layout;
		}

		private async Task<(ItemsRepeater, ScrollViewer)> SetupRepeater(object dataSource, string itemContent = @"<Button Content='{Binding}' Height='100' />")
		{
			VirtualizingLayout layout = null;
			RunOnUIThread.Execute(() => layout = new StackLayout());
			return await SetupRepeater(dataSource, layout, itemContent);
		}

		private async Task<(ItemsRepeater, ScrollViewer)> SetupRepeater(object dataSource, VirtualizingLayout layout)
		{
			return await SetupRepeater(dataSource, layout, @"<Button Content='{Binding}' Height='100' />");
		}

		private async Task<(ItemsRepeater, ScrollViewer)> SetupRepeater(object dataSource, VirtualizingLayout layout, string itemContent)
		{
			ItemsRepeater repeater = null;
			ScrollViewer sv = null;
			RunOnUIThread.Execute(() =>
			{
				var elementFactory = new RecyclingElementFactory();
				elementFactory.RecyclePool = new RecyclePool();
				elementFactory.Templates["Item"] = (DataTemplate)XamlReader.Load(
					@"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'> " + itemContent + @"</DataTemplate>");

				repeater = new ItemsRepeater()
				{
					ItemsSource = dataSource,
					ItemTemplate = elementFactory,
					Layout = layout,
					HorizontalCacheLength = 0.0,
					VerticalCacheLength = 0.0
				};

				sv = new ScrollViewer
				{
					Content = repeater
				};

				Content = new ItemsRepeaterScrollHost()
				{
					Width = 200,
					Height = 200,
					ScrollViewer = sv
				};
			});

			await TestServices.WindowHelper.WaitForIdle();
			return (repeater, sv);
		}

		private int DefaultWaitTime = 2000;
	}
}
