namespace Microsoft.UI.Xaml.Controls
{
	public partial class ContentDialogClosedEventArgs
	{
		internal ContentDialogClosedEventArgs(ContentDialogResult result)
		{
			Result = result;
		}

		public global::Microsoft.UI.Xaml.Controls.ContentDialogResult Result { get; }
	}
}
