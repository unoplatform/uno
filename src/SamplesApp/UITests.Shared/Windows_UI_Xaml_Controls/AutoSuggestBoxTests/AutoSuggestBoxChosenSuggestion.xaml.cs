using System;
using System.Collections;
using System.Linq;
using Windows.Foundation;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UITests.Shared.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
	public class Author
	{
		public static Author[] All = new Author[]
											{
														new Author { Name = "A0" },
														new Author { Name = "A1" },
														new Author { Name = "A2" },
														new Author { Name = "A3" },
														new Author { Name = "B0" },
														new Author { Name = "B1" },
														new Author { Name = "B2" },
														new Author { Name = "B3" },
														new Author { Name = "a0" },
														new Author { Name = "a1" },
														new Author { Name = "a2" },
														new Author { Name = "a3" },
											};

		public string Name { get; set; } = string.Empty;

		public override string ToString() => Name;
	}

	public class Book
	{	
		public Author Author { get; set; }		
	}   

	[SampleControlInfo("AutoSuggestBox", nameof(AutoSuggestBoxChosenSuggestion))]
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
					Content = $"e.ChosenSuggestion == NULL because the selectiopn does not match. The Text inputted is '{sender.Text}'!",
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
}
