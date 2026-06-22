using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Uno.UI.Runtime.Skia;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation
{
	/// <summary>
	/// Runtime tests for the DataGrid ARIA grid pattern mapping on Skia. The Community Toolkit
	/// DataGrid is not referenced here, so these use mock peers whose control type and patterns
	/// mirror the real toolkit peers (verified by reflection against
	/// Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid 7.1.205):
	///   - DataGridColumnHeaderAutomationPeer  -> AutomationControlType.HeaderItem, no GridItem
	///   - DataGridCellAutomationPeer          -> AutomationControlType.Custom, IGridItemProvider + ISelectionItemProvider
	/// Before the fix, headers mapped to a role-less generic node that emitted the invalid ARIA
	/// role "headeritem", and cells (Custom) emitted no role at all. AriaMapper.GetSemanticElementType
	/// must now resolve them to ColumnHeader / GridCell so the factory emits role="columnheader"
	/// and role="gridcell".
	/// </summary>
	[TestClass]
	public class Given_AccessibleDataGrid
	{
#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ColumnHeader_Then_SemanticType_Is_ColumnHeader()
		{
			var control = new ColumnHeaderControl();
			await UITestHelper.Load(control);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.ColumnHeader,
				AriaMapper.GetSemanticElementType(peer),
				"A HeaderItem peer must map to ColumnHeader (role=columnheader), not a generic 'headeritem' node.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Cell_With_GridItem_Then_SemanticType_Is_GridCell()
		{
			var control = new GridCellControl();
			await UITestHelper.Load(control);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.GridCell,
				AriaMapper.GetSemanticElementType(peer),
				"A Custom peer exposing the GridItem pattern must map to GridCell (role=gridcell), not a role-less generic node.");
		}

		// A control whose peer matches the toolkit's DataGridColumnHeaderAutomationPeer shape.
		private sealed partial class ColumnHeaderControl : Control
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new ColumnHeaderPeer(this);
		}

		private sealed partial class ColumnHeaderPeer : FrameworkElementAutomationPeer
		{
			public ColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.HeaderItem;
		}

		// A control whose peer matches the toolkit's DataGridCellAutomationPeer shape: Custom control
		// type, exposing GridItem + SelectionItem.
		private sealed partial class GridCellControl : Control
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new GridCellPeer(this);
		}

		private sealed partial class GridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider, ISelectionItemProvider
		{
			public GridCellPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.Custom;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem or PatternInterface.SelectionItem
					? this
					: base.GetPatternCore(patternInterface);

			public int Row => 4;
			public int Column => 1;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid => null;

			public bool IsSelected => true;
			public IRawElementProviderSimple SelectionContainer => null;
			public void AddToSelection() { }
			public void RemoveFromSelection() { }
			public void Select() { }
		}
#endif
	}
}
