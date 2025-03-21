namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Data for event which occurs when a user completes an access key sequence.
	/// </summary>
	public partial class AccessKeyInvokedEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the AccessKeyInvokedEventArgs class.
		/// </summary>
		public AccessKeyInvokedEventArgs()
		{
		}

		/// <summary>
		/// Gets or sets a value that marks the routed event as handled.
		/// A true value for Handled prevents most handlers along the event
		/// route from handling the same event again.
		/// </summary>
		public bool Handled { get; set; }
	}
}
