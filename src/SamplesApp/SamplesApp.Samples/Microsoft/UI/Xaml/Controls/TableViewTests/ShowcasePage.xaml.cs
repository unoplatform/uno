#nullable enable
#pragma warning disable CS8305 // TableView is [Experimental]

using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Tabular;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace MUXControlsTestApp;

// A screenshot-friendly tour: template cells, milestone grouping, a live Progress sort,
// a frozen leading column and a selected row, all on one TableViewSource.
[Sample("TableView", Name = "TableView_Showcase")]
public sealed partial class ShowcasePage : Page
{
	private TableViewTemplateColumn _progress = null!;

	public ShowcasePage()
	{
		InitializeComponent();

		Table.GroupHeaderTemplate = GetTemplate("MilestoneHeader");
		BuildColumns();
		Table.ItemsSource = TableViewSource
			.From(Roadmap.Items())
			.GroupBy(new TableViewKeySelector(item => ((Roadmap)item!).Milestone));

		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Table.SortByColumn(_progress, SortDirection.Descending);
		Table.Select(2);
	}

	private DataTemplate GetTemplate(string key) => (DataTemplate)Resources[key];

	private void BuildColumns()
	{
		_progress = Column("Progress", "ProgressCell", new GridLength(220), nameof(Roadmap.Progress));

		Table.Columns.Add(Column("Feature", "FeatureCell", new GridLength(1, GridUnitType.Auto), nameof(Roadmap.Feature), TableViewFrozenEdge.Leading));
		Table.Columns.Add(Column("Owner", "OwnerCell", new GridLength(1, GridUnitType.Auto), nameof(Roadmap.Owner)));
		Table.Columns.Add(Column("Status", "StatusCell", new GridLength(150), nameof(Roadmap.Status)));
		Table.Columns.Add(_progress);
		Table.Columns.Add(Column("Tests", "TestsCell", new GridLength(120), nameof(Roadmap.Tests)));
		Table.Columns.Add(Column("Updated", "UpdatedCell", new GridLength(1, GridUnitType.Star), nameof(Roadmap.UpdatedMinutes)));
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
	private static readonly SolidColorBrush[] _avatarBrushes =
	{
		new(Color.FromArgb(255, 0x5B, 0x5F, 0xE6)),
		new(Color.FromArgb(255, 0xD9, 0x4F, 0x8C)),
		new(Color.FromArgb(255, 0x1F, 0x9D, 0x8B)),
		new(Color.FromArgb(255, 0xE0, 0x8A, 0x1E)),
		new(Color.FromArgb(255, 0x3A, 0x8D, 0xDE)),
		new(Color.FromArgb(255, 0x8E, 0x5B, 0xD9)),
	};

	public string Feature { get; init; } = "";
	public string Area { get; init; } = "";
	public string Glyph { get; init; } = "";
	public Brush TileBrush { get; init; } = null!;
	public string Owner { get; init; } = "";
	public string Initials { get; init; } = "";
	public Brush AvatarBrush { get; init; } = null!;
	public string Status { get; init; } = "";
	public Brush StatusForeground { get; init; } = null!;
	public Brush StatusBackground { get; init; } = null!;
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
	public string Milestone { get; init; } = "";

	public static List<Roadmap> Items()
	{
		List<Roadmap> items = new();
		var avatar = 0;

		void Add(string milestone, string feature, string area, string glyph, Color tile, string owner, string status, double progress, int tests, int updatedMinutes)
		{
			var (fg, bg) = StatusColors(status);
			var parts = owner.Split(' ');
			items.Add(new Roadmap
			{
				Milestone = milestone,
				Feature = feature,
				Area = area,
				Glyph = glyph,
				TileBrush = new SolidColorBrush(tile),
				Owner = owner,
				Initials = $"{parts[0][0]}{parts[^1][0]}",
				AvatarBrush = _avatarBrushes[avatar++ % _avatarBrushes.Length],
				Status = status,
				StatusForeground = fg,
				StatusBackground = bg,
				Progress = progress,
				Tests = tests,
				UpdatedMinutes = updatedMinutes,
			});
		}

		var blue = Color.FromArgb(255, 0x2F, 0x6F, 0xD6);
		var violet = Color.FromArgb(255, 0x7A, 0x4F, 0xD8);
		var teal = Color.FromArgb(255, 0x13, 0x8A, 0x7E);
		var amber = Color.FromArgb(255, 0xC9, 0x7A, 0x12);
		var rose = Color.FromArgb(255, 0xC2, 0x3D, 0x6E);

		Add("Milestone 1 · Shipped", "Offline sync", "Storage", "", teal, "Ada Lovelace", "Shipped", 100, 1284, 2880);
		Add("Milestone 1 · Shipped", "Markdown editor", "Editor", "", blue, "Grace Hopper", "Shipped", 100, 962, 4320);
		Add("Milestone 1 · Shipped", "Dark theme", "Design", "", violet, "Jo Nakamura", "Shipped", 100, 311, 10080);
		Add("Milestone 1 · Shipped", "Keyboard shortcuts", "Input", "", amber, "Alan Turing", "Shipped", 100, 457, 7200);

		Add("Milestone 2 · Beta", "Real-time collaboration", "Sync", "", rose, "Omar Farouk", "In review", 86, 742, 18);
		Add("Milestone 2 · Beta", "Full-text search", "Search", "", blue, "Nina Patel", "In progress", 64, 528, 95);
		Add("Milestone 2 · Beta", "Handwriting & ink", "Input", "", violet, "Yu Chen", "In progress", 41, 203, 240);
		Add("Milestone 2 · Beta", "Screen reader pass", "Accessibility", "", teal, "Ada Lovelace", "In review", 92, 388, 42);
		Add("Milestone 2 · Beta", "Image attachments", "Editor", "", amber, "Grace Hopper", "Blocked", 23, 97, 1500);

		Add("Milestone 3 · Planned", "AI summaries", "Intelligence", "", violet, "Alan Turing", "Planned", 8, 12, 4320);
		Add("Milestone 3 · Planned", "Shared notebooks", "Sync", "", rose, "Omar Farouk", "Planned", 3, 0, 8640);
		Add("Milestone 3 · Planned", "Web clipper", "Browser", "", blue, "Nina Patel", "Planned", 15, 46, 2160);

		return items;
	}

	private static (Brush Foreground, Brush Background) StatusColors(string status)
	{
		var fg = status switch
		{
			"Shipped" => Color.FromArgb(255, 0x6C, 0xCB, 0x5F),
			"In review" => Color.FromArgb(255, 0x60, 0xCD, 0xFF),
			"In progress" => Color.FromArgb(255, 0xB3, 0x9D, 0xFF),
			"Blocked" => Color.FromArgb(255, 0xFF, 0x99, 0xA4),
			_ => Color.FromArgb(255, 0xC8, 0xC8, 0xC8),
		};

		return (new SolidColorBrush(fg), new SolidColorBrush(Color.FromArgb(0x26, fg.R, fg.G, fg.B)));
	}
}
