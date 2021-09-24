using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the FocusEngaged event.
	/// </summary>
	public partial class FocusEngagedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		internal FocusEngagedEventArgs()
		{
		}

		/// <summary>
		/// Gets or sets a value that marks the routed event as handled. A true value for Handled prevents most handlers along the event route from handling the same event again.
		/// </summary>
		public bool Handled { get; set; }

		bool IHandleableRoutedEventArgs.Handled
		{
			get => Handled;
			set => Handled = value;
		}
	}
}
