using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>Specifies when the Click event should be raised for a control.</summary>
	[WebHostHidden]
	[ContractVersion(typeof(UniversalApiContract), 65536U)]
	public enum ClickMode
	{
		/// <summary>Specifies that the Click event should be raised when the left mouse button is pressed and released, and the mouse pointer is over the control. If you are using the keyboard, specifies that the Click event should be raised when the SPACEBAR or ENTER key is pressed and released, and the control has keyboard focus.</summary>
		Release,
		/// <summary>Specifies that the Click event should be raised when the mouse button is pressed and the mouse pointer is over the control. If you are using the keyboard, specifies that the Click event should be raised when the SPACEBAR or ENTER key is pressed and the control has keyboard focus.</summary>
		Press,
		/// <summary>Specifies that the Click event should be raised when the mouse pointer moves over the control.</summary>
		Hover,
	}
}
