// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\Repeater\APITests\RepeaterFocusTests.cs, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

using Common;
using MUXControlsTestApp.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using System;

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
using ItemsRepeaterScrollHost = Microsoft/* UWP don't rename */.UI.Xaml.Controls.ItemsRepeaterScrollHost;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests;
using System.Threading.Tasks;
using Private.Infrastructure;

namespace Windows.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	[RequiresFullWindow]
	public class RepeaterFocusTests : MUXApiTestBase
	{
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		[TestMethod]
		public async Task ValidateTabNavigation()
		{
			ItemsRepeater repeater = null;
			ScrollViewer scrollViewer = null;
			var data = new ObservableCollection<string>(Enumerable.Range(0, 50).Select(i => "Item #" + i));
			var viewChangedEvent = new UnoAutoResetEvent(false);

			await RunOnUIThread.ExecuteAsync(() =>
			{
				var itemTemplate = (DataTemplate)XamlReader.Load(
						@"<DataTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                            <Button Content='{Binding}' Height='100' />
                        </DataTemplate>");

				var elementFactory = new RecyclingElementFactory()
				{
					RecyclePool = new RecyclePool(),
					Templates =
					{
						{ "itemTemplate", itemTemplate },
					}
				};

				Content = CreateAndInitializeRepeater
				(
				   data,
				   new StackLayout(),
				   elementFactory,
				   ref repeater,
				   ref scrollViewer
				);

				scrollViewer.ViewChanged += (s, e) =>
				{
					if (!e.IsIntermediate)
					{
						viewChangedEvent.Set();
					}
				};

				repeater.TabFocusNavigation = KeyboardNavigationMode.Local;
				Content.UpdateLayout();
				ValidateTabNavigationOrder(repeater);
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Move window - disconnected
				scrollViewer.ChangeView(null, 2000, null, true);
			});

			Verify.IsTrue(await viewChangedEvent.WaitOne(DefaultWaitTimeInMS), "Waiting for final ViewChanged event.");
			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ValidateTabNavigationOrder(repeater);

				// Delete a couple of elements and validate we
				// ignore unrealized elements.
				// First visible element is expected to be index 20.
				while (data.Count > 21) { data.RemoveAt(21); }

				repeater.UpdateLayout();
				ValidateTabNavigationOrder(repeater);
			});
		}

		private static void ValidateTabNavigationOrder(ItemsRepeater repeater)
		{
			var expectedSequence = GetRealizedRange(repeater);

			expectedSequence.Last().Focus(FocusState.Keyboard);
			Log.Comment("\n\nStart point: " + expectedSequence.Last().Content);

			Log.Comment("Validating forward tab navigation...");

			FindNextElementOptions options = new FindNextElementOptions
			{
				SearchRoot = repeater.XamlRoot.Content
			};

			foreach (var expectedElement in expectedSequence)
			{
				var actualElement = FocusManager.FindNextElement(FocusNavigationDirection.Next, options);
				var actualElementAsbutton = FocusManager.FindNextElement(FocusNavigationDirection.Next, options) as Button;
				var actualElementAsToggleButton = FocusManager.FindNextElement(FocusNavigationDirection.Next, options) as ToggleButton;

				string content = actualElementAsbutton != null ? (string)actualElementAsbutton.Content : (string)actualElementAsToggleButton.Content;
				// We need to ignore the toggle theme button, so lets set its tabstop to false and get next element.
				if (content == "Toggle theme")
				{
					actualElementAsbutton.IsTabStop = false;
					actualElement = FocusManager.FindNextElement(FocusNavigationDirection.Next, options);
				}
				actualElementAsbutton = FocusManager.FindNextElement(FocusNavigationDirection.Next, options) as Button;
				actualElementAsToggleButton = FocusManager.FindNextElement(FocusNavigationDirection.Next, options) as ToggleButton;

				content = actualElementAsbutton != null ? (string)actualElementAsbutton.Content : (string)actualElementAsToggleButton.Content;
				// We need to ignore the lab dimensions button, so lets set its tabstop to false and get next element.
				if (content == "Render innerframe in lab dimensions")
				{
					actualElementAsToggleButton.IsTabStop = false;
					actualElement = FocusManager.FindNextElement(FocusNavigationDirection.Next, options);
				}
				Log.Comment("Expected: " + expectedElement.Content);
				Log.Comment("Actual: " + content);
				Verify.AreEqual(expectedElement, actualElement);
				expectedElement.Focus(FocusState.Keyboard);
			}

			Log.Comment("Validating backward tab navigation...");

			foreach (var expectedElement in expectedSequence.Reverse().Skip(1))
			{
				var actualElement = (Button)FocusManager.FindNextElement(FocusNavigationDirection.Previous, options);
				Log.Comment("Expected: " + expectedElement.Content);
				Log.Comment("Actual: " + actualElement.Content);
				Verify.AreEqual(expectedElement, actualElement);
				expectedElement.Focus(FocusState.Keyboard);
			}
		}

		private static Button[] GetRealizedRange(ItemsRepeater repeater)
		{
			return Enumerable.Range(0, VisualTreeHelper.GetChildrenCount(repeater))
				.Select(i => (Button)VisualTreeHelper.GetChild(repeater, i))
				.Where(e => repeater.GetElementIndex(e) >= 0)
				.OrderBy(e => repeater.GetElementIndex(e))
				.ToArray();
		}

		private static ItemsRepeaterScrollHost CreateAndInitializeRepeater(
			object itemsSource,
			VirtualizingLayout layout,
			ElementFactory elementFactory,
			ref ItemsRepeater repeater,
			ref ScrollViewer scrollViewer)
		{
			repeater = new ItemsRepeater()
			{
				ItemsSource = itemsSource,
				Layout = layout,
				ItemTemplate = elementFactory,
				VerticalCacheLength = 0
			};

			scrollViewer = new ScrollViewer()
			{
				Content = repeater
			};

			return new ItemsRepeaterScrollHost()
			{
				Width = 400,
				Height = 400,
				ScrollViewer = scrollViewer
			};
		}
	}
}
