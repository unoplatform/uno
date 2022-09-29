#nullable disable

namespace Uno.UI.Xaml.Input
{
	internal interface IPreventDefaultHandling
	{
		/// <summary>		
		/// By default an event flagged as <see cref="Handled"/> will prevent the "default behavior" of the browser.
		/// Setting this flag to `true` will make sure that this "default behavior" will not be suppressed.
		/// cf. https://developer.mozilla.org/en-US/docs/Web/API/Event/preventDefault
		/// </summary>
		bool DoNotPreventDefault { get; set; }
	}
}
