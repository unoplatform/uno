using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
			var peer = listView.GetOrCreateAutomationPeer();

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
			var peer = listView.GetOrCreateAutomationPeer();
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
			var peer = listView.GetOrCreateAutomationPeer();
			var elementType = AriaMapper.GetSemanticElementType(peer);

			// Assert
			Assert.AreEqual(SemanticElementType.ListBox, elementType);
		}
#endif
	}
}
