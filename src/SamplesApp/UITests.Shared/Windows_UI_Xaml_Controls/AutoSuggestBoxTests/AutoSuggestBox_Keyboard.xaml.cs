using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using SamplesApp;
using UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Perception.Spatial;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UITests.Shared.Windows_UI_Xaml_Controls.AutoSuggestBoxTests;

[Sample("AutoSuggestBox", IsManualTest = false)]
public sealed partial class AutoSuggestBox_Keyboard : UserControl
{
	public class Suggestion
	{
		public string SuggestionText { get; }

		public Suggestion(string t) { SuggestionText = t; }
	}

	public AutoSuggestBox_Keyboard()
	{
		this.InitializeComponent();

		((AutoSuggestConverter)Resources["AutoSuggestConverter"]).AutoSuggestBox = TestAutoSuggestBox;

		Book.AuthorChanged += (s, e) =>
		{
			OutputTextBlock.Text = $"New author = '{Book.Author}'";
		};
	}

	public Book Book { get; } = new Book { Author = new Author { Name = "A0" } };

	private void AutoSuggestBox_TextChanged(AutoSuggestBox s, AutoSuggestBoxTextChangedEventArgs e)
	{
		if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
		{
			s.ItemsSource = Author.All.Where(a => a.Name.StartsWith(s.Text?.Trim())).ToArray();
		}
	}

	private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox s, AutoSuggestBoxQuerySubmittedEventArgs e)
	{
		s.Text = e.ChosenSuggestion?.ToString() ?? "";
		s.ItemsSource = null;

		await new ContentDialog
		{
			Content = "Type 'String' is wrong here! Type='" + (e.ChosenSuggestion?.GetType().Name + "'" ?? "NULL"),
			PrimaryButtonText = "OK",
			RequestedTheme = ElementTheme.Default,
			XamlRoot = XamlRoot,
		}.ShowAsync();
	}
}

public class AutoSuggestConverter : IValueConverter
{
	public AutoSuggestBox AutoSuggestBox { get; set; }

	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value == null)
			return null;

		return value.ToString();
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		var result = (AutoSuggestBox?.ItemsSource as IList)?.Cast<object>().FirstOrDefault(i => i.ToString() == (value as string));
		return result;
	}
}
