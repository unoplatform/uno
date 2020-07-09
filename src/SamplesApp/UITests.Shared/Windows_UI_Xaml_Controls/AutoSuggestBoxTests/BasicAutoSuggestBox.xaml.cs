using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
	[SampleControlInfo("AutoSuggestBox", nameof(BasicAutoSuggestBox))]
	public sealed partial class BasicAutoSuggestBox : UserControl
	{
		private ObservableCollection<string> _suggestions = new ObservableCollection<string>();
		int suggests = 0;
		int querys = 0;
		int textChangeds = 0;
		int userInput = 0;
		int programmatic = 0;
		int suggestionChosen = 0;

		public BasicAutoSuggestBox()
		{
			this.InitializeComponent();
		}

		private bool ShouldClear => ShouldClearTextBox.IsChecked.GetValueOrDefault();

		private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			textChangeds += 1;

			switch (args.Reason)
			{
				case AutoSuggestionBoxTextChangeReason.UserInput:
					userInput += 1;

					if (ShouldClear || _suggestions.Count > 10)
					{
						_suggestions.Clear();
					}

					_suggestions.Add(sender.Text + "1");
					_suggestions.Add(sender.Text + "2");
					box1.ItemsSource = _suggestions;
					break;
				case AutoSuggestionBoxTextChangeReason.ProgrammaticChange:
					programmatic += 1;
					break;
				case AutoSuggestionBoxTextChangeReason.SuggestionChosen:
					suggestionChosen += 1;
					break;
			}

			textChanged.Text = $"{textChangeds}:\n\tUserInputs: {userInput}\n\tProgrammatic Changes: {programmatic}\n\tSuggestions Chosen: {suggestionChosen}";
		}

		private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			suggests += 1;
			suggest.Text = "SuggestionChosen: " + suggests + " " + args.SelectedItem.ToString();
		}

		private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			querys += 1;
			query.Text = "QuerySubmitted: " + querys + " " + args.QueryText;
		}
	}
}
