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

	private void OnRemoveExpandersClick(object sender, RoutedEventArgs e)
	{
		for (var i = _items.Count - 1; i >= 0; i--)
		{
			if (_items[i].ItemType == FeedItemType.DetailSection)
			{
				_items.RemoveAt(i);
			}
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
		"Preheating the oven",
		"Chopping the onions",
		"Simmering the stock",
		"Kneading the dough",
		"Whisking the eggs",
		"Toasting the spices",
		"Marinating the chicken",
		"Reducing the sauce",
		"Folding in the butter",
		"Resting the steak",
		"Proofing the bread",
		"Plating the dish",
	];

	private static readonly string[] TimeLabels =
	[
		"2 min", "15 min", "1 hr", "45 min", "30 min", "5 min", "90 min", "20 min",
	];

	private static readonly string[] CardTitles =
	[
		"Classic margherita pizza",
		"Slow-roasted lamb shoulder",
		"Lemon and herb roast chicken",
		"Creamy wild mushroom risotto",
		"Dark chocolate fondant",
	];

	private static readonly string[] CardBodies =
	[
		"A thin, blistered crust topped with San Marzano tomatoes, fresh mozzarella, and basil. Bake on a preheated stone for the crispest base. Serves four as a light dinner.",
		"Lamb rubbed with garlic and rosemary, then cooked low and slow until it falls apart at the touch of a fork. Rest under foil before carving. Pairs beautifully with roasted root vegetables.",
		"A whole chicken brined overnight, stuffed with lemon and thyme, and roasted until the skin turns golden and crisp. Baste twice during cooking for an even colour all over.",
		"Arborio rice cooked gently with white wine and warm stock, finished with sautéed mushrooms and a generous handful of grated parmesan. Stir often for a silky, loose texture.",
		"An indulgent dessert with a molten centre. Bake for exactly twelve minutes so the edges set while the middle stays liquid. Serve immediately with a scoop of vanilla ice cream.",
	];

	private static readonly string[] SectionTitles =
	[
		"Knife skills walkthrough",
		"Building flavour in stews",
		"Mastering the perfect sear",
		"Bread proofing explained",
		"Emulsions and sauces",
	];

	private static readonly string[] SectionBodies =
	[
		"Keep the blade angled slightly outward and let the knife do the work in long, smooth strokes. Curl your guiding fingertips inward so the flat of the blade rests against your knuckles. Practise on softer vegetables before moving on to denser ones.",
		"Start by browning the meat in batches so the pan stays hot and the surface caramelises properly. Deglaze with a splash of wine to lift the fond, then add the aromatics and let everything cook down slowly. Time is the most important ingredient here.",
		"Pat the protein completely dry and season it generously just before it hits the pan. Use a heavy skillet and resist the urge to move the food too early. A proper crust will release on its own once it is ready.",
		"During proofing the yeast produces gas that stretches the gluten network and gives bread its airy crumb. A warm, draught-free spot speeds things up, while a slow cold proof in the fridge develops a deeper flavour over many hours.",
		"An emulsion suspends tiny droplets of fat in a water-based liquid, held together by an emulsifier such as egg yolk or mustard. Add the oil slowly at first and whisk constantly so the mixture thickens without splitting.",
	];

	private static readonly string[] InputTexts =
	[
		"Can we swap the cream for something a little lighter?",
		"What sides would go well with the roast lamb?",
		"Let's try the dough with a longer overnight proof",
		"How do I stop the sauce from splitting next time?",
		"That risotto turned out perfectly, thank you",
		"Could you scale the recipe up for eight guests?",
	];

	private static readonly string[] BannerTitles =
	[
		"Dish ready to serve", "Oven needs attention", "Ingredient running low",
		"Timer finished", "Taste and adjust",
	];

	private static readonly string[] BannerSubtitles =
	[
		"All four courses are plated and still warm",
		"The roast has reached its target temperature",
		"Only a handful of fresh basil leaves left",
		"The bread has finished its second proof",
		"Season the soup before the final simmer",
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
