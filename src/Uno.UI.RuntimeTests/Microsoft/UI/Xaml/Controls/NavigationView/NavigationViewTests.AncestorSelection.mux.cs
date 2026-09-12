#nullable enable

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Microsoft.UI.Xaml.Tests.MUXControls.ApiTests;

public partial class NavigationViewTests
{
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24508")]
	public async Task When_SelectedChildMovesBetweenParents_OnlyCurrentAncestorsStaySelected()
	{
		var selected = new AncestorSelectionNode("Selected");
		var source = new AncestorSelectionNode("Source");
		source.Children.Add(selected);
		source.Children.Add(new AncestorSelectionNode("Remaining"));
		var destination = new AncestorSelectionNode("Destination");
		destination.Children.Add(new AncestorSelectionNode("Existing"));
		var navigation = new NavigationView
		{
			PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
			IsPaneOpen = true,
			Width = 600,
			Height = 500,
			MenuItemsSource = new ObservableCollection<AncestorSelectionNode> { source, destination },
			MenuItemTemplate = (DataTemplate)XamlReader.Load("""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<NavigationViewItem Content="{Binding Title}" MenuItemsSource="{Binding Children}" IsExpanded="True" />
				</DataTemplate>
				""")
		};

		try
		{
			await UITestHelper.Load(navigation);
			navigation.SelectedItem = selected;
			await WindowHelper.WaitForIdle();
			var sourceContainer = (NavigationViewItem)navigation.ContainerFromMenuItem(source);
			var destinationContainer = (NavigationViewItem)navigation.ContainerFromMenuItem(destination);
			Assert.IsTrue(sourceContainer.IsChildSelected);

			source.Children.Remove(selected);
			destination.Children.Add(selected);
			await WindowHelper.WaitForIdle();
			navigation.SelectedItem = selected;
			await WindowHelper.WaitForIdle();

			Assert.AreSame(selected, navigation.SelectedItem);
			var selectedContainer = (NavigationViewItem)navigation.ContainerFromMenuItem(selected);
			Assert.IsFalse(sourceContainer.IsChildSelected);
			Assert.IsTrue(destinationContainer.IsChildSelected);
			Assert.IsFalse(selectedContainer.IsChildSelected);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(sourceContainer);
			((IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)).Collapse();
			await WindowHelper.WaitForIdle();
			Assert.AreSame(selected, navigation.SelectedItem);
			Assert.IsTrue(selectedContainer.IsSelected);
			Assert.IsFalse(sourceContainer.IsChildSelected);
			Assert.IsTrue(destinationContainer.IsChildSelected);

			navigation.IsPaneOpen = false;
			await WindowHelper.WaitForIdle();
			navigation.IsPaneOpen = true;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(sourceContainer.IsChildSelected);
			Assert.IsTrue(destinationContainer.IsChildSelected);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24508")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_AncestorIndexChanges_DeselectClearsOriginalAncestor(bool changePaneMode)
	{
		var selected = new NavigationViewItem { Content = "Selected" };
		var innerParent = new NavigationViewItem { Content = "Inner parent", IsExpanded = true };
		innerParent.MenuItems.Add(selected);
		var parent = new NavigationViewItem { Content = "Parent", IsExpanded = true };
		parent.MenuItems.Add(innerParent);
		var topLevel = new NavigationViewItem { Content = "Top level" };
		var roots = new ObservableCollection<object> { parent, topLevel };
		var navigation = new NavigationView
		{
			PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
			IsPaneOpen = true,
			Width = 600,
			Height = 500,
			MenuItemsSource = roots
		};

		try
		{
			await UITestHelper.Load(navigation);
			navigation.SelectedItem = selected;
			await WindowHelper.WaitForIdle();

			if (changePaneMode)
			{
				navigation.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
				navigation.IsPaneOpen = true;
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(parent.IsChildSelected);
				Assert.IsTrue(innerParent.IsChildSelected);
			}

			roots.Insert(0, new NavigationViewItem { Content = "Inserted" });
			await WindowHelper.WaitForIdle();
			Assert.AreSame(selected, navigation.SelectedItem);
			Assert.IsTrue(parent.IsChildSelected);
			Assert.IsTrue(innerParent.IsChildSelected);

			navigation.SelectedItem = topLevel;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(parent.IsChildSelected);
			Assert.IsFalse(innerParent.IsChildSelected);
			Assert.IsFalse(topLevel.IsChildSelected);
			Assert.IsFalse(selected.IsChildSelected);
			Assert.IsTrue(topLevel.IsSelected);

			navigation.SelectedItem = selected;
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(parent.IsChildSelected);
			Assert.IsTrue(innerParent.IsChildSelected);
			navigation.SelectedItem = null;
			await WindowHelper.WaitForIdle();
			Assert.IsFalse(parent.IsChildSelected);
			Assert.IsFalse(innerParent.IsChildSelected);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24508")]
	public async Task When_SelectedChildMovesIntoCollapsedParent_SelectionFollowsItsNewAncestor()
	{
		var selected = new AncestorSelectionNode("Selected");
		var source = new AncestorSelectionNode("Source");
		var destination = new AncestorSelectionNode("Destination");
		source.Children.Add(selected);
		source.Children.Add(new AncestorSelectionNode("Remaining"));
		destination.Children.Add(new AncestorSelectionNode("Existing"));
		var navigation = new NavigationView
		{
			PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
			IsPaneOpen = true,
			Width = 600,
			Height = 500,
			MenuItemsSource = new ObservableCollection<AncestorSelectionNode> { source, destination },
			MenuItemTemplate = (DataTemplate)XamlReader.Load("""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<NavigationViewItem Content="{Binding Title}" MenuItemsSource="{Binding Children}" IsExpanded="True" />
				</DataTemplate>
				""")
		};

		try
		{
			await UITestHelper.Load(navigation);
			navigation.SelectedItem = selected;
			await WindowHelper.WaitForIdle();
			var sourceContainer = (NavigationViewItem)navigation.ContainerFromMenuItem(source);
			var destinationContainer = (NavigationViewItem)navigation.ContainerFromMenuItem(destination);
			destinationContainer.IsExpanded = false;
			await WindowHelper.WaitForIdle();
			source.Children.Remove(selected);
			destination.Children.Add(selected);
			navigation.SelectedItem = selected;
			await WindowHelper.WaitForIdle();
			Assert.AreSame(selected, navigation.SelectedItem);
			Assert.IsFalse(sourceContainer.IsChildSelected);
			Assert.IsTrue(destinationContainer.IsChildSelected);
			Assert.IsFalse(destinationContainer.IsExpanded);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	public sealed class AncestorSelectionNode(string title)
	{
		public string Title { get; } = title;
		public ObservableCollection<AncestorSelectionNode> Children { get; } = new();
	}
}
