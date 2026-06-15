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
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
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

		private static void EnableAccessibilityThroughDom()
		{
			InvokeBrowserJs("(function(){const button = document.getElementById('uno-enable-accessibility'); if (button) { button.click(); } return 'ok';})()");
		}

		// Targets the exact semantic element for a given element via its visual handle, mirroring the
		// id scheme used by the WASM runtime (uno-semantics-{handle}).
		private static string GetSemanticElementId(UIElement element)
			=> $"uno-semantics-{((long)element.Visual.Handle)}";

		private static bool SemanticElementExists(UIElement element)
			=> InvokeBrowserJs($"(function(){{return document.getElementById('{GetSemanticElementId(element)}') ? '1' : '0';}})()") == "1";

		private static string GetSemanticAttribute(UIElement element, string attribute)
			=> InvokeBrowserJs($"(function(){{const e = document.getElementById('{GetSemanticElementId(element)}'); return e ? (e.getAttribute('{attribute}') ?? '') : '';}})()");

		private static string InvokeBrowserJs(string javascript)
		{
			var runtimeType = Type.GetType("Uno.Foundation.WebAssemblyRuntime, Uno.Foundation.Runtime.WebAssembly", throwOnError: false);
			Assert.IsNotNull(runtimeType, "Unable to locate Uno.Foundation.WebAssemblyRuntime at runtime.");

			var invokeJs = runtimeType.GetMethod("InvokeJS", new[] { typeof(string) });
			Assert.IsNotNull(invokeJs, "Unable to locate Uno.Foundation.WebAssemblyRuntime.InvokeJS(string).");

			return invokeJs.Invoke(obj: null, parameters: new object[] { javascript }) as string ?? string.Empty;
		}
#endif

	}
}
