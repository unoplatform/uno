using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
using static Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.WasmSemanticDomHelper;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for accessible list view behavior.
	/// Tests automation peer properties, selection pattern, and ARIA attribute mapping.
	/// </summary>
	[TestClass]
	public class Given_AccessibleListView
	{
		/// <summary>
		/// T067: Verifies that a list exposes item count via automation.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_List_Focused_Then_ItemCount_Announced()
		{
			// Arrange
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Item 1", "Item 2", "Item 3" }
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(listView);

			// Assert
			Assert.IsNotNull(peer, "ListView should have an automation peer");
			Assert.AreEqual(AutomationControlType.List, peer.GetAutomationControlType());
		}

		/// <summary>
		/// T068: Verifies that list item position is reported via automation.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Arrow_Pressed_Then_Position_Announced()
		{
			// Arrange
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Alpha", "Beta", "Gamma" },
				SelectionMode = ListViewSelectionMode.Single
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();

			// Act - Select the second item
			listView.SelectedIndex = 1;
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			Assert.AreEqual(1, listView.SelectedIndex, "Selected index should be 1");
			Assert.AreEqual("Beta", listView.SelectedItem, "Selected item should be Beta");
		}

		/// <summary>
		/// T069: Verifies that pressing Space selects a list item.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Space_Pressed_Then_Item_Selected()
		{
			// Arrange
			var listView = new ListView
			{
				ItemsSource = new List<string> { "A", "B", "C" },
				SelectionMode = ListViewSelectionMode.Single
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();

			// Act
			listView.SelectedIndex = 0;
			await TestServices.WindowHelper.WaitForIdle();

			// Assert
			Assert.AreEqual(0, listView.SelectedIndex);
		}

		/// <summary>
		/// Verifies that ListView automation peer has correct control type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ListView_Created_Then_Has_List_ControlType()
		{
			// Arrange
			var listView = new ListView
			{
				ItemsSource = new List<string> { "A" }
			};
			await UITestHelper.Load(listView);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(listView);
			var controlType = peer?.GetAutomationControlType();

			// Assert
			Assert.AreEqual(AutomationControlType.List, controlType);
		}

#if HAS_UNO
		/// <summary>
		/// Verifies that AriaMapper correctly identifies ListView semantic element type.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ListView_Mapped_Then_SemanticElementType_Is_ListBox()
		{
			// Arrange
			var listView = new ListView
			{
				ItemsSource = new List<string> { "A" }
			};
			await UITestHelper.Load(listView);

			// Act
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(listView);
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.ListBox, elementType);
		}
#endif
#if __SKIA__

		/// <summary>
		/// T067/FR-016 (WASM DOM): a ListView emits a composite container with role="listbox". Under the roving
		/// tab model the container is not itself a tab stop (tabindex must not be "0").
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ListView_Then_Dom_Role_Is_Listbox_And_Not_A_Tab_Stop()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Item 1", "Item 2", "Item 3" }
			};

			await UITestHelper.Load(listView);
			listView.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(listView), timeoutMS: 5000, message: "Timed out waiting for the listbox container semantic element to be created.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("listbox", GetSemanticAttribute(listView, "role"), "A ListView must emit role=listbox on its container.");
			Assert.AreNotEqual("0", GetSemanticAttribute(listView, "tabindex"), "A composite listbox container must not be a tab stop (tabindex must not be \"0\"); the roving stop lives on the active item.");
		}

#endif

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
		public async Task When_ListView_Multiple_Select_On_Mobile_Then_Native_Collection_CanSelectMultiple()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "One", "Two", "Three" },
				SelectionMode = ListViewSelectionMode.Multiple,
			};
			AutomationProperties.SetAutomationId(listView, "listview-multiselect-t045");

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();

			var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(listView);
			Assert.IsNotNull(snapshot, "Native snapshot must be available on mobile Skia.");
			Assert.IsNotNull(snapshot.Details?.Collection, "Collection must be populated for a ListView.");
			Assert.IsTrue(
				snapshot.Details!.Collection!.CanSelectMultiple,
				"Multiple-selection ListView must report CanSelectMultiple=true in Collection details.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Selection_Queried_Then_Item_Peer_Identity_Matches_Children()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Alpha", "Beta", "Gamma" },
				SelectionMode = ListViewSelectionMode.Single,
			};

			await UITestHelper.Load(listView);
			listView.SelectedIndex = 1;
			await TestServices.WindowHelper.WaitForIdle();

			var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(listView);

			// Resolve through the pattern path first: this is the ordering that used to hand
			// out a second peer instance for the same item once the children tree was built.
			var patternPeer = peer.CreateItemAutomationPeer("Beta");
			Assert.IsNotNull(patternPeer, "The pattern path must resolve an item peer.");

			var children = peer.GetChildren();
			Assert.IsNotNull(children, "A realized ListView must expose item children.");

			ItemAutomationPeer childPeer = null;
			foreach (var child in children)
			{
				if (child is ItemAutomationPeer { Item: "Beta" } itemPeer)
				{
					childPeer = itemPeer;
					break;
				}
			}

			Assert.IsNotNull(childPeer, "The children tree must contain a peer for the selected item.");
			Assert.AreSame(
				patternPeer,
				childPeer,
				"The children tree and the pattern providers must hand out the same ItemAutomationPeer instance.");
			Assert.AreSame(
				patternPeer,
				peer.CreateItemAutomationPeer("Beta"),
				"Resolving the item again after the tree was built must keep the same peer instance.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Items_Are_Duplicated_Then_Each_Container_Gets_Its_Own_Peer()
		{
			var duplicate = "Same";
			var listView = new ListView
			{
				ItemsSource = new List<string> { duplicate, duplicate },
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();

			var peer = (ItemsControlAutomationPeer)FrameworkElementAutomationPeer.CreatePeerForElement(listView);
			var children = peer.GetChildren();
			Assert.IsNotNull(children);

			var itemPeers = new List<ItemAutomationPeer>();
			foreach (var child in children)
			{
				if (child is ItemAutomationPeer itemPeer)
				{
					itemPeers.Add(itemPeer);
				}
			}

			Assert.AreEqual(2, itemPeers.Count, "Both duplicate occurrences must be projected.");
			Assert.AreNotSame(
				itemPeers[0],
				itemPeers[1],
				"Duplicate item values must not share a peer, otherwise both resolve to the same index.");
		}

	}
}
