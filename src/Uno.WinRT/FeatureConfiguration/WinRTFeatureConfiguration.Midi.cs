namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class Midi
	{
#if __CROSSRUNTIME__
		/// <summary>
		/// Allows MIDI System exclusive access for WebAssembly.
		/// </summary>
		/// <remarks>Applies to WebAssembly only.</remarks>
		public static bool RequestSystemExclusiveAccess { get; set; }
#endif
	}
}
