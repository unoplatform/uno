namespace Windows.UI.Core
{
	/// <summary>
	/// Contains the set of arguments returned to an app after a window input or behavior event.
	/// </summary>
	public sealed partial class CoreWindowEventArgs : ICoreWindowEventArgs
	{
		/// <summary>
		/// Specifies the property that gets or sets whether the event was handled.
		/// </summary>
		public bool Handled { get; set; }
	}
}
