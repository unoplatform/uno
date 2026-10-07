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
using Uno.UI.Helpers.WinUI;
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

	[TestMethod]
	public async Task When_Header_Is_Not_A_String()
	{
		var table = new TableView();
		var intColumn = TextColumn(nameof(Person.Age));
		intColumn.Header = 42;
		var captionColumn = TextColumn(nameof(Person.Name));
		captionColumn.Header = new HeaderCaption("Full name");
		var elementColumn = TextColumn(nameof(Person.City));
		elementColumn.Header = new TextBlock { Text = "City" };
		table.Columns.Add(intColumn);
		table.Columns.Add(captionColumn);
		table.Columns.Add(elementColumn);
		table.ItemsSource = People(3);
		await LoadAsync(table);

		// Managed headers are IStringable through their CCW in WinUI, so ToString() names them;
		// a framework element is native there and is not.
		AssertHeaderName(intColumn, "42");
		AssertHeaderName(captionColumn, "Full name");
		AssertHeaderName(elementColumn, "");

		void AssertHeaderName(TableViewColumn column, string expected)
		{
			var headerCell = GetHeaderCell(table, column);
			Assert.AreEqual(expected, AutomationProperties.GetName(headerCell), "header cell name");

			var gripper = FindGripper(headerCell) as global::Microsoft.UI.Private.Controls.ResizeGripper;
			Assert.IsNotNull(gripper);
			Assert.AreEqual(expected, gripper.OwnerName, "gripper OwnerName");
		}
	}

	private sealed class HeaderCaption(string caption)
	{
		public override string ToString() => caption;
	}

#if __SKIA__
	[TestMethod]
	public async Task When_Selection_Raises_Automation_Events()
	{
		var items = People(5);
		var table = CreateTable(items);
		await LoadAsync(table);

		var tablePeer = FrameworkElementAutomationPeer.CreatePeerForElement(table);
		var row0Peer = FrameworkElementAutomationPeer.CreatePeerForElement(GetRow(table, 0)!);
		var row1Peer = FrameworkElementAutomationPeer.CreatePeerForElement(GetRow(table, 1)!);

		using var listener = RecordingAutomationListener.Install();

		table.Select(0);
		listener.Events.Clear();

		table.Select(1);

		// RaiseSelectionAutomationEvents: container first, then the per-row pattern events, then the
		// IsSelected property changes (selected row first).
		CollectionAssert.AreEqual(
			new[]
			{
				(tablePeer, "SelectionPatternOnInvalidated"),
				(row1Peer, "SelectionItemPatternOnElementSelected"),
				(row0Peer, "SelectionItemPatternOnElementRemovedFromSelection"),
				(row1Peer, "IsSelected:False->True"),
				(row0Peer, "IsSelected:True->False"),
			},
			listener.Events.Where(IsSelectionEvent).ToArray());

		// FromElement is GetOrCreateAutomationPeer in WinUI (FrameworkElementAutomationPeer_partial.cpp),
		// so a realized row nobody queried still gets a peer and raises; the C++ comment claiming
		// otherwise does not match the framework.
		var row2 = GetRow(table, 2)!;
		listener.Events.Clear();

		table.Select(2);

		var row2Peer = FrameworkElementAutomationPeer.FromElement(row2);
		Assert.IsNotNull(row2Peer);
		CollectionAssert.AreEqual(
			new[]
			{
				(tablePeer, "SelectionPatternOnInvalidated"),
				(row2Peer, "SelectionItemPatternOnElementSelected"),
				(row1Peer, "SelectionItemPatternOnElementRemovedFromSelection"),
				(row2Peer, "IsSelected:False->True"),
				(row1Peer, "IsSelected:True->False"),
			},
			listener.Events.Where(IsSelectionEvent).ToArray());

		static bool IsSelectionEvent((AutomationPeer Peer, string Event) e)
			=> e.Event is "SelectionPatternOnInvalidated"
				or "SelectionItemPatternOnElementSelected"
				or "SelectionItemPatternOnElementRemovedFromSelection"
				|| e.Event.StartsWith("IsSelected:", System.StringComparison.Ordinal);
	}

	[TestMethod]
	public async Task When_Sort_Announces()
	{
		var table = CreateTable(People(6));
		var name = table.Columns[0];
		await LoadAsync(table);

		using var listener = RecordingAutomationListener.Install();

		// AnnounceSortChange: ActionCompleted / MostRecent on the table's own peer.
		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		AssertAnnouncement(ResourceAccessor.SR_TableViewSortedAscending, "Name");

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Descending));
		AssertAnnouncement(ResourceAccessor.SR_TableViewSortedDescending, "Name");

		Assert.IsTrue(table.SortByColumn(name, SortDirection.None));
		AssertAnnouncement(ResourceAccessor.SR_TableViewSortCleared, "Name");

		Assert.IsTrue(table.SortByColumn(name, SortDirection.Ascending));
		listener.TakeNotifications("TableViewSortChanged");

		Assert.IsTrue(table.ClearSort());
		AssertAnnouncement(ResourceAccessor.SR_TableViewSortClearedAll, null);

		await WindowHelper.WaitForIdle();

		void AssertAnnouncement(string resourceName, string? header)
		{
			var announcements = listener.TakeNotifications("TableViewSortChanged");
			Assert.AreEqual(1, announcements.Count, resourceName);
			var announcement = announcements[0];
			Assert.AreSame(FrameworkElementAutomationPeer.FromElement(table), announcement.Peer);
			Assert.AreEqual(AutomationNotificationKind.ActionCompleted, announcement.Kind);
			Assert.AreEqual(AutomationNotificationProcessing.MostRecent, announcement.Processing);

			var format = ResourceAccessor.GetLocalizedStringResource(resourceName);
			Assert.AreEqual(header is null ? format : StringUtil.FormatString(format, header), announcement.DisplayString);
		}
	}

	internal sealed record Notification(AutomationPeer Peer, AutomationNotificationKind Kind, AutomationNotificationProcessing Processing, string DisplayString, string ActivityId);

	// Installed through AutomationPeer.TestAutomationPeerListener so ListenerExists reports true.
	internal sealed class RecordingAutomationListener : IAutomationPeerListener, System.IDisposable
	{
		private readonly IAutomationPeerListener? _previous;

		private RecordingAutomationListener()
		{
			_previous = AutomationPeer.TestAutomationPeerListener;
			AutomationPeer.TestAutomationPeerListener = this;
		}

		public static RecordingAutomationListener Install() => new();

		public List<(AutomationPeer Peer, string Event)> Events { get; } = new();

		public List<Notification> Notifications { get; } = new();

		public List<Notification> TakeNotifications(string activityId)
		{
			var taken = Notifications.Where(n => n.ActivityId == activityId).ToList();
			Notifications.RemoveAll(n => n.ActivityId == activityId);
			return taken;
		}

		public void Dispose() => AutomationPeer.TestAutomationPeerListener = _previous;

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => Events.Add((peer, eventId.ToString()));

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		{
		}

		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
		{
			if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty)
			{
				Events.Add((peer, $"IsSelected:{oldValue}->{newValue}"));
			}
		}

		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
			=> Notifications.Add(new(peer, notificationKind, notificationProcessing, displayString, activityId));

		public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child)
		{
		}

		public void NotifyInvalidatePeer(AutomationPeer peer)
		{
		}

		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData)
		{
		}
	}
#endif

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
