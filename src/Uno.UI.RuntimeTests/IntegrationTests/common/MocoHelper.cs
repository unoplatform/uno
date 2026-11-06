// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Tests.Enterprise;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.Common;

internal static class MocoHelper
{
	public enum ListControlType
	{
		ListView,
		GridView
	}

	public enum DataCollectionType
	{
		Flat,
		Grouped
	}

	private static string ListViewBaseXaml(string controlName) =>
		"""
		<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='root'>
		    <Grid.Resources>
		      <CollectionViewSource x:Name='cvs'/>
		    </Grid.Resources>
		    <CONTROLNAME x:Name='list' Height='500' Width='500' ItemsSource='{Binding Source={StaticResource cvs}}'>
		        <CONTROLNAME.ItemTemplate>
		            <DataTemplate>
		                <Grid Background='Orange' Height='150' Width='150'>
		                    <StackPanel Orientation='Vertical'>
		                        <TextBlock Text='{Binding}' />
		                    </StackPanel>
		                </Grid>
		            </DataTemplate>
		        </CONTROLNAME.ItemTemplate>
		        <CONTROLNAME.GroupStyle>
		          <GroupStyle>
		            <GroupStyle.Panel>
		                <ItemsPanelTemplate>
		                    <VariableSizedWrapGrid Orientation='Horizontal'/>
		                </ItemsPanelTemplate>
		            </GroupStyle.Panel>
		            <GroupStyle.HeaderTemplate>
		                <DataTemplate>
		                    <Grid Background='Blue'>
		                        <TextBlock Text='{Binding}' Foreground='White' FontSize='17' />
		                    </Grid>
		                </DataTemplate>
		            </GroupStyle.HeaderTemplate>
		            <GroupStyle.ContainerStyle>
		                <Style TargetType='GroupItem'>
		                    <Setter Property='BorderBrush' Value='White' />
		                    <Setter Property='BorderThickness' Value='3' />
		                </Style>
		            </GroupStyle.ContainerStyle>
		        </GroupStyle>
		    </CONTROLNAME.GroupStyle>
		  </CONTROLNAME>
		</Grid>
		""".Replace("CONTROLNAME", controlName);

	private static ListViewBase CreateList(ListControlType listType)
		=> listType == ListControlType.ListView ? new ListView() : new GridView();

	public static async Task<ListViewBase> SetUpBasicEnvironment(ListControlType listType, int itemsCount, int width = 500, int height = 500)
	{
		ListViewBase list = null;
		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			list = CreateList(listType);
			rootPanel.Children.Add(list);
			WindowHelper.WindowContent = rootPanel;
			list.Height = height;
			list.Width = width;
			PopulateList(list, itemsCount);
		});
		await WindowHelper.WaitForIdle();

		return list;
	}

	public static async Task<ListViewBase> SetUpGroupedEnvironment(ListControlType listType, int groupCount, int itemsCount)
	{
		ListViewBase list = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)XamlReader.Load(ListViewBaseXaml(listType == ListControlType.ListView ? "ListView" : "GridView"));
			list = (ListViewBase)rootPanel.FindName("list");
			var cvs = (CollectionViewSource)rootPanel.FindName("cvs");

			PopulateList(list, groupCount, true /*isGrouped*/, itemsCount, cvs);

			WindowHelper.WindowContent = rootPanel;
		});
		await WindowHelper.WaitForIdle();

		return list;
	}

	public static async Task<ListViewBase> SetUpResettableEnvironment(ListControlType listType, int itemsCount, int width = 500, int height = 500)
	{
		ListViewBase list = null;
		await RunOnUIThread(() =>
		{
			var rootPanel = new Grid();
			list = CreateList(listType);
			rootPanel.Children.Add(list);
			WindowHelper.WindowContent = rootPanel;
			list.Height = height;
			list.Width = width;
			PopulateList(list, itemsCount, false /* isGrouped */, 0, null, true /* isResettable */);
		});
		await WindowHelper.WaitForIdle();

		return list;
	}

	public static async Task<ListViewBase> SetUpGroupedResettableEnvironment(ListControlType listType, int groupCount, int itemsCount)
	{
		ListViewBase list = null;

		await RunOnUIThread(() =>
		{
			var rootPanel = (Grid)XamlReader.Load(ListViewBaseXaml(listType == ListControlType.ListView ? "ListView" : "GridView"));
			list = (ListViewBase)rootPanel.FindName("list");
			var cvs = (CollectionViewSource)rootPanel.FindName("cvs");

			PopulateList(list, groupCount, true /*isGrouped*/, itemsCount, cvs, true /* isResettable */);

			WindowHelper.WindowContent = rootPanel;
		});
		await WindowHelper.WaitForIdle();

		return list;
	}

	public static async Task SelectItemAtIndex(ListViewBase list, int index, bool doTap)
	{
		var selectionChangedRegistration = CreateSafeEventRegistration<ListViewBase, SelectionChangedEventHandler>("SelectionChanged");
		var selectionChangedEvent = new Event();
		FrameworkElement itemToSelect = null;

		await RunOnUIThread(() =>
		{
			selectionChangedRegistration.Attach(list, (sender, e) =>
			{
				selectionChangedEvent.Set();
			});

			itemToSelect = list.ContainerFromIndex(index) as FrameworkElement;
			VERIFY_IS_NOT_NULL(itemToSelect);
		});

		if (doTap)
		{
			InputHelper.Tap(itemToSelect);
		}
		else
		{
			InputHelper.LeftMouseClick(itemToSelect);
		}
		await selectionChangedEvent.WaitForDefault();
	}

	public static void PopulateList(ListViewBase list, int count = 25, bool isGrouped = false, int itemsCount = 4, CollectionViewSource cvs = null, bool isResettable = false)
	{
		if (isGrouped)
		{
			if (isResettable)
			{
				var dataCollection = MocoBasicDataSource.GetGroupedResettableDataCollection(count, itemsCount, false);
				cvs.Source = dataCollection;
				cvs.IsSourceGrouped = true;
			}
			else
			{
				var dataCollection = MocoBasicDataSource.GetGroupedDataCollection(count, itemsCount, false);
				cvs.Source = dataCollection;
				cvs.IsSourceGrouped = true;
			}
		}
		else
		{
			if (isResettable)
			{
				var dataCollection = MocoBasicDataSource.GetResettableDataCollection(count);
				list.ItemsSource = dataCollection;
			}
			else
			{
				var dataCollection = MocoBasicDataSource.GetFlatDataCollection(count);
				list.ItemsSource = dataCollection;
			}
		}
	}
}
