using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Provides data for the NoFocusCandidateFound event.
	/// </summary>
	public partial class NoFocusCandidateFoundEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		internal NoFocusCandidateFoundEventArgs(FocusNavigationDirection direction, FocusInputDeviceKind inputDevice)
		{
			Direction = direction;
			InputDevice = inputDevice;
		}

		/// <summary>
		/// Gets the direction that focus moved from element to element within the app UI.
		/// </summary>
		public FocusNavigationDirection Direction { get; }

		/// <summary>
		/// Gets the input device type from which input events are received.
		/// </summary>
		public FocusInputDeviceKind InputDevice { get; }

		/// <summary>
		/// Gets or sets a value that marks the routed event as handled.
		/// A true value for Handled prevents most handlers along the event
		/// route from handling the same event again.
		/// </summary>
		public bool Handled { get; set; }

		bool IHandleableRoutedEventArgs.Handled
		{
			get => Handled;
			set => Handled = value;
		}
	}
}
