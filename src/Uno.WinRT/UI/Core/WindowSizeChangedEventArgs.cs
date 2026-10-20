using Windows.Foundation;

namespace Windows.UI.Core
{
	/// <summary>
	/// Contains the argument returned by a window size change event.
	/// </summary>
	public sealed partial class WindowSizeChangedEventArgs : ICoreWindowEventArgs
	{
		internal WindowSizeChangedEventArgs(Size newSize)
		{
			Size = newSize;
		}

		/// <summary>
		/// Gets or sets whether the window size event was handled.
		/// </summary>
		public bool Handled { get; set; }

		/// <summary>
		/// Gets the new size of the window in units of effective (view) pixels.
		/// </summary>
		public Size Size { get; }
	}
}
