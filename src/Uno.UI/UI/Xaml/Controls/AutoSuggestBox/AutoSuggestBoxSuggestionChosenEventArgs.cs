namespace Windows.UI.Xaml.Controls
{
	public partial class AutoSuggestBoxSuggestionChosenEventArgs
	{
		public object SelectedItem { get; }

		public AutoSuggestBoxSuggestionChosenEventArgs() : base()
		{
		}

		internal AutoSuggestBoxSuggestionChosenEventArgs(object selectedItem)
		{
			SelectedItem = selectedItem;
		}
	}
}
