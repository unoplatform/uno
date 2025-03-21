using System;
using System.Linq;
using Windows.UI;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TreeView = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeView;
using TreeViewItem = Microsoft/* UWP don't rename */.UI.Xaml.Controls.TreeViewItem;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.TreeViewTests;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Uno.UI;
using MUXControlsTestApp.Utilities;

#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

#if __MACOS__
[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
#if __IOS__
[Ignore("Test is unstable on iOS currently")]
#endif
[TestClass]
[RunsOnUIThread]
public class Given_TreeView
{
	[TestMethod]
	public async Task When_Open_Close_Twice()
	{
		var SUT = new When_Open_Close_Twice();
		TestServices.WindowHelper.WindowContent = SUT;

		var root = new MyNode();
		root.Name = "root";
		var child1 = new MyNode { Name = "Child 1" };
		var child2 = new MyNode { Name = "Child 2" };
		root.Children.Add(child1);
		root.Children.Add(child2);

		SUT.myTree.ItemsSource = new[] { root };
		await TestServices.WindowHelper.WaitForIdle();

		var rootNode = (TreeViewItem)SUT.myTree.ContainerFromItem(root);
		rootNode.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		var child1Node = (TreeViewItem)SUT.myTree.ContainerFromItem(child1);
		Assert.IsNotNull(child1Node);
		Assert.AreEqual("Child 1", child1Node.Content);

		rootNode.IsExpanded = false;
		await TestServices.WindowHelper.WaitForIdle();

		rootNode.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.WindowHelper.WaitFor(() => (SUT.myTree.ContainerFromItem(child1) as TreeViewItem)?.Content?.ToString() == child1.Name);
		await TestServices.WindowHelper.WaitFor(() => (SUT.myTree.ContainerFromItem(child2) as TreeViewItem)?.Content?.ToString() == child2.Name);

		TreeViewItem child1NodeAfter = (TreeViewItem)SUT.myTree.ContainerFromItem(child1);
		Assert.IsNotNull(child1NodeAfter);

		Assert.AreEqual("Child 1", child1NodeAfter.Content);

		var child2NodeAfter = (TreeViewItem)SUT.myTree.ContainerFromItem(child2);
		await TestServices.WindowHelper.WaitForLoaded(child2NodeAfter);
		Assert.IsNotNull(child2NodeAfter);

		Assert.AreEqual("Child 2", child2NodeAfter.Content);
	}

	[TestMethod]
	public async Task When_Open_Close_Twice_Grid()
	{
		var SUT = new When_Open_Close_Twice_Grid();
		TestServices.WindowHelper.WindowContent = SUT;

		var root = new MyNode();
		root.Name = "root 4";
		root.IsDirectory = true;
		var child1 = new MyNode { Name = "Child 1" };
		var child2 = new MyNode { Name = "Child 2" };
		root.Children.Add(child1);
		root.Children.Add(child2);

		SUT.myTree.ItemsSource = new[] { root };
		await TestServices.WindowHelper.WaitForIdle();

		var rootNode = (TreeViewItem)SUT.myTree.ContainerFromItem(root);
		rootNode.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		var child1Node = (TreeViewItem)SUT.myTree.ContainerFromItem(child1);
		Assert.IsNotNull(child1Node);
		Assert.AreEqual(child1, child1Node.DataContext);

		rootNode.IsExpanded = false;
		await TestServices.WindowHelper.WaitForIdle();

		rootNode.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.WindowHelper.WaitFor(() => (SUT.myTree.ContainerFromItem(child1) as TreeViewItem)?.DataContext == child1);
		await TestServices.WindowHelper.WaitFor(() => (SUT.myTree.ContainerFromItem(child2) as TreeViewItem)?.DataContext == child2);

		await TestServices.WindowHelper.WaitFor(() => (SUT.myTree.ContainerFromItem(child1) as TreeViewItem)?.Content is Grid);
		await TestServices.WindowHelper.WaitFor(() => (SUT.myTree.ContainerFromItem(child2) as TreeViewItem)?.Content is Grid);

		var child1NodeAfter = (TreeViewItem)SUT.myTree.ContainerFromItem(child1);
		Assert.IsNotNull(child1NodeAfter);

		Assert.AreEqual(child1, child1NodeAfter.DataContext);

		var child2NodeAfter = (TreeViewItem)SUT.myTree.ContainerFromItem(child2);
		await TestServices.WindowHelper.WaitForLoaded(child2NodeAfter);
		Assert.IsNotNull(child2NodeAfter);

		Assert.AreEqual(child2, child2NodeAfter.DataContext);
	}

#if __ANDROID__
	[Ignore("Test is not operational on Android, items are not returned properly https://github.com/unoplatform/uno/issues/9080")]
#endif
	[TestMethod]
	public async Task When_Open_Close_Root_Twice_Keep_State()
	{
		var container = new Grid();
		container.Width = 500;
		container.Height = 500;

		var SUT = new When_Open_Close_Twice_Grid();

		container.Children.Add(SUT);

		TestServices.WindowHelper.WindowContent = container;

		var root = new MyNode();
		root.Name = "root 4";
		root.IsDirectory = true;
		var child1 = new MyNode { Name = "Child 1", IsDirectory = true };
		var child2 = new MyNode { Name = "Child 2", IsDirectory = true };
		var child3 = new MyNode { Name = "Child 3", IsDirectory = true };
		root.Children.Add(child1);
		root.Children.Add(child2);
		root.Children.Add(child3);

		var child11 = new MyNode { Name = "Child 11" };
		var child12 = new MyNode { Name = "Child 12" };
		child1.Children.Add(child11);
		child1.Children.Add(child12);

		var child21 = new MyNode { Name = "Child 21" };
		var child22 = new MyNode { Name = "Child 22" };
		child2.Children.Add(child21);
		child2.Children.Add(child22);

		var child31 = new MyNode { Name = "Child 31" };
		var child32 = new MyNode { Name = "Child 32" };
		child3.Children.Add(child31);
		child3.Children.Add(child32);

		SUT.myTree.ItemsSource = new[] { root };
		await TestServices.WindowHelper.WaitForIdle();

		var rootNode = (TreeViewItem)SUT.myTree.ContainerFromItem(root);
		rootNode.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		var child2Node = (TreeViewItem)SUT.myTree.ContainerFromItem(child2);
		child2Node.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		rootNode.IsExpanded = false;
		await TestServices.WindowHelper.WaitForIdle();

		rootNode.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		// Force refresh
		container.Width = 700;
		container.Height = 700;

		await TestServices.WindowHelper.WaitForIdle();

		var child1Node = (TreeViewItem)SUT.myTree.ContainerFromItem(child1);
		Assert.IsNotNull(child1Node);
		Assert.AreEqual(child1, child1Node.DataContext);

		rootNode.IsExpanded = false;
		await TestServices.WindowHelper.WaitForIdle();

		rootNode.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

		var child3Node = (TreeViewItem)SUT.myTree.ContainerFromItem(child3);

		// When index mappings are incorrect, the third node is expanded because
		// of recursive items removal in the TreeView control.
		Assert.IsFalse(child3Node.IsExpanded);
		child3Node.IsExpanded = false;
		await TestServices.WindowHelper.WaitForIdle();

		child3Node.IsExpanded = true;
		await TestServices.WindowHelper.WaitForIdle();

#if HAS_UNO
		Assert.AreEqual(
			1,
			MUXTestPage.FindVisualChildrenByType<TextBlock>(SUT.myTree).Count(c => c.Text == "Child 21"));
#endif
	}
}

public class MyNode
{
	public string Name { get; set; }

	public bool IsDirectory { get; set; }

	public ObservableCollection<MyNode> Children { get; } = new ObservableCollection<MyNode>();
}

public class FSObjectTemplateSelector : DataTemplateSelector
{
	public DataTemplate FileTemplate { get; set; }
	public DataTemplate DirectoryTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);

	protected override DataTemplate SelectTemplateCore(object item)
		=> item is MyNode node && node.IsDirectory
			? DirectoryTemplate
			: FileTemplate;
}
