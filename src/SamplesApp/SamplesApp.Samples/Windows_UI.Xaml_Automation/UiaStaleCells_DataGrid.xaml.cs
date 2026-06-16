#nullable enable
// Repro for: Accessibility/Automation – DataGridRow contents display empty cells after an action
// that refreshes the data (#492).
//
// WCT DataGrid automation shape that triggers the bug:
//
//   DataGrid
//     └─ DataGridAutomationPeer (canonical, Owner=DataGrid)
//          └─ GetChildrenCore() → one DataGridItemAutomationPeer per data item (cached by item)
//
//   DataGridItemAutomationPeer  (Owner=DataGrid → a "virtual" peer, no UIElement of its own)
//        └─ GetChildrenCore():
//               OwningRowPeer.InvalidatePeer();      // drop the row's cached UIA node
//               return OwningRowPeer.GetChildren();  // cells of the *currently displayed* row
//
// Every cell/name/etc. is resolved live from OwningRow (the row container currently realized for
// the item). So the data is always available — but on Skia/Win32 the UIA provider for the virtual
// item peer caches the first GetChildren() result and is never invalidated (it is keyed by peer,
// not by element, so the visual-tree-driven invalidation path can't reach it). After a refresh the
// cached cell peers point at recycled/detached containers → the row reports empty cells.
//
// This sample replicates that exact pattern with minimal custom controls so it can be verified
// with inspect.exe / Accessibility Insights without the WCT DataGrid package.

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation;

[Sample(
	"Automation",
	Name = "UiaStaleCells_DataGrid",
	Description = "Repro for #492: DataGrid rows report empty/stale cells after a data refresh. A custom grid replicates WCT's DataGridItemAutomationPeer (virtual per-item peer delegating to the realized row). After 'Refresh data', inspect.exe must show the current cell text, not the old/empty cells.",
	IsManualTest = true)]
public sealed partial class UiaStaleCells_DataGrid : UserControl
{
	private readonly MiniGrid _grid = new();

	public UiaStaleCells_DataGrid()
	{
		this.InitializeComponent();

		_grid.SetItems(new[]
		{
			new RowData("Row 0"),
			new RowData("Row 1"),
			new RowData("Row 2"),
		});

		MiniGridHost.Content = _grid;
		RefreshButton.Click += (_, _) => _grid.Refresh();
	}
}

/// <summary>One data row. Its <see cref="Value"/> changes on every refresh.</summary>
internal sealed class RowData
{
	private int _version = 1;

	public RowData(string label) => Label = label;

	public string Label { get; }

	public string Value => $"{Label} — v{_version}";

	public void Bump() => _version++;
}

// ──────────────────────────────────────────────────────────────────────────────
// MiniGrid — analogous to DataGrid
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Minimal DataGrid stand-in. Realizes one <see cref="MiniRow"/> per item and keeps an
/// item→container map so the virtual item peers can resolve their "owning row" live.
/// </summary>
internal sealed partial class MiniGrid : ContentControl
{
	private readonly StackPanel _rowsHost = new() { Spacing = 4 };
	private readonly List<RowData> _items = new();
	private readonly Dictionary<RowData, MiniRow> _containers = new();

	public MiniGrid()
	{
		Content = _rowsHost;
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
	}

	internal IReadOnlyList<RowData> Items => _items;

	internal void SetItems(IEnumerable<RowData> items)
	{
		_items.Clear();
		_items.AddRange(items);
		Realize();
	}

	/// <summary>Returns the realized container for an item, or null if not displayed.</summary>
	internal MiniRow? GetContainerForItem(RowData item)
		=> _containers.TryGetValue(item, out var row) ? row : null;

	/// <summary>
	/// Mimics a "log refresh": bump each item's value and rebuild the realized cell content.
	/// Replacing the cell content detaches the previous cell elements — exactly the situation
	/// that leaves stale/empty automation cells when the item peer's children cache is not
	/// invalidated.
	/// </summary>
	internal void Refresh()
	{
		foreach (var item in _items)
		{
			item.Bump();
		}

		// Rebuild containers from scratch to model container recycling.
		Realize();
	}

	private void Realize()
	{
		_rowsHost.Children.Clear();
		_containers.Clear();

		foreach (var item in _items)
		{
			var row = new MiniRow();
			row.SetData(item);
			_containers[item] = row;
			_rowsHost.Children.Add(row);
		}
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new MiniGridAutomationPeer(this);
}

// ──────────────────────────────────────────────────────────────────────────────
// MiniRow — analogous to DataGridRow
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Row container. Its cells (two <see cref="TextBlock"/>s) are rebuilt by <see cref="SetData"/>,
/// so a refresh detaches the previous cell elements.
/// </summary>
internal sealed partial class MiniRow : ContentControl
{
	public MiniRow() => HorizontalContentAlignment = HorizontalAlignment.Stretch;

	internal void SetData(RowData item)
	{
		// New cell elements each time → the previous ones are removed from the tree.
		Content = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 12,
			Children =
			{
				new TextBlock { Text = item.Label },
				new TextBlock { Text = item.Value },
			},
		};
	}
}

// ──────────────────────────────────────────────────────────────────────────────
// MiniGridAutomationPeer — analogous to DataGridAutomationPeer
// ──────────────────────────────────────────────────────────────────────────────

internal sealed class MiniGridAutomationPeer : FrameworkElementAutomationPeer
{
	// Cache item peers by item, like DataGridAutomationPeer._itemPeers / GetOrCreateItemPeer.
	private readonly Dictionary<RowData, MiniItemAutomationPeer> _itemPeers = new();

	public MiniGridAutomationPeer(MiniGrid owner) : base(owner) { }

	protected override string GetClassNameCore() => "MiniGrid";

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var grid = (MiniGrid)Owner;
		var children = new List<AutomationPeer>();

		foreach (var item in grid.Items)
		{
			var itemPeer = GetOrCreateItemPeer(item, grid);

			// Link the realized row peer's EventsSource to the item peer, exactly like
			// DataGridAutomationPeer.UpdateRowPeerEventsSource. This makes the row/cell
			// peers resolve to the item peer's provider for events.
			if (grid.GetContainerForItem(item) is { } container
				&& CreatePeerForElement(container) is { } rowPeer)
			{
				rowPeer.EventsSource = itemPeer;
			}

			children.Add(itemPeer);
		}

		return children;
	}

	private MiniItemAutomationPeer GetOrCreateItemPeer(RowData item, MiniGrid grid)
	{
		if (!_itemPeers.TryGetValue(item, out var peer))
		{
			peer = new MiniItemAutomationPeer(item, grid);
			_itemPeers[item] = peer;
		}

		return peer;
	}
}

// ──────────────────────────────────────────────────────────────────────────────
// MiniItemAutomationPeer — analogous to DataGridItemAutomationPeer (virtual peer)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Virtual per-item peer: its Owner is the grid (not a row), and all of its content is resolved
/// live from the currently-realized row container. This is a faithful reduction of WCT's
/// <c>DataGridItemAutomationPeer</c>.
/// </summary>
internal sealed class MiniItemAutomationPeer : FrameworkElementAutomationPeer
{
	private readonly RowData _item;
	private readonly MiniGrid _grid;

	public MiniItemAutomationPeer(RowData item, MiniGrid grid) : base(grid)
	{
		_item = item;
		_grid = grid;
	}

	private AutomationPeer? OwningRowPeer
		=> _grid.GetContainerForItem(_item) is { } row ? CreatePeerForElement(row) : null;

	protected override string GetClassNameCore() => "MiniItem";

	protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		if (OwningRowPeer is { } rowPeer)
		{
			// Exactly DataGridItemAutomationPeer.GetChildrenCore: ask the row peer to refresh,
			// then return its (live) children.
			rowPeer.InvalidatePeer();
			return rowPeer.GetChildren() ?? new List<AutomationPeer>();
		}

		return new List<AutomationPeer>();
	}

	protected override string GetNameCore()
		=> OwningRowPeer?.GetName() is { Length: > 0 } name ? name : _item.Label;
}
