namespace Windows.UI.Xaml.Controls
{
	public sealed partial class TextChangedEventArgs : RoutedEventArgs
	{
		internal TextChangedEventArgs(object originalSource, bool isUserModifyingText, bool isTextChangedPending)
			: base(originalSource)
		{
			IsUserModifyingText = isUserModifyingText;
			IsTextChangedPending = isTextChangedPending;
		}

		internal bool IsUserModifyingText { get; }

		/// <summary>i.e. yet another TextChanged will fire soon.</summary>
		internal bool IsTextChangedPending { get; }
	}
}
