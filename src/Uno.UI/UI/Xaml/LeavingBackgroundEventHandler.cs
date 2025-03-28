using Windows.ApplicationModel;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Represents the method that will handle the Application.LeavingBackground event.
	/// </summary>
	/// <param name="sender">The object where the handler is attached.</param>
	/// <param name="e">Event data for the event.</param>
	public delegate void LeavingBackgroundEventHandler(object sender, LeavingBackgroundEventArgs e);
}
