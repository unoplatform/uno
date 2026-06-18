using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.TreeViewTests;
using Windows.System;
using Windows.UI;
using TreeView = Microsoft.UI.Xaml.Controls.TreeView;
using TreeViewItem = Microsoft.UI.Xaml.Controls.TreeViewItem;


#if HAS_UNO && !HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

#if __APPLE_UIKIT__
[Ignore("Test is unstable on iOS currently")]
#endif
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

#if __SKIA__
	[TestMethod]
	public async Task When_Delete_Node_With_MenuFlyout()
	{
		var SUT = new When_Delete_Node_With_MenuFlyout();
		TestServices.WindowHelper.WindowContent = SUT;

		var root = new MyNode();
		root.Name = "root";
		var child1 = new MyNode { Name = "Child 1" };
		var child2 = new MyNode { Name = "Child 2" };
		root.Children.Add(child1);
		root.Children.Add(child2);

		SUT.myTree.ItemsSource = new[] { root };
		await TestServices.WindowHelper.WaitForIdle();

		var rootCt = SUT.myTree.ContainerFromItem(root) as TreeViewItem;
		rootCt.IsExpanded = true;

		rootCt.Focus(FocusState.Programmatic);

		await TestServices.WindowHelper.WaitForIdle();

		var child1Ct = SUT.myTree.ContainerFromItem(child1) as TreeViewItem;
		child1Ct.Focus(FocusState.Programmatic);

		await TestServices.WindowHelper.WaitForIdle();

		// Pressing delete or any other key should not fail when a MenuFlyout
		child1Ct.RaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(SUT, VirtualKey.Delete, VirtualKeyModifiers.None));
	}
#endif

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

	[TestMethod]
	public async Task When_SelectNode_DoesNotToggle_OtherNodesExpansion()
	{
		// We had an issue where in a treeview of mixed expanded/collapsed nodes,
		// sometimes selecting a node could cause the viewport to shift and
		// toggling the IsExpanded of the nodes above. Happens only with selection, and not scrolling...

		// What happened was that TreeViewList::PrepareContainerForItemOverride syncs the IsExpanded state
		// between the container and the internal node bi-directionally, and the container was associated to a stale item.

		var sources = Enumerable.Range('A', 'Z' - 'A' + 1)
			.Select(x => new TreeNodeVM(((char)x).ToString(), isExpanded: true)
			{
				Children = [..Enumerable.Range('A', 'Z' - 'A' + 1).Select(y =>
					new TreeNodeVM($"{(char)x}{(char)y}")
				)]
			})
			.ToArray();
		var flattened = Flatten(sources).ToArray();

		var SUT = new TreeView
		{
			Width = 150,
			Height = 500,
			SelectionMode = TreeViewSelectionMode.Single,
			ItemsSource = sources,
			ItemTemplate = XamlHelper.LoadXaml<DataTemplate>("""
				<DataTemplate>
					<TreeViewItem Height="50" ItemsSource="{Binding Children}" IsExpanded="{Binding IsExpanded, Mode=TwoWay}">
						<TextBlock Text="{Binding Name}" />
					</TreeViewItem>
				</DataTemplate>
			"""),
		};
		await UITestHelper.Load(SUT);
		await TestServices.WindowHelper.WaitFor(() => IsWithinViewport(SUT.ContainerFromItem(sources[0])));

		foreach (var c in "BCDEFGHI")
		{
			// select and wait for the selection BringIntoView to occur
			var node = flattened.First(x => x.Name == $"{c}A");
			SUT.SelectedItem = node;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => SUT.ContainerFromItem(node) is { });
		}

		Assert.IsTrue(sources.All(x => x.IsExpanded), "All top-level nodes should remain expanded.");

		IEnumerable<TreeNodeVM> Flatten(IEnumerable<TreeNodeVM> nodes) =>
			nodes.SelectMany(x => Flatten(x.Children).Prepend(x));
		bool IsWithinViewport(DependencyObject x)
		{
			if (x is FrameworkElement fe)
			{
				var offset = fe.TransformToVisual(SUT).TransformPoint(default);

				return 0 <= offset.Y && (offset.Y + fe.ActualHeight - 1.0) <= SUT.ActualHeight;
			}

			return false;
		}
	}
}

public class MyNode
{
	public string Name { get; set; }

	public bool IsDirectory { get; set; }

	public ObservableCollection<MyNode> Children { get; } = new ObservableCollection<MyNode>();
}

public class TreeNodeVM : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	private string _name;
	private bool _isExpanded;
	public string Name { get => _name; set => SetAndRaisePropertyChanged(ref _name, value); }
	public bool IsExpanded { get => _isExpanded; set => SetAndRaisePropertyChanged(ref _isExpanded, value); }

	public TreeNodeVM(string name, bool isExpanded = false)
	{
		_name = name;
		_isExpanded = isExpanded;
	}

	public ObservableCollection<TreeNodeVM> Children { get; init; } = new();

	public override string ToString() => $"[{(IsExpanded ? "+" : "-")}]{Name}";

	private void SetAndRaisePropertyChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
	{
		if (!EqualityComparer<T>.Default.Equals(field, value))
		{
			field = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
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
