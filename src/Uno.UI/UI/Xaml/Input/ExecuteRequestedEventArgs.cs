namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Provides event data for the ExecuteRequested event.
	/// </summary>
	public partial class ExecuteRequestedEventArgs
	{
		internal ExecuteRequestedEventArgs()
		{
		}

		/// <summary>
		/// Gets the command parameter passed into the Execute method that raised this event.
		/// </summary>
		public object Parameter { get; internal set; }
	}
}
