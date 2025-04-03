using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Input;

internal interface IHtmlHandleableRoutedEventArgs : IHandleableRoutedEventArgs
{
	/// <summary>
	/// Defines, if this event args if flagged as <see cref="IHandleableRoutedEventArgs.Handled"/>, how the native event will be handled.
	/// </summary>
	/// <remarks>
	/// The default value is expected to be customized by events.
	/// For instance, for pointer events the default will be <see cref="Microsoft.UI.Xaml.HtmlEventDispatchResult.StopPropagation"/>
	/// while the common default value is <see cref="Microsoft.UI.Xaml.HtmlEventDispatchResult.StopPropagation"/> | <see cref="Microsoft.UI.Xaml.HtmlEventDispatchResult.PreventDefault"/>.
	/// </remarks>
	Microsoft.UI.Xaml.HtmlEventDispatchResult HandledResult { get; set; }
}
