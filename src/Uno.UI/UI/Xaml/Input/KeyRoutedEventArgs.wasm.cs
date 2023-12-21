using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Input;

public partial class KeyRoutedEventArgs : IHtmlHandleableRoutedEventArgs
{
	/// <inheritdoc />
	HtmlEventDispatchResult IHtmlHandleableRoutedEventArgs.HandledResult { get; set; } = HtmlEventDispatchResult.StopPropagation | HtmlEventDispatchResult.PreventDefault;
}
