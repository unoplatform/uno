namespace Windows.UI.Xaml
{
	/// <summary>
	/// Specifies the visual feedback used to indicate the UI element with focus when navigating with a keyboard or gamepad.
	/// </summary>
	public enum FocusVisualKind
	{
		/// <summary>
		/// A dotted line rectangle. Also known as "marching ants".
		/// </summary>
		/// <remarks>
		/// The default on Windows 10 November Update
		/// (Windows 10 version 1511 - build number 10.0.10586, codenamed "Threshold 2")
		/// and earlier.
		/// </remarks>
		DottedLine,

		/// <summary>
		/// A solid line rectangle composed of an inner and outer rectangle of contrasting colors.
		/// </summary>
		/// <remarks>
		/// The default on Windows 10 Anniversary Update
		/// (Windows 10 version 1607 - build number 10.0.14393, codenamed "Redstone 1")
		/// and earlier.
		/// DottedLine visual is difficult to see in 10-foot experience.
		/// </remarks>
		HighVisibility,

		/// <summary>
		/// A solid line rectangle, surrounded by a glowing light effect to simulate depth.
		/// </summary>
		/// <remarks>
		/// Opt-in feature for Xbox with Windows 10 version 1803 (codenamed "Redstone 4") and later.
		/// </remarks>
		Reveal,
	}
}
