using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater;

[Sample(
	"ItemsRepeater",
	IsManualTest = true,
	Description = "ScrollViewer + ItemsRepeater with a DataTemplateSelector dispatching to 5 templates of varying heights (32–400px). 150 items anchored to bottom. Verifies no overlap, no drift, and correct template selection across scroll jumps.")]
public sealed partial class ItemsRepeaterMultiTemplate : Page
{
	private const int DefaultItemCount = 150;

	private static readonly Brush s_greenBrush = new SolidColorBrush(Color.FromArgb(255, 76, 175, 80));
	private static readonly Brush s_blueBrush = new SolidColorBrush(Color.FromArgb(255, 66, 133, 244));
	private static readonly Brush s_orangeBrush = new SolidColorBrush(Color.FromArgb(255, 255, 152, 0));
	private static readonly Brush s_redBrush = new SolidColorBrush(Color.FromArgb(255, 229, 57, 53));
	private static readonly Brush s_purpleBrush = new SolidColorBrush(Color.FromArgb(255, 156, 39, 176));
	private static readonly Brush s_tealBrush = new SolidColorBrush(Color.FromArgb(255, 0, 150, 136));
	private static readonly Brush s_grayBrush = new SolidColorBrush(Colors.Gray);

	private readonly ObservableCollection<FeedItem> _items;

	public ItemsRepeaterMultiTemplate()
	{
		InitializeComponent();
		_items = new ObservableCollection<FeedItem>(GenerateItems(0, DefaultItemCount));
		Repeater.ItemsSource = _items;
		UpdateStatus();
	}

	private static IEnumerable<FeedItem> GenerateItems(int startIndex, int count)
	{
		for (var i = startIndex; i < startIndex + count; i++)
		{
			yield return CreateItem(i);
		}
	}

	private static FeedItem CreateItem(int index)
	{
		// Cycle through 5 template types with a varied but deterministic pattern
		var templateType = (FeedItemType)((index % 13) switch
		{
			0 => (int)FeedItemType.StatusRow,
			1 => (int)FeedItemType.StatusRow,
			2 => (int)FeedItemType.ContentCard,
			3 => (int)FeedItemType.StatusRow,
			4 => (int)FeedItemType.StatusRow,
			5 => (int)FeedItemType.DetailSection,
			6 => (int)FeedItemType.StatusRow,
			7 => (int)FeedItemType.InputBubble,
			8 => (int)FeedItemType.StatusRow,
			9 => (int)FeedItemType.ContentCard,
			10 => (int)FeedItemType.StatusRow,
			11 => (int)FeedItemType.StatusRow,
			12 => (int)FeedItemType.Banner,
			_ => (int)FeedItemType.StatusRow,
		});

		return templateType switch
		{
			FeedItemType.StatusRow => new FeedItem
			{
				Index = index,
				ItemType = FeedItemType.StatusRow,
				Title = StatusTitles[index % StatusTitles.Length],
				Subtitle = TimeLabels[index % TimeLabels.Length],
				AccentBrush = index % 3 == 0 ? s_greenBrush : index % 3 == 1 ? s_blueBrush : s_grayBrush,
			},
			FeedItemType.ContentCard => new FeedItem
			{
				Index = index,
				ItemType = FeedItemType.ContentCard,
				Title = CardTitles[index % CardTitles.Length],
				Body = CardBodies[index % CardBodies.Length],
				AccentBrush = s_blueBrush,
			},
			FeedItemType.DetailSection => new FeedItem
			{
				Index = index,
				ItemType = FeedItemType.DetailSection,
				Title = SectionTitles[index % SectionTitles.Length],
				Body = SectionBodies[index % SectionBodies.Length],
				IsExpanded = index % 2 == 0,
				InnerHeight = 60 + (index % 5) * 40,
				AccentBrush = s_purpleBrush,
			},
			FeedItemType.InputBubble => new FeedItem
			{
				Index = index,
				ItemType = FeedItemType.InputBubble,
				Title = InputTexts[index % InputTexts.Length],
				AccentBrush = s_tealBrush,
			},
			FeedItemType.Banner => new FeedItem
			{
				Index = index,
				ItemType = FeedItemType.Banner,
				Title = BannerTitles[index % BannerTitles.Length],
				Subtitle = BannerSubtitles[index % BannerSubtitles.Length],
				IconGlyph = BannerGlyphs[index % BannerGlyphs.Length],
				AccentBrush = index % 3 == 0 ? s_greenBrush : index % 3 == 1 ? s_orangeBrush : s_redBrush,
			},
			_ => throw new InvalidOperationException(),
		};
	}

	private void OnTopClick(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, 0, null, disableAnimation: true);

	private void OnBottomClick(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, Scroller.ScrollableHeight, null, disableAnimation: true);

	private void OnAddItemsClick(object sender, RoutedEventArgs e)
	{
		var start = _items.Count;
		foreach (var item in GenerateItems(start, 20))
		{
			_items.Add(item);
		}

		UpdateStatus();
	}

	private void OnClearClick(object sender, RoutedEventArgs e)
	{
		_items.Clear();
		UpdateStatus();
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		_items.Clear();
		foreach (var item in GenerateItems(0, DefaultItemCount))
		{
			_items.Add(item);
		}

		Scroller.ChangeView(null, Scroller.ScrollableHeight, null, disableAnimation: true);
		UpdateStatus();
	}

	private void UpdateStatus() =>
		StatusText.Text = $"{_items.Count} items";

	// --- Mock data pools ---

	private static readonly string[] StatusTitles =
	[
		"Searching workspace files",
		"Reading configuration",
		"Analyzing dependencies",
		"Compiling module",
		"Running validation checks",
		"Indexing symbols",
		"Resolving references",
		"Processing metadata",
		"Updating cache",
		"Scanning for changes",
		"Fetching remote data",
		"Applying transformations",
	];

	private static readonly string[] TimeLabels =
	[
		"2s", "150ms", "4s", "1.2s", "800ms", "3s", "50ms", "2.5s",
	];

	private static readonly string[] CardTitles =
	[
		"Analysis complete for module",
		"Dependency graph resolved",
		"Build output summary",
		"Configuration validation results",
		"Schema migration report",
	];

	private static readonly string[] CardBodies =
	[
		"Found 14 files across 3 directories. All references resolved successfully. No circular dependencies detected in the current scope. The module graph is stable.",
		"The dependency tree contains 42 nodes with a maximum depth of 7. Two optional packages were excluded based on platform constraints. Consider consolidating shared utilities.",
		"Build completed with 0 errors and 3 warnings. Output size is 2.4 MB compressed. Tree-shaking removed 18 unused exports. Source maps generated for all entry points.",
		"All 12 required fields are present and correctly typed. Two optional fields have default values applied. The schema version matches the expected runtime contract.",
		"Migration from v2 to v3 requires updating 6 table definitions. Estimated data transformation time: 45 seconds for current dataset. Backward-compatible read path preserved.",
	];

	private static readonly string[] SectionTitles =
	[
		"Detailed trace output",
		"Expanded diagnostic log",
		"Full resolution chain",
		"Step-by-step execution",
		"Intermediate computation results",
	];

	private static readonly string[] SectionBodies =
	[
		"The resolver walked 23 nodes before finding the target. Each hop added approximately 2ms of latency. The final resolution path was cached for subsequent lookups. No fallback strategies were needed during this traversal.",
		"Diagnostic capture includes 847 events across 12 categories. Memory allocation peaked at 64 MB during the sort phase. GC pressure was moderate with 3 gen-0 collections. Thread pool utilization reached 75% capacity.",
		"Starting from the root namespace, the chain resolved through 5 intermediate aliases before reaching the concrete type. Each alias contributed zero runtime overhead due to compile-time elimination.",
		"Phase 1 (parse): 120ms. Phase 2 (validate): 340ms. Phase 3 (transform): 890ms. Phase 4 (emit): 210ms. Total pipeline: 1.56s. Bottleneck identified in the transform phase due to recursive template expansion.",
		"The computation produced 3 intermediate matrices of dimensions 128x128, 256x256, and 512x512 respectively. Final reduction yielded a scalar value within expected tolerance bounds.",
	];

	private static readonly string[] InputTexts =
	[
		"Can you check why the layout is shifting when new items are added?",
		"Show me the full trace for that last operation",
		"Try running it again with the updated configuration",
		"What happens if we increase the batch size to 500?",
		"Looks good, go ahead and apply those changes",
		"Can you compare the output between the two versions?",
	];

	private static readonly string[] BannerTitles =
	[
		"Operation completed", "Warning detected", "Action required",
		"Process finished", "Review needed",
	];

	private static readonly string[] BannerSubtitles =
	[
		"All 24 items processed without errors",
		"3 items skipped due to format mismatch",
		"Manual confirmation needed before proceeding",
		"Output written to target directory",
		"2 conflicts found in the merge result",
	];

	private static readonly string[] BannerGlyphs =
	[
		"\uE10B", "\uE7BA", "\uE783", "\uE930", "\uE71C",
	];
}

public enum FeedItemType
{
	StatusRow,
	ContentCard,
	DetailSection,
	InputBubble,
	Banner,
}

public sealed class FeedItem : INotifyPropertyChanged
{
	private bool _isExpanded;

	public int Index { get; init; }

	public FeedItemType ItemType { get; init; }

	public string Title { get; init; } = string.Empty;

	public string Subtitle { get; init; } = string.Empty;

	public string Body { get; init; } = string.Empty;

	public string IconGlyph { get; init; } = "\uE10B";

	public Brush AccentBrush { get; init; }

	public double InnerHeight { get; init; } = 80;

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (_isExpanded != value)
			{
				_isExpanded = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;
}

public sealed class FeedItemTemplateSelector : DataTemplateSelector
{
	public DataTemplate StatusRowTemplate { get; set; }

	public DataTemplate ContentCardTemplate { get; set; }

	public DataTemplate DetailSectionTemplate { get; set; }

	public DataTemplate InputBubbleTemplate { get; set; }

	public DataTemplate BannerTemplate { get; set; }

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
	{
		if (item is FeedItem feedItem)
		{
			return feedItem.ItemType switch
			{
				FeedItemType.StatusRow => StatusRowTemplate,
				FeedItemType.ContentCard => ContentCardTemplate,
				FeedItemType.DetailSection => DetailSectionTemplate,
				FeedItemType.InputBubble => InputBubbleTemplate,
				FeedItemType.Banner => BannerTemplate,
				_ => StatusRowTemplate,
			};
		}

		return StatusRowTemplate;
	}
}
