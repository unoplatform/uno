namespace Windows.UI.Core
{
	/// <summary>
	/// Contains the arguments returned by the event fired when a CoreWindow instance's visibility changes.
	/// </summary>
	public sealed partial class VisibilityChangedEventArgs : ICoreWindowEventArgs
	{
		/// <summary>
		/// Gets or sets a value indicating whether the VisibilityChanged event was handled.
		/// </summary>
		public bool Handled { get; set; }

		/// <summary>
		/// Gets whether the window is visible or not.
		/// </summary>
		public bool Visible { get; }
	}
}
