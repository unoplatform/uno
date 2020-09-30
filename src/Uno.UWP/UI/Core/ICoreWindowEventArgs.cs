namespace Windows.UI.Core
{
	/// <summary>
	/// Defines the set of arguments returned to an app after a window input or behavior event.
	/// </summary>
	public partial interface ICoreWindowEventArgs
	{
		/// <summary>
		/// Specifies the property that gets or sets whether the event was handled.
		/// </summary>
		bool Handled { get; set; }
	}
}
