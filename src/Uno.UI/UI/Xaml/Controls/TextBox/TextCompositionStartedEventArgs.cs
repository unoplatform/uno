namespace Microsoft.UI.Xaml.Controls;

public partial class TextCompositionStartedEventArgs
{
	private readonly int _startIndex;
	private readonly int _length;

	internal TextCompositionStartedEventArgs(int startIndex, int length)
	{
		_startIndex = startIndex;
		_length = length;
	}

	public int StartIndex => _startIndex;

	public int Length => _length;
}
