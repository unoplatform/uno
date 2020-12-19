namespace Microsoft.UI.Xaml.Controls
{
	public class PipsPagerSelectedIndexChangedEventArgs
	{
		internal PipsPagerSelectedIndexChangedEventArgs(int newPageIndex, int previousPageIndex) =>
			(NewPageIndex, PreviousPageIndex) = (newPageIndex, previousPageIndex);

		public int NewPageIndex { get; }

		public int PreviousPageIndex { get; }
	}
}
