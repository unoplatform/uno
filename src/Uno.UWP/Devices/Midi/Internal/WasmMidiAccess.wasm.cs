using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Uno.Devices.Midi.Internal
{
	/// <summary>
	/// Handles WASM MIDI access permission request
	/// </summary>
	public static class WasmMidiAccess
	{
		private const string JsType = "Uno.Devices.Midi.Internal.WasmMidiAccess";

		private static bool _webMidiAccessible = false;

		public static async Task<bool> RequestAsync()
		{
			if (_webMidiAccessible)
			{
				return true;
			}

			var systemExclusiveRequested = WinRTFeatureConfiguration.Midi.RequestSystemExclusiveAccess;
			var serializedRequest = systemExclusiveRequested.ToString().ToLowerInvariant();
			var command = $"{JsType}.request({serializedRequest})";
			var result = await InvokeAsync(command);
			_webMidiAccessible = bool.Parse(result);
			return _webMidiAccessible;
		}
	}
}
