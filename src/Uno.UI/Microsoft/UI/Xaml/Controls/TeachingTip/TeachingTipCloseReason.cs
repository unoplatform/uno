// MUX Reference TeachingTip.idl, commit de78834

#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Defines constants that indicate the cause of the TeachingTip closure.
	/// </summary>
	public enum TeachingTipCloseReason
	{
		/// <summary>
		/// The teaching tip was closed by the user clicking the close button.
		/// </summary>
		CloseButton,

		/// <summary>
		/// The teaching tip was closed by light-dismissal.
		/// </summary>
		LightDismiss,

		/// <summary>
		/// The teaching tip was programmatically closed.
		/// </summary>
		Programmatic,
	}
}
