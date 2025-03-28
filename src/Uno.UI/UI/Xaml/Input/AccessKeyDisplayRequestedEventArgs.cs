namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Data for event which occurs when the user requests that access keys be displayed.
	/// </summary>
	public partial class AccessKeyDisplayRequestedEventArgs
	{
		/// <summary>
		/// Initializes a new instance of the AccessKeyDisplayRequestedEventArgs class.
		/// </summary>
		public AccessKeyDisplayRequestedEventArgs()
		{
		}

		/// <summary>
		/// Gets the keys that were pressed to start the access key sequence.
		/// </summary>
		public string PressedKeys { get; internal set; }
	}
}
