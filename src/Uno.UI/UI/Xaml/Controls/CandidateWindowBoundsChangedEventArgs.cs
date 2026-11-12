#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class CandidateWindowBoundsChangedEventArgs
{
	private Rect _bounds;

	internal CandidateWindowBoundsChangedEventArgs(Rect bounds) => _bounds = bounds;

	public Rect Bounds => _bounds;
}
