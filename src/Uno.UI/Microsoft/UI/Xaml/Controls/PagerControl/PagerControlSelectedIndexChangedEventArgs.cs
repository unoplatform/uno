namespace Microsoft.UI.Xaml.Controls
{
	public class PagerControlSelectedIndexChangedEventArgs
	{
		public PagerControlSelectedIndexChangedEventArgs(int previousIndex, int newIndex)
		{
			PreviousPageIndex = previousIndex;
			NewPageIndex = newIndex;
		}

		public int PreviousPageIndex { get; }

		public int NewPageIndex { get; }
	}
}
