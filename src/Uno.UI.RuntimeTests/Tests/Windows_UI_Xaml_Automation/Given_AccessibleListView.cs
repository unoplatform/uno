using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
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

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ListView_Is_Activated_Then_Initial_Option_State_And_Roving_Tab_Stop_Are_Valid()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Alpha", "Beta", "Gamma" },
				SelectedIndex = 1,
				Width = 240,
				Height = 240,
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();
			var first = listView.ContainerFromIndex(0) as ListViewItem;
			var selected = listView.ContainerFromIndex(1) as ListViewItem;
			var disabled = listView.ContainerFromIndex(2) as ListViewItem;
			Assert.IsNotNull(first);
			Assert.IsNotNull(selected);
			Assert.IsNotNull(disabled);
			disabled!.IsEnabled = false;

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(first!) && SemanticElementExists(selected!) && SemanticElementExists(disabled),
				timeoutMS: 5000,
				message: "Timed out waiting for realized ListView options.");

			Assert.AreEqual("option", GetSemanticAttribute(first!, "role"));
			Assert.AreEqual("false", GetSemanticAttribute(first!, "aria-selected"));
			Assert.AreEqual("true", GetSemanticAttribute(selected!, "aria-selected"));
			Assert.AreEqual("true", GetSemanticAttribute(disabled, "aria-disabled"));
			Assert.AreEqual("-1", GetSemanticAttribute(disabled, "tabindex"));
			var tabStopCount = InvokeBrowserJs($"document.querySelectorAll('#{GetSemanticElementId(listView)} > [role=\"option\"][tabindex=\"0\"]:not([aria-disabled=\"true\"])').length.toString()");
			var optionState = InvokeBrowserJs($"JSON.stringify(Array.from(document.querySelectorAll('#{GetSemanticElementId(listView)} [role=\"option\"]')).map(e => ({{id:e.id,parent:e.parentElement?.id,selected:e.getAttribute('aria-selected'),disabled:e.getAttribute('aria-disabled'),focusable:e.dataset.unoOptionFocusable,tabIndex:e.tabIndex}})))");
			Assert.AreEqual("1", tabStopCount, $"A nonempty ListView must expose exactly one enabled option as its roving tab stop. Options: {optionState}");
			Assert.AreEqual("0", GetSemanticAttribute(selected!, "tabindex"), "The initially selected option should own the roving tab stop.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GridView_Is_Activated_Then_It_Uses_Listbox_Option_Semantics()
		{
			var gridView = new GridView
			{
				ItemsSource = new List<string> { "One", "Two", "Three" },
				Width = 320,
				Height = 240,
			};

			await UITestHelper.Load(gridView);
			await TestServices.WindowHelper.WaitForIdle();
			var first = gridView.ContainerFromIndex(0) as GridViewItem;
			Assert.IsNotNull(first);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(gridView) && SemanticElementExists(first!),
				timeoutMS: 5000,
				message: "Timed out waiting for GridView list semantics.");

			Assert.AreEqual("listbox", GetSemanticAttribute(gridView, "role"), "GridView exposes UIA List and must map to an ARIA listbox.");
			Assert.AreEqual("option", GetSemanticAttribute(first!, "role"), "A GridView item must be an option under its listbox, not a row without cells.");
			Assert.AreEqual(
				"0",
				InvokeBrowserJs($"document.querySelectorAll('#{GetSemanticElementId(gridView)} > [role=\"row\"]').length.toString()"),
				"GridView must not emit orphan row roles without gridcell descendants.");
			Assert.AreEqual(
				"1",
				InvokeBrowserJs($"document.querySelectorAll('#{GetSemanticElementId(gridView)} > [role=\"option\"][tabindex=\"0\"]').length.toString()"),
				"GridView must expose exactly one roving option tab stop.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Virtualized_Option_Layout_Changes_Then_Position_Remains_Listbox_Relative()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Offset item", "Second item" },
				Margin = new Thickness(320, 80, 0, 0),
				Width = 220,
				Height = 180,
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();
			var first = listView.ContainerFromIndex(0) as ListViewItem;
			Assert.IsNotNull(first);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(listView) && SemanticElementExists(first!),
				timeoutMS: 5000,
				message: "Timed out waiting for the offset ListView semantics.");

			first!.Margin = new Thickness(7, 5, 0, 0);
			await TestServices.WindowHelper.WaitForIdle();
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs($"(function(){{const list=document.getElementById('{GetSemanticElementId(listView)}');const item=document.getElementById('{GetSemanticElementId(first)}');if(!list||!item)return '0';const lr=list.getBoundingClientRect();const ir=item.getBoundingClientRect();return ir.left>=lr.left&&ir.left<lr.right&&ir.top>=lr.top&&ir.top<lr.bottom?'1':'0';}})()") == "1",
				timeoutMS: 3000,
				message: "A virtualized option switched to root-relative coordinates after layout and was offset twice by its listbox position.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Focused_Virtualized_Option_Moves_Then_Deferred_Clear_Follows_The_Handle()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Pinned", "Next", "Third" },
				Width = 240,
				Height = 220,
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();
			var first = listView.ContainerFromIndex(0) as ListViewItem;
			var second = listView.ContainerFromIndex(1) as ListViewItem;
			Assert.IsNotNull(first);
			Assert.IsNotNull(second);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(first!) && SemanticElementExists(second!),
				timeoutMS: 5000,
				message: "Timed out waiting for focus-pin option semantics.");

			var region = GetVirtualizedRegion(listView);
			var regionType = region.GetType();
			var pinnedHandle = regionType.GetProperty("PinnedHandle", BindingFlags.Instance | BindingFlags.NonPublic);
			var indexChanged = regionType.GetMethod("OnItemIndexChanged", BindingFlags.Instance | BindingFlags.NonPublic);
			var unrealized = regionType.GetMethod("OnItemUnrealized", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(pinnedHandle);
			Assert.IsNotNull(indexChanged);
			Assert.IsNotNull(unrealized);

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(first!)}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => (IntPtr)pinnedHandle!.GetValue(region)! == first!.Visual.Handle,
				timeoutMS: 3000,
				message: "Browser focus did not pin the realized option by handle.");

			indexChanged!.Invoke(region, new object[] { first.Visual.Handle, 0, 4, 5 });
			var removedImmediately = (bool)unrealized!.Invoke(region, new object[] { first.Visual.Handle, 4 })!;
			Assert.IsFalse(removedImmediately, "Unrealizing the focused handle must defer semantic removal until focus leaves.");
			Assert.IsTrue(SemanticElementExists(first), "A focused option must remain in the semantic DOM while its clear is deferred.");
			Assert.AreEqual(first.Visual.Handle, (IntPtr)pinnedHandle.GetValue(region)!, "An index change must not retarget a handle-based focus pin.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(second!)}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => !SemanticElementExists(first) && (IntPtr)pinnedHandle.GetValue(region)! == second!.Visual.Handle,
				timeoutMS: 3000,
				message: "Moving focus did not release the deferred old option and retain the new focused handle.");
		}

		private static object GetVirtualizedRegion(ListViewBase listView)
		{
			var accessibility = GetAccessibility();
			var accessibilityType = accessibility.GetType();
			var registrations = accessibilityType.GetField("_virtualizedRegions", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(accessibility) as IDictionary;
			Assert.IsNotNull(registrations);
			var registration = registrations![listView.Visual.Handle];
			Assert.IsNotNull(registration, "The ListView did not own a registered virtualized semantic region.");
			var region = registration!.GetType().GetProperty("Region", BindingFlags.Instance | BindingFlags.Public)?.GetValue(registration);
			Assert.IsNotNull(region);
			return region!;
		}

		private static object GetAccessibility()
		{
			var accessibilityType = Type.GetType("Uno.UI.Runtime.Skia.WebAssemblyAccessibility, Uno.UI.Runtime.Skia.WebAssembly.Browser", throwOnError: true)!;
			var accessibility = accessibilityType.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
			Assert.IsNotNull(accessibility);
			return accessibility!;
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ListView_Item_Is_Removed_Then_Its_Semantic_Node_Is_Cleared()
		{
			var items = new ObservableCollection<string> { "A", "B", "C" };
			var listView = new ListView { ItemsSource = items, Width = 200, Height = 200 };

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();
			var removedContainer = listView.ContainerFromIndex(1) as UIElement;
			Assert.IsNotNull(removedContainer);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(removedContainer), timeoutMS: 5000,
				message: "Timed out waiting for the ListView item semantic node.");

			items.RemoveAt(1);
			await UITestHelper.WaitFor(() => !SemanticElementExists(removedContainer), timeoutMS: 3000,
				message: "Cleared ListView container remained in the semantic DOM.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Virtualized_Item_Replaces_Same_Index_Then_Old_Relationship_Authority_Is_Removed()
		{
			var source = new Button { Content = "Source" };
			var listView = new ListView
			{
				ItemsSource = new List<string> { "Old item" },
				Width = 200,
				Height = 120,
			};
			var panel = new StackPanel { Children = { source, listView } };

			await UITestHelper.Load(panel);
			await TestServices.WindowHelper.WaitForIdle();
			var oldItem = listView.ContainerFromIndex(0) as UIElement;
			Assert.IsNotNull(oldItem);
			AutomationProperties.GetFlowsFrom(oldItem).Add(source);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(source, "aria-flowto") == GetSemanticElementId(oldItem),
				timeoutMS: 5000,
				message: "Timed out waiting for the virtualized inverse-flow relationship.");

			var replacement = new Border { Width = 20, Height = 20 };
			AutomationProperties.SetName(replacement, "Replacement item");
			var accessibility = GetAccessibility();
			var emitRealizedItem = accessibility.GetType().GetMethod("EmitRealizedItem", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(emitRealizedItem);
			emitRealizedItem.Invoke(accessibility, new object[]
			{
				GetVirtualizedRegion(listView),
				listView.Visual.Handle,
				replacement,
				0,
				1,
				"option",
			});

			await UITestHelper.WaitFor(
				() => !SemanticElementExists(oldItem) && !SemanticElementHasAttribute(source, "aria-flowto"),
				timeoutMS: 3000,
				message: "Same-index replacement retained the old semantic node or its inverse-flow authority.");
			Assert.IsTrue(SemanticElementExists(replacement));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Virtualized_Region_Resynchronizes_Then_All_Stale_Handles_Are_Reported()
		{
			var listView = new ListView
			{
				ItemsSource = new List<string> { "First", "Second" },
				Width = 200,
				Height = 160,
			};

			await UITestHelper.Load(listView);
			await TestServices.WindowHelper.WaitForIdle();
			var first = listView.ContainerFromIndex(0) as UIElement;
			var second = listView.ContainerFromIndex(1) as UIElement;
			Assert.IsNotNull(first);
			Assert.IsNotNull(second);
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(first) && SemanticElementExists(second),
				timeoutMS: 5000,
				message: "Timed out waiting for the virtualized items.");

			var region = GetVirtualizedRegion(listView);
			var resynchronizeItems = region.GetType().GetMethod("ResynchronizeItems", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(resynchronizeItems);
			var removedHandles = resynchronizeItems.Invoke(
				region,
				new object[] { Array.Empty<(IntPtr Handle, int Index)>(), 0 }) as IntPtr[];
			Assert.IsNotNull(removedHandles);
			CollectionAssert.AreEquivalent(
				new[] { first.Visual.Handle, second.Visual.Handle },
				removedHandles,
				"Resynchronization must report every stale handle so the owner can revoke relationship state.");

			var accessibility = GetAccessibility();
			var cleanupHandles = accessibility.GetType().GetMethod("CleanupVirtualizedHandles", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(cleanupHandles);
			cleanupHandles.Invoke(accessibility, new object[] { removedHandles });
			Assert.IsFalse(SemanticElementExists(first));
			Assert.IsFalse(SemanticElementExists(second));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ListItem_Position_Becomes_Unknown_Then_Set_Position_Attributes_Are_Removed()
		{
			var item = new UnknownPositionListItemControl { PositionInSet = 2, SizeOfSet = 5 };
			var list = new MockListBoxControl { Children = { item } };

			await UITestHelper.Load(list);
			list.GetOrCreateAutomationPeer();
			var peer = item.GetOrCreateAutomationPeer();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(item), timeoutMS: 5000,
				message: "Timed out waiting for the unknown-position list item.");

			Assert.AreEqual("option", GetSemanticAttribute(item, "role"));
			Assert.AreEqual("2", GetSemanticAttribute(item, "aria-posinset"));
			Assert.AreEqual("5", GetSemanticAttribute(item, "aria-setsize"));

			item.PositionInSet = 0;
			item.SizeOfSet = 0;
			AutomationProperties.SetPositionInSet(item, 0);
			AutomationProperties.SetSizeOfSet(item, 0);
			await UITestHelper.WaitFor(
				() => !SemanticElementHasAttribute(item, "aria-posinset") && !SemanticElementHasAttribute(item, "aria-setsize"),
				timeoutMS: 3000,
				message: "Unknown set position left stale aria-posinset/aria-setsize values.");
		}

		private sealed partial class MockListBoxControl : Grid
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new MockListBoxPeer(this);
		}

		private sealed partial class MockListBoxPeer : FrameworkElementAutomationPeer
		{
			public MockListBoxPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
		}

		private sealed partial class UnknownPositionListItemControl : Control
		{
			public int PositionInSet { get; set; }
			public int SizeOfSet { get; set; }

			public UnknownPositionListItemControl()
			{
				Width = 100;
				Height = 30;
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new UnknownPositionListItemPeer(this);
		}

		private sealed partial class UnknownPositionListItemPeer : FrameworkElementAutomationPeer
		{
			public UnknownPositionListItemPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

			protected override int GetPositionInSetCore() => ((UnknownPositionListItemControl)Owner).PositionInSet;

			protected override int GetSizeOfSetCore() => ((UnknownPositionListItemControl)Owner).SizeOfSet;
		}





#endif

	}
}
