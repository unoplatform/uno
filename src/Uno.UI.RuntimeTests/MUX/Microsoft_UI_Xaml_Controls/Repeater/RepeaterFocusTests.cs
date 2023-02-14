// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RepeaterFocusTests.cs, commit 93d6555

using Common;
using MUXControlsTestApp.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System;

#if USING_TAEF
using WEX.TestExecution;
using WEX.TestExecution.Markup;
using WEX.Logging.Interop;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
#endif

using VirtualizingLayout = Microsoft.UI.Xaml.Controls.VirtualizingLayout;
using ItemsRepeater = Microsoft.UI.Xaml.Controls.ItemsRepeater;
using ElementFactory = Microsoft.UI.Xaml.Controls.ElementFactory;
using RecyclingElementFactory = Microsoft.UI.Xaml.Controls.RecyclingElementFactory;
using RecyclePool = Microsoft.UI.Xaml.Controls.RecyclePool;
using StackLayout = Microsoft.UI.Xaml.Controls.StackLayout;
using ItemsRepeaterScrollHost = Microsoft.UI.Xaml.Controls.ItemsRepeaterScrollHost;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests;
using System.Threading.Tasks;
using Private.Infrastructure;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests.RepeaterTests
{
	[TestClass]
	[RequiresFullWindow]
	public class RepeaterFocusTests : MUXApiTestBase
	{
		[TestMethod]
		public async Task ValidateTabNavigation()
		{
			if (!PlatformConfiguration.IsOsVersionGreaterThan(OSVersion.Redstone2))
			{
				Log.Warning("Test is disabled on anything lower than RS3 because the GetChildrenInTabFocusOrder API is not available on previous versions.");
				return;
			}

			ItemsRepeater repeater = null;
			ScrollViewer scrollViewer = null;
			var data = new ObservableCollection<string>(Enumerable.Range(0, 50).Select(i => "Item #" + i));
			var viewChangedEvent = new AutoResetEvent(false);

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
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ValidateTabNavigationOrder(repeater);
			});

			IdleSynchronizer.Wait();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				// Move window - disconnected
				scrollViewer.ChangeView(null, 2000, null, true);
			});

			Verify.IsTrue(viewChangedEvent.WaitOne(DefaultWaitTimeInMS), "Waiting for final ViewChanged event.");
			IdleSynchronizer.Wait();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ValidateTabNavigationOrder(repeater);

				// Delete a couple of elements and validate we
				// ignore unrealized elements.
				// First visible element is expected to be index 20.
				while (data.Count > 21) { data.RemoveAt(21); }

				repeater.UpdateLayout();
			});

			await TestServices.WindowHelper.WaitForIdle();

			await RunOnUIThread.ExecuteAsync(() =>
			{
				ValidateTabNavigationOrder(repeater);
			});
		}

		private static void ValidateTabNavigationOrder(ItemsRepeater repeater)
		{
			var expectedSequence = GetRealizedRange(repeater);

			expectedSequence.Last().Focus(FocusState.Keyboard);
			Log.Comment("\n\nStart point: " + expectedSequence.Last().Content);

			Log.Comment("Validating forward tab navigation...");

			foreach (var expectedElement in expectedSequence)
			{
				var actualElement = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next);
				var actualElementAsbutton = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next) as Button;
				var actualElementAsToggleButton = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next) as ToggleButton;

				string content = actualElementAsbutton != null ? (string)actualElementAsbutton.Content : (string)actualElementAsToggleButton.Content;
				// We need to ignore the toggle theme button, so lets set its tabstop to false and get next element.
				if (content == "Toggle theme")
				{
					actualElementAsbutton.IsTabStop = false;
					actualElement = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next);
				}
				actualElementAsbutton = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next) as Button;
				actualElementAsToggleButton = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next) as ToggleButton;

				content = actualElementAsbutton != null ? (string)actualElementAsbutton.Content : (string)actualElementAsToggleButton.Content;
				// We need to ignore the lab dimensions button, so lets set its tabstop to false and get next element.
				if (content == "Render innerframe in lab dimensions")
				{
					actualElementAsToggleButton.IsTabStop = false;
					actualElement = FocusManager.FindNextFocusableElement(FocusNavigationDirection.Next);
				}
				Log.Comment("Expected: " + expectedElement.Content);
				Log.Comment("Actual: " + content);
				Verify.AreEqual(expectedElement, actualElement);
				expectedElement.Focus(FocusState.Keyboard);
			}

			Log.Comment("Validating backward tab navigation...");

			foreach (var expectedElement in expectedSequence.Reverse().Skip(1))
			{
				var actualElement = (Button)FocusManager.FindNextFocusableElement(FocusNavigationDirection.Previous);
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
