// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Uno.UI.RuntimeTests.MUX.Helpers;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Common;

internal static class ItemsControlHelper
{
	public static async Task<TItemsControl> AddItemsControl<TItemsControl>(bool addItemsDirectly, int numItemsToAdd)
		where TItemsControl : ItemsControl, new()
	{
		TItemsControl itemsControl = null;
		var loadedRegistration = CreateSafeEventRegistration<ItemsControl, RoutedEventHandler>("Loaded");
		var loadedEvent = new Event();

		await RunOnUIThread(() =>
		{
			LOG_OUTPUT("Creating and adding the %s to the visual tree with %d items.", typeof(TItemsControl).Name, numItemsToAdd);

			itemsControl = new TItemsControl();
			itemsControl.Width = 200;
			itemsControl.Height = 200;

			if (addItemsDirectly)
			{
				for (int i = 0; i < numItemsToAdd; i++)
				{
					itemsControl.Items.Add("Item " + i);
				}
			}
			else
			{
				var itemsVector = new ObservableCollection<string>();

				for (int i = 0; i < numItemsToAdd; i++)
				{
					itemsVector.Add("Item " + i);
				}

				itemsControl.ItemsSource = itemsVector;
			}

			loadedRegistration.Attach(itemsControl, (sender, e) =>
			{
				loadedEvent.Set();
			});

			WindowHelper.WindowContent = itemsControl;
		});

		LOG_OUTPUT("Waiting for %s to be loaded...", typeof(TItemsControl).Name);
		await loadedEvent.WaitForDefault();
		LOG_OUTPUT("%s loaded.", typeof(TItemsControl).Name);
		await WindowHelper.WaitForIdle();

		return itemsControl;
	}

	// The ItemsSource must be an IList<TItemsClass>.
	public static async Task ScrollToIndex<TListItemsControl, TItemsClass>(TListItemsControl itemsControl, int index)
		where TListItemsControl : ListViewBase
	{
		ScrollViewer scrollViewer = null;
		var viewChangedRegistration = CreateSafeEventRegistration<ScrollViewer, EventHandler<ScrollViewerViewChangedEventArgs>>("ViewChanged");
		var viewChangedEvent = new Event();

		await RunOnUIThread(() =>
		{
			scrollViewer = TreeHelper.GetVisualChildByName(itemsControl, "ScrollViewer") as ScrollViewer;

			viewChangedRegistration.Attach(scrollViewer, (sender, args) =>
			{
				if (args.IsIntermediate == false)
				{
					viewChangedEvent.Set();
				}
			});

			LOG_OUTPUT("Scrolling to index %d.", index);

			var itemsVector = itemsControl.ItemsSource as IList<TItemsClass>;
			itemsControl.ScrollIntoView(itemsVector[index]);
		});

		LOG_OUTPUT("Waiting for scroll to complete...");
		await viewChangedEvent.WaitForDefault();
		LOG_OUTPUT("Done.");
	}

	public static async Task<List<DependencyObject>> GetContainersFromVisualTree<TItemsControl>(TItemsControl itemsControl, int expectedItemCount = 0)
		where TItemsControl : ItemsControl
	{
		var containersVector = new List<DependencyObject>();
		var containerPanel = await GetContainerPanelFromControl(itemsControl);

		await RunOnUIThread(() =>
		{
			if (VerifyContainerPanel<TItemsControl>(containerPanel) == false)
			{
				return;
			}

			int childCount = VisualTreeHelper.GetChildrenCount(containerPanel);

			if (expectedItemCount > 0 && childCount != expectedItemCount)
			{
				return;
			}

			for (int i = 0; i < childCount; i++)
			{
				containersVector.Add(VisualTreeHelper.GetChild(containerPanel, i));
			}
		});

		return containersVector;
	}

	// TExpectedContainerClass must be a ContentControl or a ContentPresenter.
	public static async Task VerifyContainersAndItems<TItemsControl, TExpectedContainerClass>(TItemsControl itemsControl, int expectedItemCount)
		where TItemsControl : ItemsControl
		where TExpectedContainerClass : FrameworkElement
	{
		var containersVector = await GetContainersFromVisualTree(itemsControl, expectedItemCount);

		await RunOnUIThread(() =>
		{
			for (int i = 0; i < containersVector.Count; i++)
			{
				var obj = containersVector[i];
				TExpectedContainerClass container = obj as TExpectedContainerClass;

				LOG_OUTPUT("Expected a %s child at index %d, received %s.", typeof(TExpectedContainerClass).Name, i, obj?.GetType().Name);
				VERIFY_IS_NOT_NULL(container);

				var expectedString = "Item " + i;

				LOG_OUTPUT("Got a %s at index %d. Verifying that its string content is '%s'.", typeof(TExpectedContainerClass).Name, i, expectedString);

				var actualString = GetContent(container) as string;

				if (actualString == null)
				{
					LOG_OUTPUT("Content was not a string.");
					VERIFY_FAIL();
				}
				else if (actualString != expectedString)
				{
					LOG_OUTPUT("Content was '%s' instead.", actualString);
					VERIFY_FAIL();
				}
				else
				{
					LOG_OUTPUT("Verified. String was '%s'.", expectedString);
				}
			}
		});
	}

	public static async Task<TPanel> GetPanelFromItemsControl<TPanel>(ItemsControl itemsControl)
		where TPanel : Panel
		=> await GetContainerPanelFromControl(itemsControl) as TPanel;

	private static object GetContent(FrameworkElement container)
		=> container switch
		{
			ContentPresenter presenter => presenter.Content,
			ContentControl control => control.Content,
			_ => null,
		};

	private static async Task<Panel> GetContainerPanelFromControl(ItemsControl itemsControl)
	{
		Panel containerPanel = null;

		await RunOnUIThread(() =>
		{
			var itemsPresenter = TreeHelper.GetVisualChildByType<ItemsPresenter>(itemsControl);

			if (itemsPresenter == null)
			{
				return;
			}

			int childCount = VisualTreeHelper.GetChildrenCount(itemsPresenter);

			if (childCount != 3)
			{
				return;
			}

			var nextObject = VisualTreeHelper.GetChild(itemsPresenter, 1);

			if (nextObject is not Panel panel)
			{
				return;
			}

			containerPanel = panel;
		});

		return containerPanel;
	}

	// WinUI 3 has no ListBox, so only the ItemsControl specialization exists here.
	private static bool VerifyContainerPanel<TItemsControl>(Panel containerPanel)
	{
		if (typeof(TItemsControl) != typeof(ItemsControl))
		{
			throw new NotSupportedException($"No container panel is expected for {typeof(TItemsControl).Name}.");
		}

		if (containerPanel is not StackPanel)
		{
			LOG_OUTPUT("Expected a StackPanel container panel, received %s.", containerPanel?.GetType().Name ?? "null");
			VERIFY_FAIL();

			return false;
		}

		return true;
	}

	private static void VERIFY_FAIL() => Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail();
}
