// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Tests.Common;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Controls;

[TestClass]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class ItemsControlIntegrationTests : BaseDxamlTestClass
{
	private const int s_itemCount = 5;

	[ClassInitialize]
	public static void ClassSetup()
	{
		CommonTestSetupHelper.CommonTestClassSetup();
	}

	[ClassCleanup]
	public static void TestCleanup()
	{
		WindowHelper.VerifyTestCleanup();
	}

	//
	// Test Cases
	//
	[TestMethod]
	[RunsOnUIThread]
	public void CanInstantiate()
	{
		var act = () => new ItemsControl();
		act.Should().NotThrow();
	}

	[TestMethod]
	public async Task CanEnterAndLeaveLiveTree()
	{
		var loadedEvent = new Event();
		var unloadedEvent = new Event();

		await RunOnUIThread(() =>
		{
			var itemsControl = new ItemsControl();
			itemsControl.Loaded += (s, e) => loadedEvent.Set();
			itemsControl.Unloaded += (s, e) => unloadedEvent.Set();
			WindowHelper.WindowContent = itemsControl;
		});
		await loadedEvent.WaitForDefault();

		await RunOnUIThread(() => WindowHelper.WindowContent = null);
		await unloadedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task CanAddItemsDirectly()
	{
		ItemsControl itemsControl = await ItemsControlHelper.AddItemsControl<ItemsControl>(true /* addItemsDirectly */, s_itemCount);
		await ItemsControlHelper.VerifyContainersAndItems<ItemsControl, ContentPresenter>(itemsControl, s_itemCount);
	}

	[TestMethod]
	public async Task CanAddItemsFromSource()
	{
		ItemsControl itemsControl = await ItemsControlHelper.AddItemsControl<ItemsControl>(false /* addItemsDirectly */, s_itemCount);
		await ItemsControlHelper.VerifyContainersAndItems<ItemsControl, ContentPresenter>(itemsControl, s_itemCount);
	}

	[TestMethod]
	public async Task CanGetContainersFromItems()
	{
		var itemsControl = await ItemsControlHelper.AddItemsControl<ItemsControl>(false /* addItemsDirectly */, s_itemCount);
		var containersVector = await ItemsControlHelper.GetContainersFromVisualTree(itemsControl);

		await RunOnUIThread(() =>
		{
			for (int i = 0; i < containersVector.Count; i++)
			{
				var itemString = "Item " + i;

				VERIFY_ARE_EQUAL(containersVector[i], itemsControl.ContainerFromItem(itemString));
			}
		});
	}

	[TestMethod]
	public async Task CanGetContainersFromIndexes()
	{
		var itemsControl = await ItemsControlHelper.AddItemsControl<ItemsControl>(false /* addItemsDirectly */, s_itemCount);
		var containersVector = await ItemsControlHelper.GetContainersFromVisualTree(itemsControl);

		await RunOnUIThread(() =>
		{
			for (int i = 0; i < containersVector.Count; i++)
			{
				VERIFY_ARE_EQUAL(containersVector[i], itemsControl.ContainerFromIndex(i));
			}
		});
	}

	[TestMethod]
	public async Task CanGetItemsFromContainers()
	{
		var itemsControl = await ItemsControlHelper.AddItemsControl<ItemsControl>(false /* addItemsDirectly */, s_itemCount);
		var containersVector = await ItemsControlHelper.GetContainersFromVisualTree(itemsControl);

		await RunOnUIThread(() =>
		{
			for (int i = 0; i < containersVector.Count; i++)
			{
				string expectedItemString = "Item " + i;

				string actualItemString = itemsControl.ItemFromContainer(containersVector[i]) as string;

				VERIFY_IS_NOT_NULL(actualItemString);
				VERIFY_ARE_EQUAL(actualItemString, expectedItemString);
			}
		});
	}

	[TestMethod]
	public async Task CanGetIndexesFromContainers()
	{
		var itemsControl = await ItemsControlHelper.AddItemsControl<ItemsControl>(false /* addItemsDirectly */, s_itemCount);
		var containersVector = await ItemsControlHelper.GetContainersFromVisualTree(itemsControl);

		await RunOnUIThread(() =>
		{
			for (int i = 0; i < containersVector.Count; i++)
			{
				VERIFY_ARE_EQUAL(itemsControl.IndexFromContainer(containersVector[i]), i);
			}
		});
	}

	[TestMethod]
	public async Task CanLayoutWithoutItemContainerGenerator()
	{
		var loadedEvent = new Event();
		var loadedRegistration = CreateSafeEventRegistration<ScrollViewer, RoutedEventHandler>("Loaded");

		ScrollViewer scrollViewer = null;

		await RunOnUIThread(() =>
		{
			StackPanel rootPanel = new StackPanel();
			scrollViewer = new ScrollViewer();
			rootPanel.Children.Add(scrollViewer);

			loadedRegistration.Attach(scrollViewer, (s, e) =>
			{
				LOG_OUTPUT("ScrollViewer.Loaded event.");
				loadedEvent.Set();
			});

			WindowHelper.WindowContent = rootPanel;
		});

		LOG_OUTPUT("Waiting for ScrollViewer.Loaded event.");
		await loadedEvent.WaitForDefault();
		await WindowHelper.WaitForIdle();

		await RunOnUIThread(() =>
		{
			GridView gridView = new GridView();
			scrollViewer.Content = gridView;
			scrollViewer.UpdateLayout();
			gridView.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load(
				"""
				<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
				    <WrapGrid/>
				</ItemsPanelTemplate>
				""");
		});

		await WindowHelper.WaitForIdle();
	}
}
