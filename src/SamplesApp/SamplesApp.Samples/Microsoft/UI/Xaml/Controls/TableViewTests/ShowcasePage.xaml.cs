#nullable enable
#pragma warning disable CS8305 // TableView is [Experimental]

using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.System;

namespace MUXControlsTestApp;

// A realistic app page built around TableView: template cells, milestone grouping, a live
// sort on Updated, a frozen leading column and a selected row. It covers the whole window
// (Esc returns to the samples browser) so it can be captured without the harness chrome.
[Sample("TableView", Name = "TableView_Showcase", IgnoreInSnapshotTests = true)]
public sealed partial class ShowcasePage : Page
{
	private TableViewTemplateColumn _updated = null!;
	private Popup? _presenter;

	public ShowcasePage()
	{
		InitializeComponent();

		Table.GroupHeaderTemplate = GetTemplate("MilestoneHeader");
		BuildColumns();
		Table.ItemsSource = TableViewSource
			.From(Roadmap.Items())
			.GroupBy(new TableViewKeySelector(item => ((Roadmap)item!).Milestone));

		Loaded += OnLoaded;
		Unloaded += (_, _) => Dismiss();
	}

	public static Brush StatusBrush(string status) =>
		(Brush)Application.Current.Resources[status switch
		{
			"Shipped" => "SystemFillColorSuccessBrush",
			"In review" => "SystemFillColorCautionBrush",
			"In progress" => "SystemFillColorAttentionBrush",
			"Blocked" => "SystemFillColorCriticalBrush",
			_ => "SystemFillColorNeutralBrush",
		}];

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Table.Loaded += OnTableLoaded;
		Present();
	}

	private void OnTableLoaded(object sender, RoutedEventArgs e)
	{
		Table.Loaded -= OnTableLoaded;
		Table.SortByColumn(_updated, SortDirection.Ascending);
		Table.Select(6);
	}

	private void Present()
	{
		if (_presenter is not null || XamlRoot is not { } xamlRoot)
		{
			return;
		}

		Content = new Grid();
		_presenter = new Popup { XamlRoot = xamlRoot, Child = Root };
		Root.KeyDown += OnRootKeyDown;
		xamlRoot.Changed += OnXamlRootChanged;
		Resize(xamlRoot);
		_presenter.IsOpen = true;
	}

	private void Dismiss()
	{
		if (_presenter is null)
		{
			return;
		}

		if (_presenter.XamlRoot is { } xamlRoot)
		{
			xamlRoot.Changed -= OnXamlRootChanged;
		}

		Root.KeyDown -= OnRootKeyDown;
		_presenter.IsOpen = false;
		_presenter.Child = null;
		_presenter = null;
		Content = Root;
	}

	private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Escape)
		{
			e.Handled = true;
			Dismiss();
		}
	}

	private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => Resize(sender);

	private void Resize(XamlRoot xamlRoot)
	{
		Root.Width = xamlRoot.Size.Width;
		Root.Height = xamlRoot.Size.Height;
	}

	private DataTemplate GetTemplate(string key) => (DataTemplate)Resources[key];

	private void BuildColumns()
	{
		_updated = Column("Updated", "UpdatedCell", new GridLength(128), nameof(Roadmap.UpdatedMinutes));

		Table.Columns.Add(Column("Feature", "FeatureCell", new GridLength(240), nameof(Roadmap.Feature), TableViewFrozenEdge.Leading));
		Table.Columns.Add(Column("Owner", "OwnerCell", new GridLength(180), nameof(Roadmap.Owner)));
		Table.Columns.Add(Column("Status", "StatusCell", new GridLength(140), nameof(Roadmap.Status)));
		Table.Columns.Add(Column("Progress", "ProgressCell", new GridLength(1, GridUnitType.Star), nameof(Roadmap.Progress)));
		Table.Columns.Add(Column("Tests", "TestsCell", new GridLength(96), nameof(Roadmap.Tests)));
		Table.Columns.Add(_updated);
	}

	private TableViewTemplateColumn Column(string header, string template, GridLength width, string sortPath, TableViewFrozenEdge frozen = TableViewFrozenEdge.None) =>
		new()
		{
			Header = header,
			CellTemplate = GetTemplate(template),
			Width = width,
			SortMemberPath = sortPath,
			FrozenEdge = frozen,
		};
}

public sealed class Roadmap
{
	public string Milestone { get; init; } = "";
	public string Feature { get; init; } = "";
	public string Area { get; init; } = "";
	public string Owner { get; init; } = "";
	public string Status { get; init; } = "";
	public double Progress { get; init; }
	public string ProgressText => $"{Progress:0}%";
	public int Tests { get; init; }
	public string TestsText => Tests.ToString("N0", CultureInfo.CurrentCulture);
	public int UpdatedMinutes { get; init; }
	public string Updated => UpdatedMinutes switch
	{
		< 60 => $"{UpdatedMinutes} min ago",
		< 1440 => $"{UpdatedMinutes / 60} h ago",
		_ => $"{UpdatedMinutes / 1440} d ago",
	};

	public static List<Roadmap> Items()
	{
		List<Roadmap> items = new();

		void Add(string milestone, string feature, string area, string owner, string status, double progress, int tests, int updatedMinutes) =>
			items.Add(new Roadmap
			{
				Milestone = milestone,
				Feature = feature,
				Area = area,
				Owner = owner,
				Status = status,
				Progress = progress,
				Tests = tests,
				UpdatedMinutes = updatedMinutes,
			});

		Add("Milestone 1 · Shipped", "Offline sync", "Storage", "Ada Lovelace", "Shipped", 100, 1284, 2880);
		Add("Milestone 1 · Shipped", "Markdown editor", "Editor", "Grace Hopper", "Shipped", 100, 962, 4320);
		Add("Milestone 1 · Shipped", "Dark theme", "Design", "Jo Nakamura", "Shipped", 100, 311, 10080);
		Add("Milestone 1 · Shipped", "Keyboard shortcuts", "Input", "Alan Turing", "Shipped", 100, 457, 7200);

		Add("Milestone 2 · Beta", "Real-time collaboration", "Sync", "Omar Farouk", "In review", 86, 742, 18);
		Add("Milestone 2 · Beta", "Full-text search", "Search", "Nina Patel", "In progress", 64, 528, 95);
		Add("Milestone 2 · Beta", "Handwriting and ink", "Input", "Yu Chen", "In progress", 41, 203, 240);
		Add("Milestone 2 · Beta", "Screen reader pass", "Accessibility", "Ada Lovelace", "In review", 92, 388, 42);
		Add("Milestone 2 · Beta", "Image attachments", "Editor", "Grace Hopper", "Blocked", 23, 97, 1500);

		Add("Milestone 3 · Planned", "Web clipper", "Browser", "Nina Patel", "Planned", 15, 46, 2160);
		Add("Milestone 3 · Planned", "Summaries", "Intelligence", "Alan Turing", "Planned", 8, 12, 4320);

		return items;
	}
}
