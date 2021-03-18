// MUX reference PagerControlSelectedIndexChangedEventArgs.cpp, commit a08f765

namespace Microsoft.UI.Xaml.Controls
{
	public partial class PagerControlSelectedIndexChangedEventArgs
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
