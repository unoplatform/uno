namespace Windows.UI.Xaml.Controls
{
	public partial class ContentDialogClosedEventArgs
	{
		internal ContentDialogClosedEventArgs(ContentDialogResult result)
		{
			Result = result;
		}

		public global::Windows.UI.Xaml.Controls.ContentDialogResult Result { get; }
	}
}
