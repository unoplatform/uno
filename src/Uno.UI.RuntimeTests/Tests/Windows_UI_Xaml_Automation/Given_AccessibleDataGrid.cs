using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
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
	/// Runtime tests for the DataGrid ARIA grid pattern mapping on Skia. The Community Toolkit
	/// DataGrid is not referenced here, so these use mock peers whose control type and patterns
	/// mirror the real toolkit peers (verified by reflection against
	/// Uno.CommunityToolkit.WinUI.UI.Controls.DataGrid 7.1.205):
	///   - DataGridAutomationPeer              -> AutomationControlType.DataGrid, IGrid + ISelection
	///   - DataGridColumnHeaderAutomationPeer  -> AutomationControlType.HeaderItem, no GridItem
	///   - DataGridItemAutomationPeer (row)    -> AutomationControlType.DataItem, ISelectionItem
	///   - DataGridCellAutomationPeer          -> AutomationControlType.Custom, IGridItem + ISelectionItem
	/// Before the fix, headers mapped to a role-less generic node that emitted the invalid ARIA
	/// role "headeritem", and cells (Custom) emitted no role at all. AriaMapper.GetSemanticElementType
	/// must now resolve them to ColumnHeader / GridCell so the factory emits role="columnheader"
	/// and role="gridcell".
	///
	/// Two layers are covered:
	///   - HAS_UNO: the AriaMapper type resolution (runs on all Skia, incl. Desktop / CI).
	///   - SkiaWasm: the full C# -> JSImport -> TS -> semantic-DOM emission, asserted against the
	///     real <c>#uno-semantics-root</c> overlay (the same tree an external runner inspects).
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
			var grid = CreateGridScope(control);
			await UITestHelper.Load(grid, x => x.IsLoaded);

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
			var grid = CreateGridScope(control);
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.GridCell,
				AriaMapper.GetSemanticElementType(peer),
				"A Custom peer exposing the GridItem pattern must map to GridCell (role=gridcell), not a role-less generic node.");
		}

		[TestMethod]
		[DataRow(AutomationControlType.Text)]
		[DataRow(AutomationControlType.ComboBox)]
		[DataRow(AutomationControlType.CheckBox)]
		[RunsOnUIThread]
		public async Task When_Typed_Cell_Has_GridItem_Then_SemanticType_Is_GridCell(AutomationControlType controlType)
		{
			var control = new TypedGridCellControl(controlType);
			var grid = CreateGridScope(control);
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.GridCell,
				AriaMapper.GetSemanticElementType(peer),
				$"A {controlType} DataGridCell peer exposing GridItem must remain the gridcell wrapper.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Header_Contains_HeaderItems_Then_SemanticType_Is_GridRow()
		{
			var control = new HeaderRowControl();
			var grid = CreateGridScope(control);
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.GridRow,
				AriaMapper.GetSemanticElementType(peer),
				"A UIA Header container owning HeaderItem peers must provide their required ARIA row context.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Row_Selection_Is_Provided_By_EventsSource_Then_Selected_Is_Exposed()
		{
			var control = new GridRowControl();
			var grid = CreateGridScope(control);
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = (GridRowPeer)FrameworkElementAutomationPeer.CreatePeerForElement(control);
			peer.EventsSource = new GridRowItemPeer(control, isSelected: true);

			Assert.AreEqual(true, AriaMapper.GetAriaAttributes(peer).Selected);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NonDataGrid_Item_Has_GridItem_Then_Control_Semantics_Are_Preserved()
		{
			var control = new CalendarGridItemControl();
			var calendar = new CalendarGridControl(control);
			await UITestHelper.Load(calendar, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.Generic,
				AriaMapper.GetSemanticElementType(peer),
				"GridItem capability alone must not turn CalendarView-style items into gridcells.");
			Assert.IsNull(
				AriaMapper.GetAriaRole(peer.GetAutomationControlType()),
				"A context-free UIA DataItem must not fall through to role=row.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NonDataGrid_Item_Is_Nested_In_DataGrid_Then_ContainingGrid_Is_Authoritative()
		{
			var control = new CalendarGridItemControl();
			var calendar = new CalendarGridControl(control);
			var grid = CreateGridScope(calendar);
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.Generic,
				AriaMapper.GetSemanticElementType(peer),
				"An explicit non-DataGrid ContainingGrid must prevent an outer DataGrid from claiming the item.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_GridItem_Has_No_ContainingGrid_Then_Outer_DataGrid_Is_Not_Borrowed()
		{
			var control = new NullContainingGridItemControl();
			var grid = CreateGridScope(control);
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);
			Assert.AreEqual(
				SemanticElementType.Button,
				AriaMapper.GetSemanticElementType(peer),
				"A null GridItem.ContainingGrid must not fall through to an unrelated visual ancestor.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Nested_HeaderItems_Are_Not_Table_Headers_Then_Header_Remains_Heading()
		{
			var foreignHeader = new HeaderRowControl(new ColumnHeaderControl());
			var grid = new GridContainerControl { Children = { foreignHeader } };
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(foreignHeader);
			Assert.IsNotNull(peer);
			Assert.AreEqual(
				SemanticElementType.Heading,
				AriaMapper.GetSemanticElementType(peer),
				"A nested Header must not become a DataGrid row unless its HeaderItems belong to the grid's Table provider.");
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_DataGrid_RowHeader_Then_SemanticType_Is_RowHeader()
		{
			var rowHeader = new RowHeaderControl();
			var row = new GridRowControl { Children = { rowHeader, new GridCellControl() } };
			var grid = CreateGridScope(row);
			await UITestHelper.Load(grid, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(rowHeader);
			Assert.IsNotNull(peer);
			Assert.AreEqual(SemanticElementType.RowHeader, AriaMapper.GetSemanticElementType(peer));
		}

#endif

#if __SKIA__
		// The DOM-level tests below assert the rendered semantic overlay on WASM Skia — the same
		// #uno-semantics-root tree an external (Playwright/Appium) runner reads — but with the fix
		// compiled in via project reference, so they are the in-repo "after" for the WASM backend.

		/// <summary>
		/// Issues #1/#2/#5 (grid container): a DataGrid peer exposing IGrid + ISelection emits
		/// role="grid" with aria-rowcount / aria-colcount and aria-multiselectable for Extended
		/// selection. Before the fix the container already mapped to grid, but multiselectable was
		/// never surfaced.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Then_Dom_Emits_Grid_Role_Counts_And_Multiselectable()
		{
			var control = new GridContainerControl();

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the grid semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("grid", GetSemanticAttribute(control, "role"), "A DataGrid peer must emit role=grid.");
			Assert.AreEqual("500", GetSemanticAttribute(control, "aria-rowcount"), "aria-rowcount must come from IGridProvider.RowCount.");
			Assert.AreEqual("3", GetSemanticAttribute(control, "aria-colcount"), "aria-colcount must come from IGridProvider.ColumnCount.");
			Assert.AreEqual("true", GetSemanticAttribute(control, "aria-multiselectable"), "Extended selection (ISelectionProvider.CanSelectMultiple) must emit aria-multiselectable.");
		}

		/// <summary>
		/// Issue #1 (column header): a HeaderItem peer emits role="columnheader" instead of the
		/// invalid role="headeritem" the generic path produced before the fix.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Then_Dom_Role_Is_ColumnHeader()
		{
			var control = new ColumnHeaderControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"), "A HeaderItem peer must emit role=columnheader (not the invalid 'headeritem').");
			Assert.AreEqual(
				"row",
				InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(control)}').parentElement.getAttribute('role')"),
				"A DataGrid columnheader must be owned by the Raw Toolkit header presenter emitted as role=row.");
		}
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_First_Header_Is_Registered_After_Visual_Add_Then_Presenter_Is_Reconciled()
		{
			var headerRow = new HeaderRowControl(allowEmpty: true);
			var grid = new GridContainerControl { Children = { headerRow } };

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(grid), timeoutMS: 5000, message: "Timed out waiting for the dynamic-header grid.");
			Assert.IsFalse(SemanticElementExists(headerRow));

			var header = new ColumnHeaderControl();
			headerRow.Children.Add(header);
			grid.ColumnHeaders.Add(header);
			await UITestHelper.WaitFor(
				() => SemanticElementExists(headerRow) && GetSemanticAttribute(header, "role") == "columnheader",
				timeoutMS: 3000,
				message: "The provider-settled first header was not reconciled into grid > row > columnheader.");
			Assert.AreEqual("row", GetSemanticAttribute(headerRow, "role"));
			Assert.AreEqual(GetSemanticElementId(headerRow), InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(header)}').parentElement.id"));

			headerRow.Children.Remove(header);
			grid.ColumnHeaders.Remove(header);
			await UITestHelper.WaitFor(() => !SemanticElementExists(headerRow), timeoutMS: 3000, message: "The empty Raw header presenter was not demoted.");

			headerRow.Children.Add(header);
			grid.ColumnHeaders.Add(header);
			await UITestHelper.WaitFor(
				() => SemanticElementExists(headerRow) && GetSemanticAttribute(header, "role") == "columnheader",
				timeoutMS: 3000,
				message: "The first header was not restored after re-add.");
		}

		/// <summary>
		/// Issue #4 (sort, emission path): the column header surfaces aria-sort from the generic
		/// HelpText channel used by Toolkit DataGrid 7.1.205.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Sorted_Then_Dom_Emits_Aria_Sort()
		{
			var control = new SortedColumnHeaderControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the sorted column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("ascending", GetSemanticAttribute(control, "aria-sort"), "A Toolkit column header reporting ascending HelpText must emit aria-sort=ascending.");
		}

		/// <summary>
		/// Issues #1/#2/#5 (cell): a Custom peer exposing IGridItem + ISelectionItem emits
		/// role="gridcell" with 1-based aria-rowindex / aria-colindex and aria-selected. Before the
		/// fix the cell rendered role-less with none of these.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Cell_Then_Dom_Emits_GridCell_Role_Indices_And_Selected()
		{
			var control = new GridCellControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the grid-cell semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("gridcell", GetSemanticAttribute(control, "role"), "A GridItem peer must emit role=gridcell.");
			// GridItemProvider is 0-based (Row=4, Column=1); ARIA indices are 1-based and
			// the visible column-header row occupies row 1.
			Assert.AreEqual("6", GetSemanticAttribute(control, "aria-rowindex"), "aria-rowindex must include the ARIA header-row offset.");
			Assert.AreEqual("2", GetSemanticAttribute(control, "aria-colindex"), "aria-colindex must be IGridItemProvider.Column + 1.");
			Assert.AreEqual("true", GetSemanticAttribute(control, "aria-selected"), "A selected cell (ISelectionItemProvider.IsSelected) must emit aria-selected=true.");
		}

		/// <summary>
		/// Issues #1/#5 (row), plus the bogus-uniform-index regression: a DataItem peer emits
		/// role="row" and aria-selected from ISelectionItem, and must NOT carry aria-rowindex (the row
		/// peer reports no position; the per-row index travels on each cell). Before the fix every row
		/// emitted aria-rowindex=1.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Row_Then_Dom_Emits_Row_Role_Selected_And_No_Bogus_RowIndex()
		{
			var control = new GridRowControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the grid-row semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("row", GetSemanticAttribute(control, "role"), "A DataItem peer must emit role=row.");
			Assert.AreEqual("false", GetSemanticAttribute(control, "aria-selected"), "An unselected row must emit aria-selected=false.");
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-rowindex"), "A row with no known position must NOT emit aria-rowindex (no more uniform 'row 1').");
		}

		/// <summary>
		/// Issue #519: a single-selection grid explicitly advertises aria-multiselectable=false.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SingleSelect_Grid_Then_Multiselectable_Is_False()
		{
			var control = new GridContainerControl { CanSelectMultipleValue = false };

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the single-select grid semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("grid", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("500", GetSemanticAttribute(control, "aria-rowcount"));
			Assert.AreEqual("false", GetSemanticAttribute(control, "aria-multiselectable"));
		}

		/// <summary>
		/// Issue #2 hygiene: an empty grid (RowCount/ColumnCount 0) omits aria-rowcount/aria-colcount
		/// but still announces role=grid.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Empty_Grid_Then_No_Counts_But_Role_Grid()
		{
			var control = new GridContainerControl { RowCountValue = 0, ColumnCountValue = 0 };

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the empty grid semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("grid", GetSemanticAttribute(control, "role"), "An empty grid is still role=grid.");
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-rowcount"), "RowCount=0 must omit aria-rowcount.");
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-colcount"), "ColumnCount=0 must omit aria-colcount.");
			Assert.AreEqual("0", GetSemanticAttribute(control, "tabindex"), "A focusable empty grid must remain reachable by Tab.");
		}

		/// <summary>
		/// Issue #5 hygiene: a cell whose peer does not expose SelectionItem must omit aria-selected
		/// entirely (omitting is correct; aria-selected="false" would wrongly imply a selectable cell).
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Cell_Not_Selectable_Then_No_Aria_Selected()
		{
			var control = new UnselectableGridCellControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the unselectable grid-cell semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("gridcell", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("3", GetSemanticAttribute(control, "aria-colindex"), "A non-selectable cell still carries aria-colindex.");
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-selected"), "A cell with no SelectionItem pattern must NOT emit aria-selected.");
		}

		/// <summary>
		/// Issue #1/#4 hygiene: a plain (unsorted, no-GridItem) column header omits aria-sort and
		/// aria-colindex is resolved from the containing DataGrid's Table header collection.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Unsorted_Then_No_Sort_And_Has_ColIndex()
		{
			var control = new ColumnHeaderControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the plain column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"));
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-sort"), "An unsorted header must NOT emit aria-sort.");
			Assert.AreEqual("1", GetSemanticAttribute(control, "aria-colindex"));
		}

		/// <summary>
		/// Issue #4: a header reporting a descending ItemStatus emits aria-sort=descending.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Descending_Then_Aria_Sort_Descending()
		{
			var control = new DescendingColumnHeaderControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the descending column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("descending", GetSemanticAttribute(control, "aria-sort"));
		}

		/// <summary>
		/// Issue #4 hygiene: an ItemStatus that is neither ascending nor descending (e.g. "Busy") must
		/// NOT be mis-mapped to aria-sort.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Unknown_Status_Then_No_Aria_Sort()
		{
			var control = new BusyColumnHeaderControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the busy column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"));
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-sort"), "A non-sort ItemStatus must NOT produce aria-sort.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Sort_Changes_Then_Authored_Description_Is_Preserved()
		{
			var control = new MutableColumnHeaderControl();
			AutomationProperties.SetFullDescription(control, "Customer surname");
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var peer = (MutableColumnHeaderPeer)control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the mutable column header.");

			Assert.AreEqual("Customer surname", GetSemanticAttribute(control, "aria-description"));
			peer.SetItemStatus("Ascending");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "aria-sort") == "ascending", timeoutMS: 3000, message: "aria-sort did not update to ascending.");
			Assert.AreEqual("Customer surname", GetSemanticAttribute(control, "aria-description"));

			peer.SetItemStatus(string.Empty);
			await UITestHelper.WaitFor(() => !SemanticElementHasAttribute(control, "aria-sort"), timeoutMS: 3000, message: "aria-sort did not clear.");
			Assert.AreEqual("Customer surname", GetSemanticAttribute(control, "aria-description"));
		}

		/// <summary>
		/// Issue #2 (positive branch): a row peer that DOES report a position emits aria-rowindex
		/// (the de-bogus fix omits it only when the position is unknown).
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Row_With_Position_Then_Aria_RowIndex()
		{
			var control = new PositionedGridRowControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the positioned grid-row semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("row", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("7", GetSemanticAttribute(control, "aria-rowindex"), "A row reporting PositionInSet=7 must emit aria-rowindex=7.");
		}

		/// <summary>
		/// Issue #1/#2: a cell with an accessible name announces it as aria-label.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Cell_With_Name_Then_Aria_Label()
		{
			var control = new GridCellControl();
			AutomationProperties.SetName(control, "John Smith");
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the named grid-cell semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("gridcell", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("John Smith", GetSemanticAttribute(control, "aria-label"), "A named cell must expose its content as aria-label.");
		}

		/// <summary>
		/// Issue #5 (dynamic) / de-risks #6: toggling a cell's selection at runtime updates aria-selected
		/// via the push (NotifyPropertyChangedEvent) path, not only at creation time.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Cell_Selection_Toggled_Then_Aria_Selected_Updates()
		{
			var control = new MutableGridCellControl();
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var peer = (MutableGridCellPeer)control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the mutable grid-cell semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("true", GetSemanticAttribute(control, "aria-selected"), "Cell starts selected.");

			peer.SetSelected(false);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "aria-selected") == "false", timeoutMS: 3000, message: "aria-selected did not update to false after a runtime selection change.");
		}
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Cell_Selection_Changes_Through_Automation_Event_Then_Aria_Selected_Updates()
		{
			var control = new MutableGridCellControl(isSelected: true);
			var grid = CreateGridScope(control);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var peer = (MutableGridCellPeer)control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the event-driven cell.");

			peer.SetSelectedFromAutomationEvent(false);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "aria-selected") == "false", timeoutMS: 3000, message: "Selection automation event did not refresh aria-selected.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Header_Sort_Changes_Through_Invoke_Event_Then_Aria_Sort_Updates()
		{
			var header = new MutableColumnHeaderControl();
			var grid = CreateGridScope(header);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var peer = (MutableColumnHeaderPeer)header.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(header), timeoutMS: 5000, message: "Timed out waiting for the event-driven header.");

			peer.SetItemStatusFromAutomationEvent("Descending");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(header, "aria-sort") == "descending", timeoutMS: 3000, message: "Header invoke automation event did not refresh aria-sort.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_NonCurrent_Cell_Is_Selected_Then_Current_Cell_Remains_The_Tab_Stop()
		{
			var current = new MutableGridCellControl(row: 0, column: 0, isSelected: false);
			var selected = new MutableGridCellControl(row: 0, column: 1, isSelected: false);
			var row = new GridRowControl { Children = { current, selected } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var selectedPeer = (MutableGridCellPeer)selected.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(selected), timeoutMS: 5000, message: "Timed out waiting for the selected gridcell.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(current)}').focus(); 'ok'");
			selectedPeer.SetSelected(true);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(selected, "aria-selected") == "true", timeoutMS: 3000, message: "Selected state did not update.");
			Assert.AreEqual("0", GetSemanticAttribute(current, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(selected, "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Selection_Provider_Throws_Then_Sibling_Semantics_Still_Emit()
		{
			var throwing = new ThrowingSelectionGridCellControl();
			var sibling = new GridCellControl();
			var row = new GridRowControl { Children = { throwing, sibling } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(sibling), timeoutMS: 5000, message: "A throwing provider aborted sibling semantic emission.");

			Assert.IsTrue(SemanticElementExists(throwing));
			Assert.IsFalse(SemanticElementHasAttribute(throwing, "aria-selected"));
			Assert.AreEqual("gridcell", GetSemanticAttribute(sibling, "role"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Has_Multiple_Cells_Then_Exactly_One_Tab_Stop()
		{
			var first = new MutableGridCellControl(row: 0, column: 0);
			var second = new MutableGridCellControl(row: 0, column: 1);
			var row = new GridRowControl { Children = { first, second } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(second), timeoutMS: 5000, message: "Timed out waiting for the second gridcell semantic element.");

			var gridId = GetSemanticElementId(grid);
			Assert.AreEqual(
				"1",
				InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[role=gridcell][tabindex=\"0\"], [role=columnheader][tabindex=\"0\"], [role=rowheader][tabindex=\"0\"]').length.toString()"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GridCell_Receives_Browser_Focus_Then_Managed_Focus_Is_On_Grid()
		{
			var cell = new GridCellControl { IsTabStop = false };
			var grid = CreateGridScope(cell);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 5000, message: "Timed out waiting for the focused gridcell semantic element.");

			var cellId = GetSemanticElementId(cell);
			InvokeBrowserJs($"document.getElementById('{cellId}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(grid.XamlRoot), grid),
				timeoutMS: 10000,
				message: "Browser focus on a semantic cell did not move managed focus to its DataGrid.");

			Assert.AreEqual(
				cellId,
				InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''"),
				$"Grid={GetSemanticElementId(grid)}; Row={GetSemanticElementId((GridRowControl)grid.Children[1])}");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Receives_Browser_Focus_Then_Active_Cell_Remains_The_Only_Tab_Stop()
		{
			var first = new GridCellControl();
			var second = new GridCellControl();
			var row = new GridRowControl { Children = { first, second } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(second), timeoutMS: 5000, message: "Timed out waiting for the direct-focus grid.");

			var gridId = GetSemanticElementId(grid);
			InvokeBrowserJs($"document.getElementById('{gridId}').focus(); 'ok'");
			Assert.AreEqual("-1", GetSemanticAttribute(grid, "tabindex"));
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
			Assert.AreEqual(GetSemanticElementId(first), InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Final_Focused_Cell_Is_Removed_Then_Grid_Becomes_The_Tab_Stop()
		{
			var cell = new GridCellControl();
			var row = new GridRowControl { Children = { cell } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 5000, message: "Timed out waiting for the final gridcell.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(cell)}').focus(); 'ok'");
			row.Children.Remove(cell);
			await UITestHelper.WaitFor(() => !SemanticElementExists(cell), timeoutMS: 3000, message: "Final gridcell semantic node was not removed.");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(grid, "tabindex") == "0", timeoutMS: 3000, message: "The empty grid did not become the fallback tab stop.");

			Assert.AreEqual("0", GetSemanticAttribute(grid, "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Active_Id_Is_Invalid_Then_Cell_Removal_Still_Completes()
		{
			var first = new GridCellControl();
			var second = new GridCellControl();
			var row = new GridRowControl { Children = { first, second } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(second), timeoutMS: 5000, message: "Timed out waiting for the invalid-active-id grid.");

			var gridId = GetSemanticElementId(grid);
			InvokeBrowserJs($"document.getElementById('{gridId}').dataset.unoGridActiveId = 'stale]active'; 'ok'");
			row.Children.Remove(first);

			await UITestHelper.WaitFor(() => !SemanticElementExists(first), timeoutMS: 3000,
				message: "An invalid stale grid active ID aborted semantic cell removal.");
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()") == "1",
				timeoutMS: 3000,
				message: "Grid focus repair did not restore a single tab stop after ignoring the invalid active ID.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Active_Cell_Is_Hidden_And_Shown_Then_Exactly_One_Tab_Stop_Remains()
		{
			var first = new GridCellControl();
			var second = new GridCellControl();
			var row = new GridRowControl { Children = { first, second } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(second), timeoutMS: 5000, message: "Timed out waiting for the hide/show grid.");

			var gridId = GetSemanticElementId(grid);
			var firstHandle = (long)first.Visual.Handle;
			InvokeBrowserJs($"globalThis.Uno.UI.Runtime.Skia.Accessibility.hideSemanticElement({firstHandle}); 'ok'");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(second, "tabindex") == "0", timeoutMS: 3000, message: "The visible replacement cell did not become the tab stop.");
			Assert.AreEqual("0", GetSemanticAttribute(second, "tabindex"));
			InvokeBrowserJs($"globalThis.Uno.UI.Runtime.Skia.Accessibility.updateSemanticElementPositioning({firstHandle}, 120, 32, 0, 0); 'ok'");
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
			Assert.AreEqual("0", GetSemanticAttribute(second, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(first, "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Focus_Reports_Current_Cell_Then_Dom_Follows_Current_Cell()
		{
			var first = new GridCellControl { IsTabStop = false };
			var current = new GridCellControl { IsTabStop = false };
			var row = new GridRowControl { Children = { first, current } };
			var grid = CreateGridScope(row);
			grid.GotFocus += (_, _) => current.GetOrCreateAutomationPeer().RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(current), timeoutMS: 5000, message: "Timed out waiting for the current gridcell semantic element.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(first)}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''") == GetSemanticElementId(current),
				timeoutMS: 10000,
				message: "Toolkit-reported current-cell focus did not replace the provisional grid tab stop.");
			Assert.AreEqual("0", GetSemanticAttribute(current, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(first, "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Is_Nested_In_Cell_Then_Inner_Grid_Has_No_Extra_Tab_Stop()
		{
			var innerCell = new GridCellControl();
			var innerGrid = CreateGridScope(innerCell);
			var outerCell = new GridCellContentControl(innerGrid);
			var outerGrid = CreateGridScope(outerCell);

			await UITestHelper.Load(outerGrid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(innerCell) && SemanticElementExists(outerCell),
				timeoutMS: 10000,
				message: "Timed out waiting for the nested gridcell semantic elements.");

			var outerGridId = GetSemanticElementId(outerGrid);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(innerCell, "tabindex") == "-1" &&
					InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[role=gridcell][tabindex=\"0\"], [role=columnheader][tabindex=\"0\"], [role=rowheader][tabindex=\"0\"]').length.toString()") == "1",
				timeoutMS: 10000,
				message: "The nested grid did not settle on one outer tab stop.");

			var innerCellId = GetSemanticElementId(innerCell);
			InvokeBrowserJs($"document.getElementById('{innerCellId}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(innerCell, "tabindex") == "0" &&
					GetSemanticAttribute(outerCell, "tabindex") == "-1" &&
					InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[role=gridcell][tabindex=\"0\"], [role=columnheader][tabindex=\"0\"], [role=rowheader][tabindex=\"0\"]').length.toString()") == "1",
				timeoutMS: 10000,
				message: "Focusing the nested cell did not preserve exactly one grid tab stop.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Is_Disabled_Then_No_Tab_Stop_And_Reenable_Restores_One()
		{
			var cell = new GridCellControl();
			var grid = CreateGridScope(cell);
			grid.IsEnabledValue = false;

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var peer = grid.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 5000, message: "Timed out waiting for the disabled grid semantic tree.");

			var gridId = GetSemanticElementId(grid);
			Assert.AreEqual("true", GetSemanticAttribute(grid, "aria-disabled"));
			Assert.AreEqual("0", InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));

			grid.IsEnabledValue = true;
			peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, false, true);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(grid, "aria-disabled") == "false", timeoutMS: 3000, message: "aria-disabled did not update after re-enabling the grid.");
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
		}
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Active_Cell_Is_Disabled_Then_Adjacent_Cell_Receives_Focus_And_One_Tab_Stop_Remains()
		{
			var first = new GridCellControl();
			var active = new GridCellControl();
			var adjacent = new GridCellControl();
			var row = new GridRowControl { Children = { first, active, adjacent } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var activePeer = active.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(adjacent), timeoutMS: 5000, message: "Timed out waiting for the disable-transition grid.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(active)}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''") == GetSemanticElementId(active),
				timeoutMS: 10000,
				message: "The middle cell did not receive browser focus before it was disabled.");
			active.IsEnabled = false;
			activePeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''") == GetSemanticElementId(adjacent),
				timeoutMS: 10000,
				message: "Disabling the active middle cell did not move focus to its adjacent successor.");

			var gridId = GetSemanticElementId(grid);
			Assert.AreEqual("-1", GetSemanticAttribute(active, "tabindex"));
			Assert.AreEqual("0", GetSemanticAttribute(adjacent, "tabindex"));
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(active)}').focus(); 'ok'");
			Assert.AreEqual(GetSemanticElementId(adjacent), InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''"));
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{gridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));

			active.IsEnabled = true;
			activePeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, false, true);
			Assert.AreEqual("0", GetSemanticAttribute(adjacent, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(active, "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Row_Is_Disabled_Then_Descendant_Tab_Stops_Are_Removed()
		{
			var cell = new GridCellControl();
			var row = new FocusableGridRowControl(new Grid { Children = { cell } });
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var rowPeer = row.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 5000, message: "Timed out waiting for the row-disable grid.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(cell)}').focus(); 'ok'");
			row.IsEnabled = false;
			rowPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(row, "aria-disabled") == "true", timeoutMS: 3000, message: "The row did not become disabled.");

			var gridId = GetSemanticElementId(grid);
			Assert.AreEqual("-1", GetSemanticAttribute(cell, "tabindex"));
			Assert.AreEqual("0", GetSemanticAttribute(grid, "tabindex"));
			Assert.AreEqual("1", InvokeBrowserJs($"(function(){{const g=document.getElementById('{gridId}');return ((g.tabIndex===0?1:0)+g.querySelectorAll('[tabindex=\"0\"]').length).toString();}})()"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Interaction_Cell_Is_Disabled_Then_Descendant_Tab_Stop_Is_Removed()
		{
			var innerButton = new Button { Content = "Edit" };
			var cell = new GridCellContentControl(innerButton);
			var grid = CreateGridScope(cell);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var cellPeer = cell.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(innerButton), timeoutMS: 5000, message: "Timed out waiting for the interaction descendant.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(innerButton)}').focus(); 'ok'");
			cell.IsEnabled = false;
			cellPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(cell, "aria-disabled") == "true", timeoutMS: 3000, message: "The interaction cell did not become disabled.");

			var gridId = GetSemanticElementId(grid);
			Assert.AreEqual("-1", GetSemanticAttribute(innerButton, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(grid, "tabindex"));
			Assert.AreEqual("1", InvokeBrowserJs($"(function(){{const g=document.getElementById('{gridId}');return ((g.tabIndex===0?1:0)+g.querySelectorAll('[tabindex=\"0\"]').length).toString();}})()"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Interaction_Descendant_Focusability_Changes_Then_Exactly_One_Tab_Stop_Remains()
		{
			var first = new Button { Content = "First" };
			var second = new Button { Content = "Second", IsTabStop = false };
			var cell = new GridCellContentControl(new StackPanel { Children = { first, second } });
			var grid = CreateGridScope(cell);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(second), timeoutMS: 5000, message: "Timed out waiting for interaction descendants.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(first)}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''") == GetSemanticElementId(first),
				timeoutMS: 10000,
				message: "The first interaction descendant did not receive browser focus.");
			second.IsTabStop = true;
			Assert.AreEqual("0", GetSemanticAttribute(first, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(second, "tabindex"));

			first.IsTabStop = false;
			await UITestHelper.WaitFor(() => InvokeBrowserJs("document.activeElement ? document.activeElement.id : ''") == GetSemanticElementId(second), timeoutMS: 10000,
				message: "Disabling the active interaction descendant did not focus its eligible sibling.");
			Assert.AreEqual("-1", GetSemanticAttribute(first, "tabindex"));
			Assert.AreEqual("0", GetSemanticAttribute(second, "tabindex"));
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(grid)}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Structural_Row_Becomes_TabStop_Then_It_Remains_Outside_Tab_Order()
		{
			var cell = new GridCellControl();
			var row = new FocusableGridRowControl(new Grid { Children = { cell } });
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 5000, message: "Timed out waiting for the structural-row grid.");

			row.IsTabStop = true;
			Assert.AreEqual("-1", GetSemanticAttribute(row, "tabindex"));
			Assert.AreEqual("0", GetSemanticAttribute(cell, "tabindex"));
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(grid)}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Cell_Is_Added_Under_Collapsed_Row_Then_It_Emits_Only_After_Row_Is_Shown()
		{
			var row = new GridRowControl { Visibility = Visibility.Collapsed };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(grid), timeoutMS: 5000, message: "Timed out waiting for the collapsed-row grid.");

			var cell = new GridCellControl();
			row.Children.Add(cell);
			await UITestHelper.WaitForIdle();
			Assert.IsFalse(SemanticElementExists(cell));
			Assert.AreEqual("0", GetSemanticAttribute(grid, "tabindex"));

			row.Visibility = Visibility.Visible;
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 3000, message: "Showing the row did not emit its previously-pruned cell subtree.");
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(grid)}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Row_DataContext_Is_Recycled_Then_Cell_Metadata_Refreshes_On_The_Same_Handle()
		{
			var cell = new RecycledGridCellControl();
			var row = new GridRowControl { DataContext = "First", Children = { cell } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => GetSemanticAttribute(cell, "aria-label") == "First", timeoutMS: 5000, message: "Timed out waiting for the initial recycled-cell label.");
			var semanticId = GetSemanticElementId(cell);

			row.DataContext = "Second";
			await UITestHelper.WaitFor(() => GetSemanticAttribute(cell, "aria-label") == "Second", timeoutMS: 3000, message: "DataContext recycling did not refresh the realized cell metadata.");
			Assert.AreEqual(semanticId, GetSemanticElementId(cell));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Provider_Counts_Change_Without_Visual_Mutation_Then_Grid_Counts_Refresh()
		{
			var grid = new GridContainerControl { RowCountValue = 10, ColumnCountValue = 2 };

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => GetSemanticAttribute(grid, "aria-rowcount") == "10", timeoutMS: 5000, message: "Timed out waiting for the initial provider counts.");

			grid.RowCountValue = 12;
			grid.ColumnCountValue = 4;
			grid.InvalidateArrange();
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(grid, "aria-rowcount") == "12" && GetSemanticAttribute(grid, "aria-colcount") == "4",
				timeoutMS: 3000,
				message: "Provider-only row/column count changes did not refresh ARIA counts.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Provider_Cell_Metadata_Changes_Without_Recycling_Then_Aria_Metadata_Refreshes()
		{
			var cell = new MutableIndexGridCellControl(row: 1, column: 0) { RowSpan = 2, ColumnSpan = 3 };
			var grid = CreateGridScope(cell);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => GetSemanticAttribute(cell, "aria-rowindex") == "3", timeoutMS: 5000, message: "Timed out waiting for the initial provider index.");
			Assert.AreEqual("2", GetSemanticAttribute(cell, "aria-rowspan"));
			Assert.AreEqual("3", GetSemanticAttribute(cell, "aria-colspan"));

			cell.Row = 5;
			cell.RowSpan = 1;
			cell.ColumnSpan = 1;
			grid.InvalidateArrange();
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(cell, "aria-rowindex") == "7" &&
					!SemanticElementHasAttribute(cell, "aria-rowspan") &&
					!SemanticElementHasAttribute(cell, "aria-colspan"),
				timeoutMS: 3000,
				message: "Provider-only realized cell metadata did not refresh indices and remove default spans.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Row_Index_Uses_Realized_Cell_Then_RowsPresenter_Peers_Are_Not_Enumerated()
		{
			var cell = new MutableIndexGridCellControl(row: 4, column: 0);
			var row = new NonEnumeratingGridRowControl { Children = { cell } };
			var presenter = new CountingRowsPresenterControl { Children = { row } };
			var grid = CreateGridScope(presenter);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => GetSemanticAttribute(row, "aria-rowindex") == "5", timeoutMS: 5000,
				message: "The realized row index was not resolved from its visual cell provider.");

			Assert.AreEqual(0, presenter.AutomationChildrenReadCount,
				"Row-index resolution must not enumerate a rows-presenter peer that materializes the full item source.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Provider_Sort_Changes_Without_Automation_Event_Then_Aria_Sort_Refreshes()
		{
			var header = new MutableColumnHeaderControl();
			var grid = CreateGridScope(header);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var peer = (MutableColumnHeaderPeer)header.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(header), timeoutMS: 5000, message: "Timed out waiting for the provider-snapshot header.");

			peer.SetItemStatusWithoutEvent("Ascending");
			await UITestHelper.WaitFor(() => GetSemanticAttribute(header, "aria-sort") == "ascending", timeoutMS: 3000,
				message: "Provider-only sort change did not refresh aria-sort.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Provider_SelectionMultiplicity_Changes_Without_Event_Then_Aria_Refreshes()
		{
			var grid = new GridContainerControl { CanSelectMultipleValue = true };

			await UITestHelper.Load(grid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => GetSemanticAttribute(grid, "aria-multiselectable") == "true", timeoutMS: 5000,
				message: "Timed out waiting for initial multiselectable state.");

			grid.CanSelectMultipleValue = false;
			await UITestHelper.WaitFor(() => GetSemanticAttribute(grid, "aria-multiselectable") == "false", timeoutMS: 3000,
				message: "Provider-only selection multiplicity change did not refresh aria-multiselectable.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Nested_Grid_Is_Disabled_And_Reenabled_Then_Exactly_One_Tab_Stop_Remains()
		{
			var innerCell = new GridCellControl();
			var innerGrid = CreateGridScope(innerCell);
			var outerCell = new GridCellContentControl(innerGrid);
			var outerGrid = CreateGridScope(outerCell);

			await UITestHelper.Load(outerGrid, x => x.IsLoaded);
			var innerPeer = innerGrid.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(
				() => SemanticElementExists(innerCell) && SemanticElementExists(outerCell),
				timeoutMS: 10000,
				message: "Timed out waiting for the nested grid semantic tree.");

			var outerGridId = GetSemanticElementId(outerGrid);
			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(innerCell)}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(innerCell, "tabindex") == "0" &&
					GetSemanticAttribute(outerCell, "tabindex") == "-1",
				timeoutMS: 10000,
				message: "The nested cell did not receive the initial grid tab stop.");

			innerGrid.IsEnabledValue = false;
			innerPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(innerGrid, "aria-disabled") == "true" &&
					GetSemanticAttribute(innerCell, "tabindex") == "-1" &&
					GetSemanticAttribute(outerCell, "tabindex") == "0" &&
					InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()") == "1",
				timeoutMS: 10000,
				message: "Disabling the nested grid did not transfer its single tab stop to the outer cell.");

			innerGrid.IsEnabledValue = true;
			innerPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, false, true);
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(innerGrid, "aria-disabled") == "false" &&
					GetSemanticAttribute(innerCell, "tabindex") == "-1" &&
					GetSemanticAttribute(outerCell, "tabindex") == "0" &&
					InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()") == "1",
				timeoutMS: 10000,
				message: "Re-enabling the nested grid changed the current outer tab stop unexpectedly.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(innerCell)}').focus(); 'ok'");
			await UITestHelper.WaitFor(
				() => GetSemanticAttribute(innerCell, "tabindex") == "0" &&
					GetSemanticAttribute(outerCell, "tabindex") == "-1" &&
					InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()") == "1",
				timeoutMS: 10000,
				message: "Refocusing the re-enabled nested cell did not restore exactly one nested tab stop.");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Outer_Grid_Is_Disabled_With_Nested_Focus_Then_All_Descendant_Tab_Stops_Are_Removed()
		{
			var innerCell = new GridCellControl();
			var innerGrid = CreateGridScope(innerCell);
			var outerCell = new GridCellContentControl(innerGrid);
			var outerGrid = CreateGridScope(outerCell);

			await UITestHelper.Load(outerGrid, x => x.IsLoaded);
			var outerPeer = outerGrid.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(innerCell), timeoutMS: 5000, message: "Timed out waiting for the outer-disable grid.");

			var outerGridId = GetSemanticElementId(outerGrid);
			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(innerCell)}').focus(); 'ok'");
			outerGrid.IsEnabledValue = false;
			outerPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(outerGrid, "aria-disabled") == "true", timeoutMS: 3000, message: "Outer grid did not become disabled.");

			Assert.AreEqual("0", InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
			Assert.AreEqual("-1", GetSemanticAttribute(outerCell, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(innerCell, "tabindex"));

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(innerCell)}').focus(); 'ok'");
			Assert.AreEqual("0", InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[tabindex=\"0\"]').length.toString()"));
			Assert.AreEqual("-1", GetSemanticAttribute(outerCell, "tabindex"));
			Assert.AreEqual("-1", GetSemanticAttribute(innerCell, "tabindex"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Last_Focused_Nested_Grid_Cell_Is_Removed_Then_Outer_Tab_Stop_Is_Restored()
		{
			var innerCell = new GridCellControl();
			var innerRow = new GridRowControl { Children = { innerCell } };
			var innerGrid = CreateGridScope(innerRow);
			var outerCell = new GridCellContentControl(innerGrid);
			var outerGrid = CreateGridScope(outerCell);

			await UITestHelper.Load(outerGrid, x => x.IsLoaded);
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(innerCell), timeoutMS: 5000, message: "Timed out waiting for the focused nested grid.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(innerCell)}').focus(); 'ok'");
			innerRow.Children.Remove(innerCell);
			await UITestHelper.WaitFor(() => !SemanticElementExists(innerCell), timeoutMS: 3000, message: "Nested gridcell semantic node was not removed.");

			var outerGridId = GetSemanticElementId(outerGrid);
			Assert.AreEqual("0", GetSemanticAttribute(outerCell, "tabindex"));
			Assert.AreEqual("1", InvokeBrowserJs($"document.getElementById('{outerGridId}').querySelectorAll('[role=gridcell][tabindex=\"0\"], [role=columnheader][tabindex=\"0\"], [role=rowheader][tabindex=\"0\"]').length.toString()"));
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_GridCell_Receives_AT_Click_Then_Selection_And_Invoke_Are_Routed()
		{
			var selectable = new MutableGridCellControl(row: 0, column: 0, isSelected: false);
			var invokable = new MutableGridCellControl(row: 0, column: 1, canInvoke: true);
			var row = new GridRowControl { Children = { selectable, invokable } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var selectablePeer = (MutableGridCellPeer)selectable.GetOrCreateAutomationPeer();
			var invokablePeer = (MutableGridCellPeer)invokable.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(invokable), timeoutMS: 5000, message: "Timed out waiting for the invokable gridcell semantic element.");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(selectable)}').click();'ok'");
			await UITestHelper.WaitFor(() => selectablePeer.IsSelected, timeoutMS: 3000, message: "AT click did not route to SelectionItem.Select().");

			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(invokable)}').click();'ok'");
			await UITestHelper.WaitFor(() => invokablePeer.InvokeCount == 1, timeoutMS: 3000, message: "AT click did not route to Invoke.Invoke().");
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Grid_Is_Disabled_Then_Synthetic_Cell_Activation_Is_Ignored()
		{
			var cell = new MutableGridCellControl(row: 0, column: 0, isSelected: false);
			var grid = CreateGridScope(cell);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var gridPeer = grid.GetOrCreateAutomationPeer();
			var cellPeer = (MutableGridCellPeer)cell.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 5000, message: "Timed out waiting for the disabled activation grid.");

			grid.IsEnabledValue = false;
			gridPeer.RaisePropertyChangedEvent(AutomationElementIdentifiers.IsEnabledProperty, true, false);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(grid, "aria-disabled") == "true", timeoutMS: 3000, message: "Grid did not become disabled.");
			InvokeBrowserJs($"document.getElementById('{GetSemanticElementId(cell)}').click(); 'ok'");
			Assert.IsFalse(cellPeer.IsSelected);
		}

		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_Detached_Semantic_Cell_Is_Clicked_Then_Stale_Callback_Is_Ignored()
		{
			var cell = new MutableGridCellControl(row: 0, column: 0, isSelected: false);
			var row = new GridRowControl { Children = { cell } };
			var grid = CreateGridScope(row);

			await UITestHelper.Load(grid, x => x.IsLoaded);
			var peer = (MutableGridCellPeer)cell.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();
			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(cell), timeoutMS: 5000, message: "Timed out waiting for the stale callback grid.");

			var cellId = GetSemanticElementId(cell);
			InvokeBrowserJs($"globalThis.__unoStaleGridCell = document.getElementById('{cellId}'); 'ok'");
			row.Children.Remove(cell);
			await UITestHelper.WaitFor(() => !SemanticElementExists(cell), timeoutMS: 3000, message: "Gridcell semantic node was not removed.");
			InvokeBrowserJs("globalThis.__unoStaleGridCell.click(); delete globalThis.__unoStaleGridCell; 'ok'");
			Assert.IsFalse(peer.IsSelected);
		}
#endif

#if HAS_UNO
		// Sized so the element lays out to a real (non-zero) visual that participates in the semantic
		// tree. A template-less Control otherwise measures to 0×0, which fails the default WaitForLoaded
		// size check — the DOM tests load with an explicit x => x.IsLoaded predicate for the same reason.
		private const double MockWidth = 120;
		private const double MockHeight = 32;

		private abstract partial class SizedMockControl : Control
		{
			protected SizedMockControl()
			{
				Width = MockWidth;
				Height = MockHeight;
			}

			protected override Size MeasureOverride(Size availableSize) => new Size(MockWidth, MockHeight);

			protected override Size ArrangeOverride(Size finalSize) => finalSize;
		}

		private static GridContainerControl CreateGridScope(UIElement child)
		{
			var grid = new GridContainerControl();
			UIElement gridChild = child;
			if (child.GetOrCreateAutomationPeer() is { } peer)
			{
				if (peer.GetAutomationControlType() is AutomationControlType.HeaderItem)
				{
					gridChild = new HeaderRowControl(child);
				}
				else if (peer.GetPattern(PatternInterface.GridItem) is IGridItemProvider)
				{
					var headerRow = new HeaderRowControl(
						new ColumnHeaderControl(),
						new ColumnHeaderControl(),
						new ColumnHeaderControl());
					grid.Children.Add(headerRow);
					RegisterHeaders(grid, headerRow);
					gridChild = new GridRowControl { Children = { child } };
				}
			}

			grid.Children.Add(gridChild);
			RegisterHeaders(grid, gridChild);
			return grid;
		}

		private static void RegisterHeaders(GridContainerControl grid, UIElement element)
		{
			if (element is RowHeaderControl rowHeader)
			{
				grid.RowHeaders.Add(rowHeader);
			}
			else if (element.GetOrCreateAutomationPeer()?.GetAutomationControlType() is AutomationControlType.HeaderItem &&
				element is FrameworkElement columnHeader)
			{
				grid.ColumnHeaders.Add(columnHeader);
			}

			if (element is Panel panel)
			{
				foreach (var child in panel.Children)
				{
					RegisterHeaders(grid, child);
				}
			}
		}

		private static T FindAncestor<T>(DependencyObject owner)
			where T : DependencyObject
		{
			for (var current = owner.GetParent(); current is not null; current = current.GetParent())
			{
				if (current is T match)
				{
					return match;
				}
			}

			return default;
		}

		private static AutomationPeer GetContainingGridPeer(DependencyObject owner)
			=> FindAncestor<GridContainerControl>(owner) is { } grid
				? FrameworkElementAutomationPeer.CreatePeerForElement(grid)
				: null;
		// A control whose peer matches the toolkit's DataGridAutomationPeer shape: DataGrid control
		// type exposing Grid (counts) + Selection (multi-select). Counts/selection are configurable so
		// the same mock covers the multi-select, single-select and empty-grid cases.
		private sealed partial class GridContainerControl : Grid
		{
			public int RowCountValue { get; set; } = 500;
			public int ColumnCountValue { get; set; } = 3;
			public bool CanSelectMultipleValue { get; set; } = true;
			public bool IsEnabledValue { get; set; } = true;
			public List<FrameworkElement> ColumnHeaders { get; } = new();
			public List<FrameworkElement> RowHeaders { get; } = new();

			public GridContainerControl()
			{
				Width = MockWidth;
				Height = MockHeight;
				IsTabStop = true;
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new GridPeer(this);
		}

		private sealed partial class GridPeer : FrameworkElementAutomationPeer, IGridProvider, ISelectionProvider, ITableProvider
		{
			private readonly GridContainerControl _owner;

			public GridPeer(GridContainerControl owner) : base(owner) => _owner = owner;

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.DataGrid;

			protected override bool IsEnabledCore() => _owner.IsEnabledValue;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.Grid or PatternInterface.Selection or PatternInterface.Table
					? this
					: base.GetPatternCore(patternInterface);

			public int RowCount => _owner.RowCountValue;
			public int ColumnCount => _owner.ColumnCountValue;
			public IRawElementProviderSimple GetItem(int row, int column) => null;

			public bool CanSelectMultiple => _owner.CanSelectMultipleValue;
			public bool IsSelectionRequired => false;
			public IRawElementProviderSimple[] GetSelection() => System.Array.Empty<IRawElementProviderSimple>();

			public Microsoft.UI.Xaml.Automation.RowOrColumnMajor RowOrColumnMajor
				=> Microsoft.UI.Xaml.Automation.RowOrColumnMajor.RowMajor;
			public IRawElementProviderSimple[] GetColumnHeaders() => GetHeaderProviders(_owner.ColumnHeaders);
			public IRawElementProviderSimple[] GetRowHeaders() => GetHeaderProviders(_owner.RowHeaders);

			private IRawElementProviderSimple[] GetHeaderProviders(List<FrameworkElement> headers)
			{
				var providers = new List<IRawElementProviderSimple>();
				foreach (var header in headers)
				{
					if (FrameworkElementAutomationPeer.CreatePeerForElement(header) is { } peer)
					{
						providers.Add(ProviderFromPeer(peer));
					}
				}

				return providers.ToArray();
			}
		}

		private sealed partial class CalendarGridControl : Grid
		{
			public CalendarGridControl(UIElement item)
			{
				Width = MockWidth;
				Height = MockHeight;
				Children.Add(item);
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new CalendarGridPeer(this);
		}

		private sealed partial class CalendarGridPeer : FrameworkElementAutomationPeer
		{
			public CalendarGridPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Calendar;
		}

		private sealed partial class CalendarGridItemControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new CalendarGridItemPeer(this);
		}

		private sealed partial class CalendarGridItemPeer : FrameworkElementAutomationPeer, IGridItemProvider
		{
			public CalendarGridItemPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem ? this : base.GetPatternCore(patternInterface);

			public int Row => 0;
			public int Column => 0;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid
				=> FindAncestor<CalendarGridControl>(Owner) is { } calendar
					? ProviderFromPeer(FrameworkElementAutomationPeer.CreatePeerForElement(calendar))
					: null;
		}

		private sealed partial class NullContainingGridItemControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new NullContainingGridItemPeer(this);
		}

		private sealed partial class NullContainingGridItemPeer : FrameworkElementAutomationPeer, IGridItemProvider
		{
			public NullContainingGridItemPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem ? this : base.GetPatternCore(patternInterface);

			public int Row => 0;
			public int Column => 0;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid => null;
		}

		// A control whose peer matches the toolkit's DataGridColumnHeaderAutomationPeer shape.
		private sealed partial class ColumnHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new ColumnHeaderPeer(this);
		}

		private partial class ColumnHeaderPeer : FrameworkElementAutomationPeer
		{
			public ColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.HeaderItem;
		}

		// Toolkit DataGridColumnHeaderAutomationPeer reports sort state through HelpText.
		private sealed partial class SortedColumnHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new SortedColumnHeaderPeer(this);
		}

		private sealed partial class SortedColumnHeaderPeer : ColumnHeaderPeer
		{
			public SortedColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override string GetHelpTextCore() => "Ascending";
		}

		private sealed partial class DescendingColumnHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new DescendingColumnHeaderPeer(this);
		}

		private sealed partial class DescendingColumnHeaderPeer : ColumnHeaderPeer
		{
			public DescendingColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override string GetHelpTextCore() => "Descending";
		}

		// A header reporting a non-sort ItemStatus: must not be mis-mapped to aria-sort.
		private sealed partial class BusyColumnHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new BusyColumnHeaderPeer(this);
		}

		private sealed partial class BusyColumnHeaderPeer : ColumnHeaderPeer
		{
			public BusyColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override string GetItemStatusCore() => "Busy";
		}

		private sealed partial class MutableColumnHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new MutableColumnHeaderPeer(this);
		}

		private sealed partial class MutableColumnHeaderPeer : ColumnHeaderPeer
		{
			private string _itemStatus = string.Empty;

			public MutableColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override string GetItemStatusCore() => _itemStatus;

			public void SetItemStatus(string value)
			{
				var oldValue = _itemStatus;
				_itemStatus = value;
				RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, oldValue, value);
			}

			public void SetItemStatusFromAutomationEvent(string value)
			{
				_itemStatus = value;
				RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
			}

			public void SetItemStatusWithoutEvent(string value) => _itemStatus = value;
		}

		// A control whose peer matches the toolkit's DataGridItemAutomationPeer (row) shape: DataItem
		// control type exposing SelectionItem, with no reported position.
		private sealed partial class GridRowControl : Grid
		{
			public GridRowControl()
			{
				Width = MockWidth;
				Height = MockHeight;
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new GridRowPeer(this);
		}

		private sealed partial class NonEnumeratingGridRowControl : Grid
		{
			public NonEnumeratingGridRowControl()
			{
				Width = MockWidth;
				Height = MockHeight;
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new NonEnumeratingGridRowPeer(this);
		}

		private sealed partial class NonEnumeratingGridRowPeer : GridRowPeer
		{
			public NonEnumeratingGridRowPeer(FrameworkElement owner) : base(owner) { }

			protected override IList<AutomationPeer> GetChildrenCore() => new List<AutomationPeer>();
		}

		private sealed partial class CountingRowsPresenterControl : Grid
		{
			public int AutomationChildrenReadCount { get; set; }

			protected override AutomationPeer OnCreateAutomationPeer() => new CountingRowsPresenterPeer(this);
		}

		private sealed partial class CountingRowsPresenterPeer : FrameworkElementAutomationPeer
		{
			public CountingRowsPresenterPeer(CountingRowsPresenterControl owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

			protected override IList<AutomationPeer> GetChildrenCore()
			{
				((CountingRowsPresenterControl)Owner).AutomationChildrenReadCount++;
				return new List<AutomationPeer>();
			}
		}

		private sealed partial class FocusableGridRowControl : ContentControl
		{
			public FocusableGridRowControl(UIElement content)
			{
				Width = MockWidth;
				Height = MockHeight;
				IsTabStop = false;
				Content = content;
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new FocusableGridRowPeer(this);
		}

		private sealed partial class FocusableGridRowPeer : FrameworkElementAutomationPeer
		{
			public FocusableGridRowPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;
		}

		private sealed partial class RowHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new RowHeaderPeer(this);
		}

		private sealed partial class RowHeaderPeer : FrameworkElementAutomationPeer
		{
			public RowHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.HeaderItem;
		}

		private partial class GridRowPeer : FrameworkElementAutomationPeer
		{
			public GridRowPeer(FrameworkElement owner) : base(owner)
				=> EventsSource = new GridRowItemPeer(owner, isSelected: false);

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.DataItem;

		}

		private sealed partial class GridRowItemPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
		{
			public GridRowItemPeer(FrameworkElement owner, bool isSelected) : base(owner) => IsSelected = isSelected;

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.DataItem;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.SelectionItem
					? this
					: base.GetPatternCore(patternInterface);

			public bool IsSelected { get; }
			public IRawElementProviderSimple SelectionContainer => null;
			public void AddToSelection() { }
			public void RemoveFromSelection() { }
			public void Select() { }
		}

		// A row that DOES report a position — exercises the aria-rowindex positive branch.
		private sealed partial class PositionedGridRowControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new PositionedGridRowPeer(this);
		}

		private sealed partial class PositionedGridRowPeer : GridRowPeer
		{
			public PositionedGridRowPeer(FrameworkElement owner) : base(owner) { }

			protected override int GetPositionInSetCore() => 7;
		}

		// A control whose peer matches the toolkit's DataGridCellAutomationPeer shape: Custom control
		// type, exposing GridItem + SelectionItem.
		private sealed partial class GridCellControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new GridCellPeer(this);
		}

		private sealed partial class GridCellContentControl : ContentControl
		{
			public GridCellContentControl(UIElement content)
			{
				Width = MockWidth;
				Height = MockHeight;
				Content = content;
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new GridCellPeer(this);
		}

		private sealed partial class TypedGridCellControl : SizedMockControl
		{
			private readonly AutomationControlType _controlType;

			public TypedGridCellControl(AutomationControlType controlType) => _controlType = controlType;

			protected override AutomationPeer OnCreateAutomationPeer() => new TypedGridCellPeer(this, _controlType);
		}

		private sealed partial class TypedGridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider
		{
			private readonly AutomationControlType _controlType;

			public TypedGridCellPeer(FrameworkElement owner, AutomationControlType controlType) : base(owner)
				=> _controlType = controlType;

			protected override AutomationControlType GetAutomationControlTypeCore() => _controlType;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem
					? this
					: base.GetPatternCore(patternInterface);

			public int Row => 0;
			public int Column => 0;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid => ProviderFromPeer(GetContainingGridPeer(Owner));
		}

		private sealed partial class HeaderRowControl : Grid
		{
			public HeaderRowControl(params UIElement[] headers)
				: this(allowEmpty: false, headers)
			{
			}

			public HeaderRowControl(bool allowEmpty, params UIElement[] headers)
			{
				Width = MockWidth;
				Height = MockHeight;
				AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
				if (!allowEmpty && headers.Length == 0)
				{
					headers = new UIElement[] { new ColumnHeaderControl(), new ColumnHeaderControl() };
				}

				foreach (var header in headers)
				{
					Children.Add(header);
				}
			}

			protected override AutomationPeer OnCreateAutomationPeer() => new HeaderRowPeer(this);
		}

		private sealed partial class RecycledGridCellControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new RecycledGridCellPeer(this);
		}

		private sealed partial class MutableIndexGridCellControl : SizedMockControl
		{
			public MutableIndexGridCellControl(int row, int column)
			{
				Row = row;
				Column = column;
			}

			public int Row { get; set; }
			public int Column { get; set; }
			public int RowSpan { get; set; } = 1;
			public int ColumnSpan { get; set; } = 1;

			protected override AutomationPeer OnCreateAutomationPeer() => new MutableIndexGridCellPeer(this);
		}

		private sealed partial class MutableIndexGridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider
		{
			private readonly MutableIndexGridCellControl _owner;

			public MutableIndexGridCellPeer(MutableIndexGridCellControl owner) : base(owner) => _owner = owner;

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem ? this : base.GetPatternCore(patternInterface);

			public int Row => _owner.Row;
			public int Column => _owner.Column;
			public int RowSpan => _owner.RowSpan;
			public int ColumnSpan => _owner.ColumnSpan;
			public IRawElementProviderSimple ContainingGrid => ProviderFromPeer(GetContainingGridPeer(Owner));
		}

		private sealed partial class RecycledGridCellPeer : GridCellPeer
		{
			public RecycledGridCellPeer(FrameworkElement owner) : base(owner) { }

			protected override string GetNameCore() => Owner.DataContext?.ToString() ?? string.Empty;
		}

		private sealed partial class HeaderRowPeer : FrameworkElementAutomationPeer
		{
			public HeaderRowPeer(HeaderRowControl owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Header;

			protected override IList<AutomationPeer> GetChildrenCore()
			{
				var children = new List<AutomationPeer>();
				foreach (var child in ((HeaderRowControl)Owner).Children)
				{
					if (FrameworkElementAutomationPeer.CreatePeerForElement(child) is { } peer)
					{
						children.Add(peer);
					}
				}

				return children;
			}
		}

		private partial class GridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider, ISelectionItemProvider
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
			public IRawElementProviderSimple ContainingGrid => ProviderFromPeer(GetContainingGridPeer(Owner));

			public bool IsSelected => true;
			public IRawElementProviderSimple SelectionContainer => null;
			public void AddToSelection() { }
			public void RemoveFromSelection() { }
			public void Select() { }
		}

		// A cell exposing GridItem but NOT SelectionItem — its aria-selected must be omitted.
		private sealed partial class UnselectableGridCellControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new UnselectableGridCellPeer(this);
		}

		private sealed partial class UnselectableGridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider
		{
			public UnselectableGridCellPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.Custom;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem
					? this
					: base.GetPatternCore(patternInterface);

			public int Row => 0;
			public int Column => 2;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid => ProviderFromPeer(GetContainingGridPeer(Owner));
		}

		private sealed partial class ThrowingSelectionGridCellControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new ThrowingSelectionGridCellPeer(this);
		}

		private sealed partial class ThrowingSelectionGridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider, ISelectionItemProvider
		{
			public ThrowingSelectionGridCellPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem or PatternInterface.SelectionItem ? this : base.GetPatternCore(patternInterface);

			public int Row => 0;
			public int Column => 0;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid => ProviderFromPeer(GetContainingGridPeer(Owner));
			public bool IsSelected => throw new System.InvalidOperationException("Selection state is temporarily unavailable.");
			public IRawElementProviderSimple SelectionContainer => null;
			public void AddToSelection() { }
			public void RemoveFromSelection() { }
			public void Select() { }
		}

		// A cell whose selection can be toggled at runtime, raising the SelectionItem.IsSelected
		// property-changed event so the push-update path can be exercised.
		private sealed partial class MutableGridCellControl : SizedMockControl
		{
			public MutableGridCellControl(int row = 0, int column = 0, bool isSelected = true, bool canInvoke = false)
			{
				Row = row;
				Column = column;
				IsSelected = isSelected;
				CanInvoke = canInvoke;
			}

			public int Row { get; }
			public int Column { get; }
			public bool IsSelected { get; }
			public bool CanInvoke { get; }

			protected override AutomationPeer OnCreateAutomationPeer() => new MutableGridCellPeer(this);
		}

		private sealed partial class MutableGridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider, ISelectionItemProvider, IInvokeProvider
		{
			private bool _isSelected;
			private readonly MutableGridCellControl _owner;

			public MutableGridCellPeer(MutableGridCellControl owner) : base(owner)
			{
				_owner = owner;
				_isSelected = owner.IsSelected;
			}

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.Custom;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem or PatternInterface.SelectionItem ||
					patternInterface is PatternInterface.Invoke && _owner.CanInvoke
					? this
					: base.GetPatternCore(patternInterface);

			public int Row => _owner.Row;
			public int Column => _owner.Column;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid => ProviderFromPeer(GetContainingGridPeer(Owner));

			public bool IsSelected => _isSelected;
			public IRawElementProviderSimple SelectionContainer => null;
			public void AddToSelection() { }
			public void RemoveFromSelection() { }
			public void Select() => SetSelected(true);
			public int InvokeCount { get; private set; }
			public void Invoke() => InvokeCount++;

			public void SetSelected(bool value)
			{
				var old = _isSelected;
				_isSelected = value;
				RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, old, value);
			}

			public void SetSelectedFromAutomationEvent(bool value)
			{
				_isSelected = value;
				RaiseAutomationEvent(value
					? AutomationEvents.SelectionItemPatternOnElementSelected
					: AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
			}
		}
#endif
	}
}
