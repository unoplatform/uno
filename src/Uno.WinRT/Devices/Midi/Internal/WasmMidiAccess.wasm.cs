using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Devices.Midi.Internal
{
	/// <summary>
	/// Handles WASM MIDI access permission request
	/// </summary>
	public static partial class WasmMidiAccess
	{
		private static bool _webMidiAccessible;

		public static async Task<bool> RequestAsync()
		{
			if (_webMidiAccessible)
			{
				return true;
			}

			var systemExclusiveRequested = WinRTFeatureConfiguration.Midi.RequestSystemExclusiveAccess;

			var result = await NativeMethods.RequestAsync(systemExclusiveRequested);

			_webMidiAccessible = bool.Parse(result);

			return _webMidiAccessible;
		}

		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Uno.Devices.Midi.Internal.WasmMidiAccess.request")]
			internal static partial Task<string> RequestAsync(bool exclusive);
		}
	}
}
