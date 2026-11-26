#if HAS_UNO
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// The XAML controls own the keyboard, as in WinUI: a key pressed while a semantic element has DOM
	/// focus bubbles to the document and is handled by the focused XAML control. The semantic layer must not
	/// act on the same key as well, so each key press has to produce exactly one action.
	/// </summary>
	[TestClass]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public class Given_WasmSemanticKeyboard
	{
		[TestInitialize]
		public async Task EnableAccessibility() => await EnsureAccessibilityEnabledAsync();

		[TestCleanup]
		public void DisableAccessibilityAfterTest()
		{
			TestServices.WindowHelper.WindowContent = null;
			DisableAccessibility();
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Space_Pressed_On_ListViewItem_Then_Toggled_Once(bool xamlOnly)
		{
			var listView = new ListView
			{
				SelectionMode = ListViewSelectionMode.Multiple,
				ItemsSource = new[] { "A", "B", "C" },
			};
			var changes = 0;
			listView.SelectionChanged += (_, _) => changes++;
			await UITestHelper.Load(listView);

			var item = (ListViewItem)listView.ContainerFromIndex(0);
			await FocusSemantic(item);

			Press(xamlOnly, " ");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(1, listView.SelectedItems.Count, "Space must select the focused item.");
			Assert.AreEqual(1, changes, "Space must change the selection once.");

			Press(xamlOnly, " ");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(0, listView.SelectedItems.Count, "A second Space must deselect the item.");
			Assert.AreEqual(2, changes, "Each Space must change the selection once.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Keys_Pressed_On_ClickableListViewItem_Then_ItemClick_Raised_Once(bool xamlOnly)
		{
			var listView = new ListView
			{
				IsItemClickEnabled = true,
				SelectionMode = ListViewSelectionMode.None,
				ItemsSource = new[] { "A", "B", "C" },
			};
			var clicks = 0;
			listView.ItemClick += (_, _) => clicks++;
			await UITestHelper.Load(listView);

			var item = (ListViewItem)listView.ContainerFromIndex(0);
			foreach (var key in new[] { "Enter", " " })
			{
				await FocusSemantic(item);
				var before = clicks;
				Press(xamlOnly, key);
				await UITestHelper.WaitForIdle();
				Assert.AreEqual(before + 1, clicks, $"'{key}' must raise ItemClick once.");
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Keys_Pressed_On_HyperlinkButton_Then_Click_Raised_Once(bool xamlOnly)
		{
			var link = new HyperlinkButton { Content = "Link" };
			var clicks = 0;
			link.Click += (_, _) => clicks++;
			await UITestHelper.Load(link);

			foreach (var key in new[] { "Enter", " " })
			{
				await FocusSemantic(link);
				var before = clicks;
				Press(xamlOnly, key);
				await UITestHelper.WaitForIdle();
				Assert.AreEqual(before + 1, clicks, $"'{key}' must raise Click once.");
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Keys_Pressed_On_TreeViewItem_Then_Handled_Once(bool xamlOnly)
		{
			var tree = new TreeView();
			var parent = new TreeViewNode { Content = "Parent" };
			parent.Children.Add(new TreeViewNode { Content = "Child 1" });
			parent.Children.Add(new TreeViewNode { Content = "Child 2" });
			tree.RootNodes.Add(parent);
			tree.RootNodes.Add(new TreeViewNode { Content = "Sibling" });
			await UITestHelper.Load(tree);

			var item = (TreeViewItem)tree.ContainerFromNode(parent);
			await FocusSemantic(item);

			Press(xamlOnly, "ArrowRight");
			await UITestHelper.WaitForIdle();
			Assert.IsTrue(parent.IsExpanded, "Right must expand the collapsed item.");
			Assert.AreSame(item, GetFocusedElement(), "Right on a collapsed item must only expand it, not also move the focus to its child.");

			Press(xamlOnly, "ArrowLeft");
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(parent.IsExpanded, "Left must collapse the expanded item.");
			Assert.AreSame(item, GetFocusedElement(), "Left on an expanded item must only collapse it, not also move the focus to its parent.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_ArrowDown_Pressed_On_TreeViewItem_Then_Focus_Moves_Once(bool xamlOnly)
		{
			var tree = new TreeView();
			for (var i = 0; i < 4; i++)
			{
				tree.RootNodes.Add(new TreeViewNode { Content = "Node " + i });
			}

			await UITestHelper.Load(tree);

			var first = (TreeViewItem)tree.ContainerFromNode(tree.RootNodes[0]);
			var second = (TreeViewItem)tree.ContainerFromNode(tree.RootNodes[1]);
			await FocusSemantic(first);

			Press(xamlOnly, "ArrowDown");
			await UITestHelper.WaitForIdle();
			Assert.AreSame(second, GetFocusedElement(), "Down must move the focus by exactly one item.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Keys_Pressed_On_MenuFlyoutItem_Then_Handled_Once(bool xamlOnly)
		{
			var clicks = new List<string>();
			var flyout = new MenuFlyout();
			foreach (var name in new[] { "One", "Two", "Three", "Four" })
			{
				var menuItem = new MenuFlyoutItem { Text = name };
				menuItem.Click += (_, _) => clicks.Add(name);
				flyout.Items.Add(menuItem);
			}

			var button = new Button { Content = "Open" };
			var openerClicks = 0;
			button.Click += (_, _) => openerClicks++;
			await UITestHelper.Load(button);

			try
			{
				flyout.ShowAt(button);
				await UITestHelper.WaitForIdle();

				var first = flyout.Items[0];
				var second = flyout.Items[1];
				await UITestHelper.WaitFor(() => SemanticElementExists(first), timeoutMS: 5000, message: "Timed out waiting for the menu items.");
				await FocusSemantic(first);

				Press(xamlOnly, "ArrowDown");
				await UITestHelper.WaitForIdle();
				Assert.AreSame(second, GetFocusedElement(), "Down must move the focus by exactly one item.");

				Press(xamlOnly, "ArrowUp");
				await UITestHelper.WaitForIdle();
				Assert.AreSame(first, GetFocusedElement(), "Up must move the focus by exactly one item.");

				Press(xamlOnly, "Enter");
				await UITestHelper.WaitForIdle();
				CollectionAssert.AreEqual(new[] { "One" }, clicks, "Enter must invoke the focused item once.");
				Assert.AreEqual(0, openerClicks, "The key must not reach the control that had the focus before the flyout.");
			}
			finally
			{
				flyout.Hide();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow(false)]
		[DataRow(true)]
		public async Task When_Keys_Pressed_On_TabViewItem_Then_Handled_Once(bool xamlOnly)
		{
			var tabView = new TabView { IsAddTabButtonVisible = false };
			for (var i = 0; i < 3; i++)
			{
				tabView.TabItems.Add(new TabViewItem { Header = "Tab " + i, Content = new TextBlock { Text = "Content " + i } });
			}

			var changes = 0;
			tabView.SelectionChanged += (_, _) => changes++;
			await UITestHelper.Load(tabView);
			await UITestHelper.WaitForIdle();

			var second = (TabViewItem)tabView.ContainerFromIndex(1);
			await FocusSemantic(second);
			var before = changes;
			Press(xamlOnly, "Enter");
			await UITestHelper.WaitForIdle();
			Assert.AreEqual(1, tabView.SelectedIndex, "Enter must select the focused tab.");
			Assert.AreEqual(before + 1, changes, "Enter must change the selection once.");
		}

		// xamlOnly dispatches the key on the document, so that only the XAML keyboard input sees it.
		private static void Press(bool xamlOnly, string key)
		{
			if (xamlOnly)
			{
				var code = key == " " ? "Space" : key;
				InvokeBrowserJs("(function(){['keydown','keyup'].forEach(function(type){document.dispatchEvent(new KeyboardEvent(type, { key: '" + key + "', code: '" + code + "', bubbles: true, cancelable: true }));}); return 'ok';})()");
			}
			else
			{
				PressKeyOnFocusedSemanticElement(key);
			}
		}

		private static UIElement GetFocusedElement()
			=> FocusManager.GetFocusedElement(TestServices.WindowHelper.XamlRoot) as UIElement;

		private static async Task FocusSemantic(UIElement element)
		{
			await UITestHelper.WaitFor(() => SemanticElementExists(element), timeoutMS: 5000, message: $"Timed out waiting for the semantic node of {element.GetType().Name}.");
			Assert.IsTrue(((Control)element).Focus(FocusState.Keyboard), $"{element.GetType().Name} must accept the focus.");
			await UITestHelper.WaitFor(
				() => GetActiveElementId() == GetSemanticElementId(element),
				timeoutMS: 5000,
				message: $"Timed out waiting for the semantic node of {element.GetType().Name} to receive DOM focus (active: {GetActiveElementId()}).");
			await UITestHelper.WaitForIdle();
		}
	}
}
#endif
