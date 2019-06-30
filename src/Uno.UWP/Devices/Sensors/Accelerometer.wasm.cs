#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Sensors
{
	public partial class Accelerometer
	{
		private static Accelerometer TryCreateInstance()
		{
			var command = $"Uno.UI.WindowManager.current.open(\"{uri.OriginalString}\");";
			var result = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
			return result == "True";
			return null;
		}
	}
}
#endif
