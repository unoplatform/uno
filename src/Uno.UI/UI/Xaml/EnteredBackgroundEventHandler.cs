using Windows.ApplicationModel;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Represents the method that will handle the Application.EnteredBackground event.
	/// </summary>
	/// <param name="sender">The object where the handler is attached.</param>
	/// <param name="e">Event data for the event.</param>
	public delegate void EnteredBackgroundEventHandler(object sender, EnteredBackgroundEventArgs e);
}
