using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Input;

internal interface IHtmlHandleableRoutedEventArgs : IHandleableRoutedEventArgs
{
	/// <summary>
	/// Defines, if this event args if flagged as <see cref="IHandleableRoutedEventArgs.Handled"/>, how the native event will be handled.
	/// </summary>
	/// <remarks>
	/// The default value is expected to be customized by events.
	/// For instance, for pointer events the default will be <see cref="HtmlEventDispatchResult.StopPropagation"/>
	/// while the common default value is <see cref="HtmlEventDispatchResult.StopPropagation"/> | <see cref="HtmlEventDispatchResult.PreventDefault"/>.
	/// </remarks>
	HtmlEventDispatchResult HandledResult { get; set; }
}
