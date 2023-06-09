using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;
#else
using static Uno.Foundation.WebAssemblyRuntime;
#endif

namespace Uno.Devices.Midi.Internal
{
	/// <summary>
	/// Handles WASM MIDI access permission request
	/// </summary>
	public static partial class WasmMidiAccess
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Uno.Devices.Midi.Internal.WasmMidiAccess";
#endif

		private static bool _webMidiAccessible;

		public static async Task<bool> RequestAsync()
		{
			if (_webMidiAccessible)
			{
				return true;
			}

			var systemExclusiveRequested = WinRTFeatureConfiguration.Midi.RequestSystemExclusiveAccess;

#if NET7_0_OR_GREATER
			var result = await NativeMethods.RequestAsync(systemExclusiveRequested);
#else
			var serializedRequest = systemExclusiveRequested.ToString().ToLowerInvariant();
			var command = $"{JsType}.request({serializedRequest})";
			var result = await InvokeAsync(command);
#endif

			_webMidiAccessible = bool.Parse(result);

			return _webMidiAccessible;
		}

#if NET7_0_OR_GREATER
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Uno.Devices.Midi.Internal.WasmMidiAccess.request")]
			internal static partial Task<string> RequestAsync(bool exclusive);
		}
#endif
	}
}
