namespace Windows.Devices.Haptics
{
	/// <summary>
	/// Provides a set of well-known haptic waveform types
	/// (based on the Haptic Usage Page HID specification).
	/// </summary>
	/// <remarks>Values based on manual test.</remarks>
	public static partial class KnownSimpleHapticsControllerWaveforms
	{
		/// <summary>
		/// Gets a buzz waveform that is generated continuously
		/// without interruption until terminated.
		/// </summary>
		public static ushort BuzzContinuous => 4100;

		/// <summary>
		/// Gets a click waveform.
		/// </summary>
		public static ushort Click => 4099;

		/// <summary>
		/// Gets a press waveform.
		/// </summary>
		public static ushort Press => 40102;

		/// <summary>
		/// Gets a release waveform.
		/// </summary>
		public static ushort Release => 4013;

		/// <summary>
		/// Gets a rumble waveform that is generated continuously
		/// without interruption until terminated.
		/// </summary>
		public static ushort RumbleContinuous => 40101;
	}
}
