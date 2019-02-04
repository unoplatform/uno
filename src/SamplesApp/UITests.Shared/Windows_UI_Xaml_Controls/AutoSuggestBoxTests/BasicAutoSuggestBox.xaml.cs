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

		public BasicAutoSuggestBox()
		{
			this.InitializeComponent();
		}

		private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
			{
				_suggestions.Clear();
				_suggestions.Add(sender.Text + "1");
				_suggestions.Add(sender.Text + "2");
			}
			box1.ItemsSource = _suggestions;
		}

		private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			result.Text = args.SelectedItem.ToString();
		}
	}
}
