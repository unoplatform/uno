namespace Windows.UI.Xaml
{
	/// <summary>
	/// Specifies the interaction experiences for non-pointer
	/// devices such as an Xbox controller or remote control.
	/// </summary>
	public enum ApplicationRequiresPointerMode
	{
		/// <summary>
		/// The default system experience for the input device.
		/// </summary>
		Auto,

		/// <summary>
		/// A pointer-like interaction experience using a mouse
		/// cursor that can be freely moved using non-pointer input devices.
		/// </summary>
		WhenRequested,
	}
}
