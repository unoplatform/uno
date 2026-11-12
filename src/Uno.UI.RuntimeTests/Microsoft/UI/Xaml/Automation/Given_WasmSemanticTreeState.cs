#if HAS_UNO
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	[TestClass]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public class Given_WasmSemanticTreeState
	{
		[TestInitialize]
		public async Task EnableAccessibility() => await EnsureAccessibilityEnabledAsync();

		[TestCleanup]
		public void DisableAccessibilityAfterTest()
		{
			TestServices.WindowHelper.WindowContent = null;
			DisableAccessibility();
		}

		/// <summary>
		/// The items of a NavigationView are realized by an ItemsRepeater, so their semantic nodes are created
		/// from a role and a label only: the expanded state of an item with children has to be applied as well.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NavigationViewItem_Has_Children_Then_Exposes_Expanded_State()
		{
			var parent = new NavigationViewItem { Content = "Parent" };
			parent.MenuItems.Add(new NavigationViewItem { Content = "Child" });
			var leaf = new NavigationViewItem { Content = "Leaf" };
			var navigationView = new NavigationView
			{
				PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
				IsPaneOpen = true,
				Height = 400,
				Width = 500,
			};
			navigationView.MenuItems.Add(parent);
			navigationView.MenuItems.Add(leaf);
			await UITestHelper.Load(navigationView);

			await UITestHelper.WaitFor(
				() => SemanticElementExists(parent) && SemanticElementExists(leaf) && GetSemanticAttribute(parent, "aria-expanded") != string.Empty,
				timeoutMS: 5000,
				message: "Timed out waiting for the item with children to expose aria-expanded.");

			Assert.AreEqual("false", GetSemanticAttribute(parent, "aria-expanded"), "A collapsed item with children must report aria-expanded=false.");
			Assert.IsFalse(SemanticElementHasAttribute(leaf, "aria-expanded"), "An item without children is not expandable.");

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(parent);
			((IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)).Expand();
			await UITestHelper.WaitFor(() => GetSemanticAttribute(parent, "aria-expanded") == "true", timeoutMS: 5000, message: "Timed out waiting for aria-expanded=true.");
		}

		/// <summary>
		/// Same as above for the selection: a realized item of a list reports the state it is created with.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ListView_Item_Initially_Selected_Then_Exposes_Selected_State()
		{
			var listView = new ListView
			{
				ItemsSource = new[] { "A", "B", "C" },
				SelectedIndex = 1,
			};
			await UITestHelper.Load(listView);

			var selected = (ListViewItem)listView.ContainerFromIndex(1);
			var other = (ListViewItem)listView.ContainerFromIndex(0);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(selected, "aria-selected") == "true",
				timeoutMS: 5000,
				message: "Timed out waiting for the selected item to expose aria-selected=true.");
			Assert.AreEqual("false", GetSemanticAttribute(other, "aria-selected"), "An unselected item must report aria-selected=false.");
		}

		/// <summary>
		/// Only the selected tab of a Pivot or TabView has content in the visual tree, and a collapsed subtree
		/// is hidden from the semantic tree: controls on an inactive tab must not be exposed.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Content_Is_Inactive_Or_Collapsed_Then_Not_In_Semantic_Tree()
		{
			var pivotCombo = new ComboBox();
			pivotCombo.Items.Add("x");
			var pivot = new Pivot();
			pivot.Items.Add(new PivotItem { Header = "First", Content = new TextBlock { Text = "first" } });
			pivot.Items.Add(new PivotItem { Header = "Second", Content = pivotCombo });

			var tabCombo = new ComboBox();
			tabCombo.Items.Add("y");
			var tabView = new TabView { IsAddTabButtonVisible = false };
			tabView.TabItems.Add(new TabViewItem { Header = "Tab 1", Content = new TextBlock { Text = "tab 1" } });
			tabView.TabItems.Add(new TabViewItem { Header = "Tab 2", Content = tabCombo });

			var collapsedCombo = new ComboBox();
			var collapsed = new StackPanel { Visibility = Visibility.Collapsed };
			collapsed.Children.Add(collapsedCombo);

			var visibleCombo = new ComboBox();
			var panel = new StackPanel();
			panel.Children.Add(pivot);
			panel.Children.Add(tabView);
			panel.Children.Add(collapsed);
			panel.Children.Add(visibleCombo);
			await UITestHelper.Load(panel);
			await UITestHelper.WaitFor(() => SemanticElementExists(visibleCombo), timeoutMS: 5000, message: "Timed out waiting for the semantic tree.");

			Assert.IsFalse(SemanticElementExists(pivotCombo), "Content of an inactive PivotItem must not be exposed.");
			Assert.IsFalse(SemanticElementExists(tabCombo), "Content of an inactive TabViewItem must not be exposed.");
			Assert.IsFalse(SemanticElementExists(collapsedCombo), "Collapsed content must not be exposed.");
		}
	}
}
#endif
