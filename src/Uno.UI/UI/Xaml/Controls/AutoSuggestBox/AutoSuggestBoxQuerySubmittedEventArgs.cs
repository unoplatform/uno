#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public partial class AutoSuggestBoxQuerySubmittedEventArgs
	{
		public object ChosenSuggestion { get; }

		public string QueryText { get; }

		public AutoSuggestBoxQuerySubmittedEventArgs() : base()
		{
		}

		internal AutoSuggestBoxQuerySubmittedEventArgs(object chosenSuggestion, string queryText)
		{
			ChosenSuggestion = chosenSuggestion;
			QueryText = queryText;
		}
	}
}
