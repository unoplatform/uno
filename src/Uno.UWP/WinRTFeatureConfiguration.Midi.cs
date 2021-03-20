namespace Uno
{
	partial class WinRTFeatureConfiguration
	{
		public static class Midi
		{
#if __WASM__
			/// <summary>
			/// Allows MIDI System exclusive access for WebAssembly.
			/// </summary>
			public static bool RequestSystemExclusiveAccess { get; set; }
#endif
		}
	}
}
