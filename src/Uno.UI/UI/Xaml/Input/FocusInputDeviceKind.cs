namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Specifies the input device types from which input events are received.
	/// </summary>
	public enum FocusInputDeviceKind
	{
		/// <summary>
		/// No input. Used only when the focus is moved programmatically.
		/// </summary>
		None,

		/// <summary>
		/// Mouse input device.
		/// </summary>
		Mouse,

		/// <summary>
		/// Touch input device.
		/// </summary>
		Touch,

		/// <summary>
		/// Pen input device.
		/// </summary>
		Pen,

		/// <summary>
		/// Keyboard input device.
		/// </summary>
		Keyboard,

		/// <summary>
		/// Game controller/remote control input device.
		/// </summary>
		GameController,
	}
}
