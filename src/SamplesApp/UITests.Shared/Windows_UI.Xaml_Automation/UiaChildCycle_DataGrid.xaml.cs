#nullable enable
// Repro for: Desktop Automation – DataGrid reports itself as a child recursively (#461)
//
// WCT DataGrid architecture that causes the cycle:
//
//   DataGrid (UIElement)
//     └─ DataGridAutomationPeer (canonical, Owner=DataGrid)
//          └─ GetChildrenCore() → visual tree walk → [DataGridRowsPresenterAutomationPeer, ...]
//
//   DataGridRowsPresenter (UIElement)
//     └─ DataGridRowsPresenterAutomationPeer
//          └─ GetChildrenCore() → GridPeer.GetChildPeers()
//              where GridPeer = CreatePeerForElement(DataGrid) = DataGridAutomationPeer
//
// Result: DataGrid → DataGridRowsPresenter → [DataGridAutomationPeer again] → infinite cycle.
//
// This sample reproduces that exact pattern with minimal custom controls so it can be
// verified without the WCT DataGrid package.

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Automation;

[Sample(
	"Automation",
	Name = "UiaChildCycle_DataGrid",
	Description = "Repro for #461: DataGrid UIA child-cycle. A custom peer deliberately returns its container's peer as a child (replicating WCT DataGridRowsPresenter). inspect.exe must show a finite tree with no repeated RuntimeIds.",
	IsManualTest = true)]
public sealed partial class UiaChildCycle_DataGrid : UserControl
{
	public UiaChildCycle_DataGrid()
	{
		this.InitializeComponent();
	}
}

// ──────────────────────────────────────────────────────────────────────────────
// UiaCycleContainer — the outer container (analogous to DataGrid)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Container control whose AutomationPeer uses the visual-tree walk, placing
/// <see cref="UiaCycleChild"/> as a child in the UIA tree.
/// </summary>
public class UiaCycleContainer : UserControl
{
	public UiaCycleContainer()
	{
		var child = new UiaCycleChild();
		// Keep a reference so the child can reach back to us.
		child.Container = this;
		Content = new StackPanel
		{
			Children =
			{
				new TextBlock { Text = "CycleChild (inner)" },
				child,
			},
		};
	}

	protected override AutomationPeer OnCreateAutomationPeer()
		=> new UiaCycleContainerAutomationPeer(this);
}

/// <summary>
/// AutomationPeer for <see cref="UiaCycleContainer"/>.
/// Uses the standard visual-tree walk (base class behaviour), so its children
/// will include <see cref="UiaCycleChildAutomationPeer"/>.
/// </summary>
public class UiaCycleContainerAutomationPeer : FrameworkElementAutomationPeer
{
	public UiaCycleContainerAutomationPeer(UiaCycleContainer owner) : base(owner) { }

	protected override string GetClassNameCore() => "UiaCycleContainer";
	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.DataGrid;
}

// ──────────────────────────────────────────────────────────────────────────────
// UiaCycleChild — the inner control (analogous to DataGridRowsPresenter)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Inner control whose AutomationPeer deliberately returns its container's peer
/// as one of its children — the exact pattern in WCT's
/// <c>DataGridRowsPresenterAutomationPeer.GetChildrenCore()</c> which calls
/// <c>GridPeer.GetChildPeers()</c> where GridPeer = CreatePeerForElement(DataGrid).
/// </summary>
public class UiaCycleChild : UserControl
{
	/// <summary>Back-reference to the container (set by <see cref="UiaCycleContainer"/>).</summary>
	internal UiaCycleContainer? Container { get; set; }

	public UiaCycleChild()
	{
		Content = new TextBlock { Text = "I am the cycle child — my peer returns the container peer as a child" };
	}

	protected override AutomationPeer OnCreateAutomationPeer()
		=> new UiaCycleChildAutomationPeer(this);
}

/// <summary>
/// AutomationPeer for <see cref="UiaCycleChild"/>.
/// Overrides <see cref="GetChildrenCore"/> to return the <em>container's</em>
/// automation peer in addition to normal children — this is the exact bug pattern
/// from WCT DataGrid.
/// </summary>
public class UiaCycleChildAutomationPeer : FrameworkElementAutomationPeer
{
	public UiaCycleChildAutomationPeer(UiaCycleChild owner) : base(owner) { }

	protected override string GetClassNameCore() => "UiaCycleChild";
	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Custom;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
		var peers = new List<AutomationPeer>();

		// Preserve any normal visual children first. For example, the TextBlock may
		// contribute its own automation peer via base.GetChildrenCore().
		if (base.GetChildrenCore() is { } baseChildren)
		{
			peers.AddRange(baseChildren);
		}

		// BUG PATTERN: deliberately inject the container's peer as a child.
		// This replicates DataGridRowsPresenterAutomationPeer returning
		// DataGridAutomationPeer via CreatePeerForElement(DataGrid).
		var container = ((UiaCycleChild)Owner).Container;
		if (container is not null)
		{
			var containerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(container);
			if (containerPeer is not null)
			{
				peers.Add(containerPeer);
			}
		}

		return peers;
	}
}
