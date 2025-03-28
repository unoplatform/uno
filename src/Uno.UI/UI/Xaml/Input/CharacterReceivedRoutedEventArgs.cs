using Uno.UI.Xaml.Input;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Data for event which occurs when a single, composed character is received by the input queue.
	/// </summary>
	public partial class CharacterReceivedRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		internal CharacterReceivedRoutedEventArgs(char character, CorePhysicalKeyStatus keyStatus)
		{
			Character = character;
			KeyStatus = keyStatus;
		}

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

		/// <summary>
		/// Gets the composed character associated with the UIElement.CharacterReceived event.
		/// </summary>
		public char Character { get; }

		/// <summary>
		/// Gets the status of the physical key that raised the character-received event.
		/// </summary>
		public CorePhysicalKeyStatus KeyStatus { get; }
	}
}
