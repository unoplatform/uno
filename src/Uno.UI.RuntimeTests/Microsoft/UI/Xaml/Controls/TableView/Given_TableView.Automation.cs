#nullable enable

#pragma warning disable CS8305 // The Tabular types are [Experimental], as in WinUI

#if !WINAPPSDK

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

public partial class Given_TableView
{
	[TestMethod]
	public async Task When_Automation_Patterns()
	{
		var items = People(4);
		var table = CreateTable(items);
		await LoadAsync(table);

		var tablePeer = FrameworkElementAutomationPeer.CreatePeerForElement(table);
		Assert.IsInstanceOfType(tablePeer, typeof(TableViewAutomationPeer));
		Assert.AreEqual(AutomationControlType.DataGrid, tablePeer.GetAutomationControlType());
		Assert.AreEqual(typeof(TableView).FullName, tablePeer.GetClassName());

		// Grid + Table describe shape and are always offered; ItemContainer enumerates past realization.
		Assert.IsNotNull(tablePeer.GetPattern(PatternInterface.Grid));
		Assert.IsNotNull(tablePeer.GetPattern(PatternInterface.Table));
		Assert.IsNotNull(tablePeer.GetPattern(PatternInterface.ItemContainer));
		Assert.IsNotNull(tablePeer.GetPattern(PatternInterface.Selection), "selectable while SelectionMode is Single");

		var grid = (IGridProvider)tablePeer;
		Assert.AreEqual(items.Count, grid.RowCount);
		Assert.AreEqual(3, grid.ColumnCount);

		// Header peers are virtual and reached through the table provider.
		var headers = GetColumnHeaderPeers(table);
		Assert.AreEqual(3, headers.Count);
		Assert.AreEqual(AutomationControlType.HeaderItem, headers[0].GetAutomationControlType());
		Assert.AreEqual("TableViewColumnHeader", headers[0].GetClassName());
		Assert.AreEqual("Name", headers[0].GetName());

		// Invoking a sortable header sorts it.
		var invoke = (IInvokeProvider)headers[0].GetPattern(PatternInterface.Invoke)!;
		Assert.IsNotNull(invoke);
		invoke.Invoke();
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(SortDirection.Ascending, table.Columns[0].SortDirection);

		// Cells: structural patterns always, Value only where SetValue can honour it.
		var bridge = new PeerBridge(table);
		var cellPeer = bridge.From(grid.GetItem(0, 1))!;
		Assert.IsNotNull(cellPeer);
		Assert.AreEqual("TableViewCell", cellPeer.GetClassName());
		Assert.AreEqual(AutomationControlType.DataItem, cellPeer.GetAutomationControlType());
		Assert.IsNotNull(cellPeer.GetPattern(PatternInterface.GridItem));
		Assert.IsNotNull(cellPeer.GetPattern(PatternInterface.TableItem));
		Assert.IsNull(cellPeer.GetPattern(PatternInterface.Value), "a read-only table offers no Value pattern");

		var gridItem = (IGridItemProvider)cellPeer.GetPattern(PatternInterface.GridItem)!;
		Assert.AreEqual(0, gridItem.Row);
		Assert.AreEqual(1, gridItem.Column);

		table.IsReadOnly = false;
		await WindowHelper.WaitForIdle();
		cellPeer = bridge.From(grid.GetItem(0, 0))!;
		Assert.IsNotNull(cellPeer.GetPattern(PatternInterface.Value), "an editable text cell offers Value");

		// Rows are DataItems with SelectionItem while selection is on.
		var rowPeer = FrameworkElementAutomationPeer.CreatePeerForElement(GetRow(table, 1)!);
		Assert.IsInstanceOfType(rowPeer, typeof(TableViewRowAutomationPeer));
		Assert.AreEqual(AutomationControlType.DataItem, rowPeer.GetAutomationControlType());
		var selectionItem = (ISelectionItemProvider)rowPeer.GetPattern(PatternInterface.SelectionItem)!;
		Assert.IsNotNull(selectionItem);
		selectionItem.Select();
		Assert.AreEqual(1, table.SelectedIndex);
		Assert.IsTrue(selectionItem.IsSelected);

		// Selection is withdrawn when the control cannot select.
		table.SelectionMode = TableViewSelectionMode.None;
		Assert.IsNull(tablePeer.GetPattern(PatternInterface.Selection));
		Assert.IsNull(rowPeer.GetPattern(PatternInterface.SelectionItem));
	}

	[TestMethod]
	public async Task When_GroupHeader_Automation_ExpandCollapse()
	{
		var items = new ObservableCollection<Person>
		{
			new("Ada", 30, "Oslo"),
			new("Bob", 31, "Kyoto"),
			new("Cy", 32, "Oslo"),
		};
		var source = TableViewSource.From(items).GroupBy(new TableViewKeySelector(item => ((Person)item!).City));
		var table = CreateTable(source);
		await LoadAsync(table, height: 600);

		var header = GetRealizedGroupHeaders(table)[0];
		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(header);
		Assert.IsInstanceOfType(peer, typeof(TableViewGroupHeaderAutomationPeer));
		Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType());
		Assert.AreEqual("TableViewGroupHeader", peer.GetClassName());
		Assert.IsNotNull(peer.GetPattern(PatternInterface.GridItem));

		var expandCollapse = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)!;
		Assert.IsNotNull(expandCollapse);
		Assert.AreEqual(ExpandCollapseState.Expanded, expandCollapse.ExpandCollapseState);

		expandCollapse.Collapse();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(1, GetRealizedRows(table).Count, "only the Kyoto row remains");
		var oslo = GetRealizedGroupHeaders(table).Single(h => Equals(((TableViewGroupInfo)h.Content).Key, "Oslo"));
		Assert.IsFalse(oslo.IsExpanded);

		var osloPeer = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(oslo)!.GetPattern(PatternInterface.ExpandCollapse)!;
		Assert.AreEqual(ExpandCollapseState.Collapsed, osloPeer.ExpandCollapseState);
		osloPeer.Expand();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(3, GetRealizedRows(table).Count);
	}

	[TestMethod]
	public async Task When_CellToolTipBinding()
	{
		var items = new ObservableCollection<Person>(People(40));
		items[0].Notes = "First note";
		items[1].Notes = "";
		var table = CreateTable(items);
		var city = table.Columns[2];
		city.CellToolTipBinding = new Binding { Path = new PropertyPath(nameof(Person.Notes)) };
		await LoadAsync(table, height: 300);

		Assert.AreEqual("First note", GetCellToolTipText(GetCell(GetRow(table, 0)!, city)));
		Assert.IsNull(ToolTipService.GetToolTip(GetCell(GetRow(table, 1)!, city)), "an empty value means no tooltip");
		Assert.IsNull(ToolTipService.GetToolTip(GetCell(GetRow(table, 2)!, city)), "a null value means no tooltip");

		// Columns without a binding never get a control-owned tooltip.
		Assert.IsNull(ToolTipService.GetToolTip(GetCell(GetRow(table, 0)!, table.Columns[0])));

		// The binding tracks the item: no invalidation call needed.
		items[2].Notes = "Late note";
		await WindowHelper.WaitForIdle();
		Assert.AreEqual("Late note", GetCellToolTipText(GetCell(GetRow(table, 2)!, city)));

		items[0].Notes = "";
		await WindowHelper.WaitForIdle();
		// The owned ToolTip is neutralized in place, not detached (TableViewToolTipHelpers.h ClearOwnedToolTip).
		var clearedCell = GetCell(GetRow(table, 0)!, city);
		Assert.IsNull(GetCellToolTipText(clearedCell), "clearing the value retracts the tooltip");
		Assert.IsFalse(((ToolTip)ToolTipService.GetToolTip(clearedCell)).IsEnabled, "the retracted tooltip is disabled");
		Assert.AreEqual("", AutomationProperties.GetHelpText(clearedCell), "the published HelpText is retracted");

		// Recycled rows carry no stale tooltip from the item they showed before.
		items[0].Notes = "First note";
		var scroller = GetBodyScroller(table);
		scroller.ChangeView(null, scroller.ScrollableHeight, null, true);
		await WindowHelper.WaitForIdle();
		await WindowHelper.WaitForIdle();

		foreach (var row in GetRealizedRows(table))
		{
			var person = (Person)row.DataContext;
			var tip = GetCellToolTipText(GetCell(row, city));
			Assert.AreEqual(string.IsNullOrEmpty(person.Notes) ? null : person.Notes, tip, $"row for {person.Name}");
		}

		// Removing the binding retracts every control-owned tooltip.
		scroller.ChangeView(null, 0, null, true);
		await WindowHelper.WaitForIdle();
		city.CellToolTipBinding = null;
		await WindowHelper.WaitForIdle();

		Assert.IsTrue(GetRealizedRows(table).All(r => ToolTipService.GetToolTip(GetCell(r, city)) is null));
	}

	[TestMethod]
	public async Task When_App_ToolTip_In_Cell_Content_Is_Not_Touched()
	{
		var items = new List<Person> { new("Ada", 30, "Oslo", "Note") };
		var table = new TableView();
		var templateColumn = new TableViewTemplateColumn
		{
			Header = "Notes",
			CellTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<TextBlock Text="{Binding Notes}" ToolTipService.ToolTip="APP-OWNED" />
				</DataTemplate>
				"""),
			CellToolTipBinding = new Binding { Path = new PropertyPath(nameof(Person.Name)) },
		};
		table.Columns.Add(templateColumn);
		table.ItemsSource = items;
		await LoadAsync(table);

		var cell = GetCell(GetRow(table, 0)!, templateColumn);
		var appTextBlock = Descendants(cell).OfType<TextBlock>().First(t => t.Text == "Note");

		Assert.AreEqual("APP-OWNED", ToolTipService.GetToolTip(appTextBlock));
		Assert.AreEqual("Ada", GetCellToolTipText(cell), "the control's tooltip lives on the wrapper, not the content");

		templateColumn.CellToolTipBinding = null;
		await WindowHelper.WaitForIdle();

		cell = GetCell(GetRow(table, 0)!, templateColumn);
		appTextBlock = Descendants(cell).OfType<TextBlock>().First(t => t.Text == "Note");
		Assert.AreEqual("APP-OWNED", ToolTipService.GetToolTip(appTextBlock));
	}

	private sealed class PeerBridge : FrameworkElementAutomationPeer
	{
		public PeerBridge(FrameworkElement owner) : base(owner)
		{
		}

		public AutomationPeer? From(IRawElementProviderSimple provider) => PeerFromProvider(provider);
	}

	private static List<AutomationPeer> GetColumnHeaderPeers(TableView table)
	{
		var tablePeer = (ITableProvider)FrameworkElementAutomationPeer.CreatePeerForElement(table);
		var bridge = new PeerBridge(table);
		return tablePeer.GetColumnHeaders().Select(p => bridge.From(p)!).ToList();
	}

	private static string? GetCellToolTipText(FrameworkElement cell)
		=> ToolTipService.GetToolTip(cell) switch
		{
			null => null,
			ToolTip tip => tip.Content as string,
			string text => text,
			var other => other.ToString(),
		};
}

#endif
