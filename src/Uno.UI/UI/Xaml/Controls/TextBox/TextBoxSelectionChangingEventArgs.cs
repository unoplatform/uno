namespace Microsoft.UI.Xaml.Controls
{
	public partial class TextBoxSelectionChangingEventArgs
	{
		internal TextBoxSelectionChangingEventArgs(int start, int length)
		{
			SelectionStart = start;
			SelectionLength = length;
		}

		public bool Cancel { get; set; }

		public int SelectionLength { get; }
		public int SelectionStart { get; }
	}
}
