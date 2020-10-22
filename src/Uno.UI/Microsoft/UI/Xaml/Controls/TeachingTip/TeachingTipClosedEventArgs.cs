// MUX Reference TeachingTipClosedEventArgs.cpp, commit 838a0cc 

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides data for the Closed event.
	/// </summary>
	public class TeachingTipClosedEventArgs
	{
		internal TeachingTipClosedEventArgs(TeachingTipCloseReason reason) =>
			Reason = reason;

		/// <summary>
		/// Gets a constant that specifies whether the cause of the Closed
		/// event was due to user interaction (Close button click), light-dismissal, or programmatic closure.
		/// </summary>
		public TeachingTipCloseReason Reason { get; } = TeachingTipCloseReason.CloseButton;
	}
}
