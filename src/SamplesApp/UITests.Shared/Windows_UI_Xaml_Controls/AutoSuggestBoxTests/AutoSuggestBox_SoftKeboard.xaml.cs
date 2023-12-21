using System;
using System.Linq;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

[SampleControlInfo("AutoSuggestBox",
				   "AutoSuggestBox_SoftKeboard",
					description: "This test needs the Soft Keyboard. To validate the fix, start typing using the Keyboard on the Screen and tapping over one of the options should make the selection.",
					isManualTest: true)]
public sealed partial class AutoSuggestBox_SoftKeboard : UserControl
{
	public AutoSuggestBox_SoftKeboard()
	{
		this.InitializeComponent();
	}

	private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
#if !__ANDROID__ && !__IOS__
		if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
		{
			return;
		}
#endif

		if (string.IsNullOrEmpty(sender.Text))
		{
			return;
		}

		var fruits = "Apple,Banana,Apricot,Atemoya,Avocados,Blueberry,Blackcurrant,Ackee,Cranberry,Cantaloupe,Cherry,Black sapote,Dragonrfruit,Dates,Cherimoya,Buddha’s hand fruit,Finger Lime,Fig,Coconut,Cape gooseberry,Grapefruit,Gooseberries,Custard apple,Chempedak,Hazelnut,Honeyberries,Dragon fruit,Durian,Horned Melon,Hog Plum,Egg fruit,Feijoa,Indian Fig,Ice Apple,Guava,Fuyu Persimmon,Jackfruit,Jujube,Honeydew melon,Jenipapo,Kiwi,Kabosu,Kiwano,Kaffir lime,Lime,Lychee,Longan,Langsat,Mango,Mulberry,Pear,Lucuma,Muskmelon,Naranjilla,Passion fruit,Mangosteen,Nectarine,Nance,Quince,Medlar fruit,Olive,Oranges,Ramphal,Mouse melon,Papaya,Peach,Rose apple,Noni fruit,Pomegranate,Pineapple,Rambutan,Snake fruit,Raspberries,Strawberries,Starfruit,Soursop,Tangerine,Watermelon,Sapota,Star apple"
			.Split(',')
			.Select(Tuple.Create<string, int>);

		sender.ItemsSource = fruits
			.Where(x => x.Item1.Contains(sender.Text, StringComparison.InvariantCultureIgnoreCase))
			.Select((x, i) => $"{x.Item2:d2} f{i:d2}: {x}")
			.ToList();
	}
	private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		SelectionText.Text = $"{DateTime.Now:HH:mm:ss}: {args.SelectedItem}";
	}
}
