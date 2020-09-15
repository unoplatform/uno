namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the Closed event.
	/// </summary>
	public class TeachingTipClosedEventArgs
    {
		/// <summary>
		/// Gets a constant that specifies whether the cause of the Closed
		/// event was due to user interaction (Close button click), light-dismissal, or programmatic closure.
		/// </summary>
		public TeachingTipCloseReason Reason { get; }
	}
}
