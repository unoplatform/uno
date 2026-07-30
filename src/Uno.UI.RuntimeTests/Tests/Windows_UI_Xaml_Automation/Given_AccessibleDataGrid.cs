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
			await UITestHelper.Load(control, x => x.IsLoaded);

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
			await UITestHelper.Load(control, x => x.IsLoaded);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
			Assert.IsNotNull(peer);

			Assert.AreEqual(
				SemanticElementType.GridCell,
				AriaMapper.GetSemanticElementType(peer),
				"A Custom peer exposing the GridItem pattern must map to GridCell (role=gridcell), not a role-less generic node.");
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

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"), "A HeaderItem peer must emit role=columnheader (not the invalid 'headeritem').");
		}

		/// <summary>
		/// Issue #4 (sort, emission path): the column header surfaces aria-sort from the generic
		/// ItemStatus channel. WCT 7.1.205 leaves this empty (upstream-blocked), so the test drives a
		/// peer that reports an "Ascending" status to prove the emission path the fix added is wired.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Sorted_Then_Dom_Emits_Aria_Sort()
		{
			var control = new SortedColumnHeaderControl();

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the sorted column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"));
			Assert.AreEqual("ascending", GetSemanticAttribute(control, "aria-sort"), "A column header reporting an ascending ItemStatus must emit aria-sort=ascending.");
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

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the grid-cell semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("gridcell", GetSemanticAttribute(control, "role"), "A GridItem peer must emit role=gridcell.");
			// GridItemProvider is 0-based (Row=4, Column=1); ARIA aria-rowindex/aria-colindex are 1-based.
			Assert.AreEqual("5", GetSemanticAttribute(control, "aria-rowindex"), "aria-rowindex must be IGridItemProvider.Row + 1.");
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

			await UITestHelper.Load(control, x => x.IsLoaded);
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
		/// Issue #5 hygiene: a single-selection grid must NOT advertise aria-multiselectable (the
		/// attribute is emitted only for multi-select), while still being a role=grid with counts.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_SingleSelect_Grid_Then_No_Multiselectable()
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
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-multiselectable"), "A single-selection grid must NOT emit aria-multiselectable.");
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

			await UITestHelper.Load(control, x => x.IsLoaded);
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
		/// aria-colindex — they appear only when a sort status / column index is actually available.
		/// </summary>
		[TestMethod]
		[RunsOnUIThread]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
		public async Task When_ColumnHeader_Unsorted_Then_No_Sort_Or_ColIndex()
		{
			var control = new ColumnHeaderControl();

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the plain column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"));
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-sort"), "An unsorted header must NOT emit aria-sort.");
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-colindex"), "A header without a GridItem column must NOT emit aria-colindex.");
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

			await UITestHelper.Load(control, x => x.IsLoaded);
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

			await UITestHelper.Load(control, x => x.IsLoaded);
			control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the busy column-header semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("columnheader", GetSemanticAttribute(control, "role"));
			Assert.IsFalse(SemanticElementHasAttribute(control, "aria-sort"), "A non-sort ItemStatus must NOT produce aria-sort.");
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

			await UITestHelper.Load(control, x => x.IsLoaded);
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

			await UITestHelper.Load(control, x => x.IsLoaded);
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

			await UITestHelper.Load(control, x => x.IsLoaded);
			var peer = (MutableGridCellPeer)control.GetOrCreateAutomationPeer();
			await TestServices.WindowHelper.WaitForIdle();

			EnableAccessibilityThroughDom();
			await UITestHelper.WaitFor(() => SemanticElementExists(control), timeoutMS: 5000, message: "Timed out waiting for the mutable grid-cell semantic element.");
			await UITestHelper.WaitForIdle();

			Assert.AreEqual("true", GetSemanticAttribute(control, "aria-selected"), "Cell starts selected.");

			peer.SetSelected(false);
			await UITestHelper.WaitFor(() => GetSemanticAttribute(control, "aria-selected") == "false", timeoutMS: 3000, message: "aria-selected did not update to false after a runtime selection change.");
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

		// A control whose peer matches the toolkit's DataGridAutomationPeer shape: DataGrid control
		// type exposing Grid (counts) + Selection (multi-select). Counts/selection are configurable so
		// the same mock covers the multi-select, single-select and empty-grid cases.
		private sealed partial class GridContainerControl : SizedMockControl
		{
			public int RowCountValue { get; init; } = 500;
			public int ColumnCountValue { get; init; } = 3;
			public bool CanSelectMultipleValue { get; init; } = true;

			protected override AutomationPeer OnCreateAutomationPeer() => new GridPeer(this);
		}

		private sealed partial class GridPeer : FrameworkElementAutomationPeer, IGridProvider, ISelectionProvider
		{
			private readonly GridContainerControl _owner;

			public GridPeer(GridContainerControl owner) : base(owner) => _owner = owner;

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.DataGrid;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.Grid or PatternInterface.Selection
					? this
					: base.GetPatternCore(patternInterface);

			public int RowCount => _owner.RowCountValue;
			public int ColumnCount => _owner.ColumnCountValue;
			public IRawElementProviderSimple GetItem(int row, int column) => null;

			public bool CanSelectMultiple => _owner.CanSelectMultipleValue;
			public bool IsSelectionRequired => false;
			public IRawElementProviderSimple[] GetSelection() => System.Array.Empty<IRawElementProviderSimple>();
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

		// A column header reporting an ascending sort via the generic ItemStatus channel (the only
		// channel the fix can read, since the WCT header peer has no UIA sort pattern).
		private sealed partial class SortedColumnHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new SortedColumnHeaderPeer(this);
		}

		private sealed partial class SortedColumnHeaderPeer : ColumnHeaderPeer
		{
			public SortedColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override string GetItemStatusCore() => "Ascending";
		}

		private sealed partial class DescendingColumnHeaderControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new DescendingColumnHeaderPeer(this);
		}

		private sealed partial class DescendingColumnHeaderPeer : ColumnHeaderPeer
		{
			public DescendingColumnHeaderPeer(FrameworkElement owner) : base(owner) { }

			protected override string GetItemStatusCore() => "Descending";
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

		// A control whose peer matches the toolkit's DataGridItemAutomationPeer (row) shape: DataItem
		// control type exposing SelectionItem, with no reported position.
		private sealed partial class GridRowControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new GridRowPeer(this);
		}

		private partial class GridRowPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
		{
			public GridRowPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.DataItem;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.SelectionItem
					? this
					: base.GetPatternCore(patternInterface);

			public virtual bool IsSelected => false;
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
			public IRawElementProviderSimple ContainingGrid => null;
		}

		// A cell whose selection can be toggled at runtime, raising the SelectionItem.IsSelected
		// property-changed event so the push-update path can be exercised.
		private sealed partial class MutableGridCellControl : SizedMockControl
		{
			protected override AutomationPeer OnCreateAutomationPeer() => new MutableGridCellPeer(this);
		}

		private sealed partial class MutableGridCellPeer : FrameworkElementAutomationPeer, IGridItemProvider, ISelectionItemProvider
		{
			private bool _isSelected = true;

			public MutableGridCellPeer(FrameworkElement owner) : base(owner) { }

			protected override AutomationControlType GetAutomationControlTypeCore()
				=> AutomationControlType.Custom;

			protected override object GetPatternCore(PatternInterface patternInterface)
				=> patternInterface is PatternInterface.GridItem or PatternInterface.SelectionItem
					? this
					: base.GetPatternCore(patternInterface);

			public int Row => 0;
			public int Column => 0;
			public int RowSpan => 1;
			public int ColumnSpan => 1;
			public IRawElementProviderSimple ContainingGrid => null;

			public bool IsSelected => _isSelected;
			public IRawElementProviderSimple SelectionContainer => null;
			public void AddToSelection() { }
			public void RemoveFromSelection() { }
			public void Select() { }

			public void SetSelected(bool value)
			{
				var old = _isSelected;
				_isSelected = value;
				RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, old, value);
			}
		}
#endif
	}
}
