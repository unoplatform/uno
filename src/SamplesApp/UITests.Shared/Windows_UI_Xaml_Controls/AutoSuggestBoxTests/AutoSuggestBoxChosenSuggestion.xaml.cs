using System;
using System.Collections;
using System.Linq;
using Windows.Foundation;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

namespace UITests.Shared.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

[Sample("AutoSuggestBox", IsManualTest = false)]
public sealed partial class AutoSuggestBoxChosenSuggestion : UserControl
{
	private Book book = new Book { Author = new Author { Name = "A0" } };

	public AutoSuggestBoxChosenSuggestion()
	{
		this.InitializeComponent();
	}

	private async void autoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
	{
		if (e.ChosenSuggestion == null)
		{
			await new ContentDialog
			{
				Content = "e.ChosenSuggestion == NULL",
				PrimaryButtonText = "OK",
				RequestedTheme = ElementTheme.Default,
				XamlRoot = XamlRoot,
			}.ShowAsync();
		}
		else
		{
			logger.Text = $"ChosenSuggestion -> {(e.ChosenSuggestion?.ToString() ?? "<null>")}";
		}
	}

	private void autoSuggestBox_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs e)
	{
		if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput && s.Text?.Trim() is { Length: > 0 } searchTerm)
		{
			s.ItemsSource = Author.All.Where(a => a.Name.StartsWith(searchTerm)).ToList();
		}
	}
}
