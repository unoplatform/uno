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

		/// <summary>
		/// [WASM ONLY] 
		/// By default an event flagged as <see cref="Handled"/> will prevent the "default behavior" of the browser.
		/// Setting this flag to `true` will make sure that this "default behavior" will not be suppressed.
		/// cf. https://developer.mozilla.org/en-US/docs/Web/API/Event/preventDefault
		/// </summary>
		bool DoNotPreventDefaultIfHandled { get; set; }
	}
}
