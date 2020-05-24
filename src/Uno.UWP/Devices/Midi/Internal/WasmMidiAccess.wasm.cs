#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Uno.Foundation.WebAssemblyRuntime;

namespace Uno.Devices.Midi.Internal
{
	/// <summary>
	/// Needs to be public to get
	/// </summary>
	public static class WasmMidiAccess
	{
		private const string JsType = "Uno.Devices.Midi.Internal.WasmMidiAccess";

		private static bool _webMidiAccessible = false;

		internal static async Task<bool> RequestAsync()
		{
			if (_webMidiAccessible) return true;

			//TODO: Support for SYS EX MESSAGES in wasm request
			//there are no access requests currently waiting for resolution, we need to invoke the check in JS
			var command = $"{JsType}.request()";
			var result = await InvokeAsync(command);
			return bool.Parse(result);
		}
	}
}
#endif
