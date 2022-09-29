#nullable disable

namespace Uno.UI.Xaml.Input
{
	/// <summary>
	/// Represents Routed event args which can be handled while bubbling/tunneling.
	/// </summary>
	internal interface IHandleableRoutedEventArgs
	{
		/// <summary>
		/// Gets or sets a value that marks the routed event as handled.
		/// A true value for Handled prevents most handlers along the event
		/// route from handling the same event again.
		/// </summary>
		bool Handled { get; set; }
	}
}
