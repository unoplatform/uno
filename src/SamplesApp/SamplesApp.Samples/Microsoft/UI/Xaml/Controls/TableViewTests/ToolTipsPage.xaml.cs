// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Samples\TableViewSampleApp\ToolTipsPage.xaml.cs, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable
#pragma warning disable CS8305 // TableView is [Experimental]

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace MUXControlsTestApp;

// Turns a row item into rich (non-string) tooltip content. A converter is how computed tooltips are
// authored now that the content is a binding value rather than something a handler hands back.
public sealed class BioCardConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, string language)
	{
		if (value is not Item item)
		{
			return null;
		}

		// A fresh element per evaluation: a UIElement is parented by the cell's ToolTip.
		var panel = new StackPanel { Spacing = 4 };
		panel.Children.Add(new TextBlock { Text = item.Name, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
		panel.Children.Add(new TextBlock { Text = $"{item.Role} - {item.City}", Opacity = 0.75 });
		panel.Children.Add(new Border
		{
			Height = 4,
			Width = 120,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Microsoft.UI.Colors.SteelBlue),
		});

		return panel;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, string language) =>
		throw new NotSupportedException();
}

// Exposes the protected peer/provider bridge so the sample can read what a UIA client would see.
internal sealed class PeerBridge : FrameworkElementAutomationPeer
{
	public PeerBridge(FrameworkElement owner) : base(owner)
	{
	}

	public AutomationPeer? From(IRawElementProviderSimple provider) => PeerFromProvider(provider);
}

// Exercises per-cell tooltips: a plain string binding, a converter producing non-string content, a
// column that opts out, a cell whose own template owns a tooltip, and late attach / detach.
[Sample("TableView", Name = "TableView_ToolTips")]
public sealed partial class ToolTipsPage : Page
{
	private readonly TableViewColumn _nameColumn;
	private readonly TableViewColumn _bioColumn;
	private readonly TableViewColumn _cityColumn;
	private readonly TableViewColumn _scoreColumn;
	private readonly TableViewColumn _unsortedTipColumn;
	private readonly TableViewColumn _unsortedBareColumn;

	private bool _attached;
	private int _mutations;

	public ToolTipsPage()
	{
		this.InitializeComponent();

		_nameColumn = SampleColumns.Text("Name", nameof(Item.Name), SampleColumns.Pixels(140));
		_bioColumn = SampleColumns.Text("Bio", nameof(Item.Bio), SampleColumns.Pixels(180));
		_cityColumn = SampleColumns.Text("City", nameof(Item.City), SampleColumns.Pixels(120));
		_scoreColumn = SampleColumns.Text("Score", nameof(Item.Score), SampleColumns.Pixels(70));
		_unsortedTipColumn = SampleColumns.Text("NoSortTip", nameof(Item.City), SampleColumns.Pixels(110));
		_unsortedBareColumn = SampleColumns.Text("NoSortBare", nameof(Item.City), SampleColumns.Pixels(110));

		Table.Columns.Add(_nameColumn);
		Table.Columns.Add(_bioColumn);
		Table.Columns.Add(_cityColumn);
		Table.Columns.Add(_scoreColumn);
		Table.Columns.Add(_unsortedTipColumn);
		Table.Columns.Add(_unsortedBareColumn);
		Table.Columns.Add(new TableViewTemplateColumn
		{
			Header = "Notes (app tooltip)",
			CellTemplate = (DataTemplate)Resources["AppToolTipCell"],
			Width = SampleColumns.Pixels(220),
		});

		Table.ItemsSource = Data.Make();

		AttachToolTipBindings();
		AttachHeaderToolTips();

		// Score deliberately never gets a binding: no tooltip should ever appear over that column.
		UpdateStatus();
	}

	private void AttachHeaderToolTips()
	{
		_nameColumn.HeaderToolTip = "Full name, as entered";
		_cityColumn.HeaderToolTip = "City the person works from";

		// Repeats the header's own text: the peer drops it so it is not announced twice.
		_scoreColumn.HeaderToolTip = "Score";

		// Non-string header content: mouse only, and never reported as help text.
		var card = new StackPanel { Spacing = 4 };
		card.Children.Add(new TextBlock { Text = "Bio", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
		card.Children.Add(new TextBlock { Text = "Free-form summary", Opacity = 0.75 });
		_bioColumn.HeaderToolTip = card;

		// Isolates the two help-text paths: an unsortable column reports only its tooltip.
		_unsortedTipColumn.CanSort = false;
		_unsortedTipColumn.HeaderToolTip = "Unsortable, tooltip only";
		_unsortedBareColumn.CanSort = false;
	}

	private void AttachToolTipBindings()
	{
		// Same text as the cell: the peer drops the duplicate HelpText so Narrator reads it once.
		_bioColumn.CellToolTipBinding = new Binding { Path = new PropertyPath(nameof(Item.Bio)) };

		// Distinct from the cell's own text, and observable: HelpText must survive here, and
		// mutating Notes must update the tooltip with no invalidation call.
		_cityColumn.CellToolTipBinding = new Binding { Path = new PropertyPath(nameof(Item.Notes)) };

		// Non-string content, produced by a converter from the whole row item.
		_nameColumn.CellToolTipBinding = new Binding
		{
			Converter = new BioCardConverter(),
		};

		_attached = true;
		AttachLate.Content = "Detach bindings";
	}

	private void OnAttachLate(object sender, RoutedEventArgs e)
	{
		if (_attached)
		{
			// Clearing the binding retracts the tooltips.
			_bioColumn.CellToolTipBinding = null;
			_cityColumn.CellToolTipBinding = null;
			_nameColumn.CellToolTipBinding = null;
			_attached = false;
			AttachLate.Content = "Attach bindings";
		}
		else
		{
			AttachToolTipBindings();
		}

		UpdateStatus();
	}

	private void OnMutateData(object sender, RoutedEventArgs e)
	{
		// No invalidation API, and none needed: Item raises PropertyChanged for Notes.
		_mutations++;
		if (Table.ItemsSource is IEnumerable<Item> items)
		{
			foreach (var item in items.Take(40))
			{
				item.Notes = $"{item.Notes.Split(" [")[0]} [rev {_mutations}]";
			}
		}

		UpdateStatus();
	}

	private void UpdateStatus()
	{
		Status.Text = $"tooltip bindings {(_attached ? "attached" : "detached")} - data revision {_mutations}"
			+ " - Bio (dup text), City (shows Notes), Name (rich content), Score (none), Notes (app-owned)";
	}

	private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
	{
		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			yield return child;
			foreach (var d in Descendants(child))
			{
				yield return d;
			}
		}
	}

	private void OnScroll(object sender, RoutedEventArgs e)
	{
		// Scrolling recycles rows. With bindings there is no per-cell work on this path.
		var scrollers = Descendants(Table).OfType<ScrollViewer>().ToList();
		var scroller = scrollers.OrderByDescending(s => s.ScrollableHeight).FirstOrDefault();
		if (scroller is null || scroller.ScrollableHeight <= 0)
		{
			Status.Text = $"SCROLL: no vertically scrollable ScrollViewer (found {scrollers.Count})";
			return;
		}

		var before = scroller.VerticalOffset;
		_ = scroller.ChangeView(null, before + 900, null, true);

		DispatcherQueue.TryEnqueue(() =>
		{
			Status.Text = $"SCROLL: {before:F0} -> {scroller.VerticalOffset:F0} (max {scroller.ScrollableHeight:F0})";
		});
	}

	// Reads the live cell wrappers and reports, per column, who owns each cell's tooltip. This is
	// the only way to check the ownership rules without UIA (whose tree walks are unreliable here).
	private void OnCheckOwnership(object sender, RoutedEventArgs e)
	{
		var perColumn = new Dictionary<string, (int cells, int controlTips, int appTipsInContent, int helpTexts, int dupHelpText)>();

		foreach (var border in Descendants(Table).OfType<Border>())
		{
			if (border.Tag is not TableViewColumn column)
			{
				continue;
			}

			var header = column.Header?.ToString() ?? "?";
			perColumn.TryGetValue(header, out var acc);

			acc.cells++;

			if (ToolTipService.GetToolTip(border) is not null)
			{
				acc.controlTips++;
			}

			var help = AutomationProperties.GetHelpText(border);
			if (!string.IsNullOrEmpty(help))
			{
				acc.helpTexts++;

				// The peer suppresses duplicate HelpText at query time, so a match here is expected.
				var cellText = Descendants(border).OfType<TextBlock>().FirstOrDefault()?.Text;
				if (!string.IsNullOrEmpty(cellText) && cellText == help)
				{
					acc.dupHelpText++;
				}
			}

			// What Narrator actually reads comes from TableViewCellAutomationPeer, which the row
			// peer creates - not from a peer for the Border. Counted in OnCheckUiaHelp instead.

			// An app tooltip declared inside the cell template lives on the content, not the wrapper.
			foreach (var inner in Descendants(border))
			{
				if (inner is FrameworkElement fe && ToolTipService.GetToolTip(fe) is not null)
				{
					acc.appTipsInContent++;
					break;
				}
			}

			perColumn[header] = acc;
		}

		var sb = new StringBuilder();
		foreach (var kv in perColumn.OrderBy(k => k.Key))
		{
			sb.Append($"[{kv.Key}] cells={kv.Value.cells} tip={kv.Value.controlTips} ")
			  .Append($"appTip={kv.Value.appTipsInContent} help={kv.Value.helpTexts} ")
			  .Append($"DUPhelp={kv.Value.dupHelpText}   ");
		}

		Status.Text = sb.Length > 0 ? sb.ToString() : "no cells found";
	}

	// Walks the real UIA peer tree: TableView peer -> row peers -> TableViewCellAutomationPeer.
	// GetHelpText() here is exactly what Narrator reads, so it proves the peer's suppression.
	private void OnCheckUiaHelp(object sender, RoutedEventArgs e)
	{
		var tablePeer = FrameworkElementAutomationPeer.CreatePeerForElement(Table);
		if (tablePeer is null)
		{
			Status.Text = "UIA: no TableView peer";
			return;
		}

		var columnNames = new[] { "Name", "Bio", "City", "Score", "Notes" };
		var withHelp = new int[columnNames.Length];
		var totals = new int[columnNames.Length];
		var firstText = new string[columnNames.Length];
		int rows = 0;

		// Row peers are not necessarily direct children of the table peer, so find the peers whose
		// own children are TableViewCell peers.
		static IEnumerable<AutomationPeer> Walk(AutomationPeer peer, int depth)
		{
			yield return peer;
			if (depth > 8)
			{
				yield break;
			}

			var children = peer.GetChildren();
			if (children is null)
			{
				yield break;
			}

			foreach (var c in children)
			{
				foreach (var d in Walk(c, depth + 1))
				{
					yield return d;
				}
			}
		}

		foreach (var candidate in Walk(tablePeer, 0))
		{
			var cells = candidate.GetChildren();
			if (cells is null || cells.Count == 0 || cells[0].GetClassName() != "TableViewCell")
			{
				continue;
			}

			rows++;
			for (int i = 0; i < cells.Count && i < columnNames.Length; i++)
			{
				totals[i]++;
				var help = cells[i].GetHelpText();
				if (!string.IsNullOrEmpty(help))
				{
					withHelp[i]++;
					firstText[i] ??= help;
				}
			}
		}

		var sb2 = new StringBuilder($"UIA rows={rows}   ");
		for (int i = 0; i < columnNames.Length; i++)
		{
			sb2.Append($"[{columnNames[i]}] uiaHelp={withHelp[i]}/{totals[i]}");
			if (firstText[i] is not null)
			{
				var t = firstText[i];
				sb2.Append($" \"{(t.Length > 18 ? t[..18] : t)}\"");
			}
			sb2.Append("   ");
		}

		Status.Text = sb2.ToString();
	}

	// Header peers are virtual: reach them through the table provider, not the peer's children.
	private void OnCheckHeaderUia(object sender, RoutedEventArgs e)
	{
		var tablePeer = FrameworkElementAutomationPeer.CreatePeerForElement(Table);
		if (tablePeer is not ITableProvider table)
		{
			Status.Text = "UIA: no TableView table provider";
			return;
		}

		var providers = table.GetColumnHeaders();
		if (providers is null || providers.Length == 0)
		{
			Status.Text = "UIA: no header providers";
			return;
		}

		var sb = new StringBuilder($"headers={providers.Length}  ");
		var bridge = new PeerBridge(Table);
		foreach (var provider in providers)
		{
			var peer = bridge.From(provider);
			if (peer is null)
			{
				sb.Append("[unconnected]   ");
				continue;
			}

			sb.Append($"[{peer.GetName()}] help=\"{peer.GetHelpText()}\"   ");
		}

		Status.Text = sb.ToString();
	}

	// Reports whether each header carries a ToolTip, verifiable without hovering.
	private void OnCheckHeaderTips(object sender, RoutedEventArgs e)
	{
		var sb = new StringBuilder();
		int tagged = 0;

		foreach (var element in Descendants(Table).OfType<FrameworkElement>())
		{
			if (element.Tag is not TableViewColumn column || element is Border)
			{
				continue;
			}

			var tip = ToolTipService.GetToolTip(element);
			if (tip is null && column.HeaderToolTip is null)
			{
				continue;
			}

			tagged++;
			var kind = tip switch
			{
				null => "none",
				ToolTip t => t.Content is string s ? $"text:\"{s}\"" : $"content:{t.Content?.GetType().Name}",
				_ => tip.GetType().Name,
			};
			sb.Append($"[{column.Header}] {kind}   ");
		}

		Status.Text = tagged > 0 ? sb.ToString() : "no header cells found";
	}

	// Hit-test near the trailing edge, away from the glyphs: a miss there means no tooltip.
	private void OnCheckHeaderHitTest(object sender, RoutedEventArgs e)
	{
		var sb = new StringBuilder();

		foreach (var element in Descendants(Table).OfType<FrameworkElement>())
		{
			if (element.Tag is not TableViewColumn column || element is Border)
			{
				continue;
			}

			var origin = element.TransformToVisual(null).TransformPoint(new Windows.Foundation.Point(0, 0));
			var probe = new Windows.Foundation.Point(
				origin.X + element.ActualWidth - 14,
				origin.Y + (element.ActualHeight / 2));

			var hits = VisualTreeHelper.FindElementsInHostCoordinates(probe, Table);
			bool reachesCell = hits.Any(h => ReferenceEquals(h, element));
			sb.Append($"[{column.Header}] {(reachesCell ? "HIT" : "MISS")}   ");
		}

		Status.Text = sb.Length > 0 ? sb.ToString() : "no header cells found";
	}

	// Exercises the in-place update path (OnColumnHeaderToolTipChanged): change one, retract one.
	private void OnMutateHeaderTips(object sender, RoutedEventArgs e)
	{
		_nameColumn.HeaderToolTip = "CHANGED in place";
		_cityColumn.HeaderToolTip = null;
		Status.Text = "header tooltips mutated: Name changed, City retracted";
	}

	// Sorting re-renders the header band, exercising the teardown before Children().Clear().
	private void OnSortHeaders(object sender, RoutedEventArgs e)
	{
		Table.SortByColumn(_nameColumn, Microsoft.UI.Xaml.Controls.Tabular.SortDirection.Ascending);
		Status.Text = "sorted by Name (header band rebuilt)";
	}
}
