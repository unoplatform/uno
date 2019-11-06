#if __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.System
{
	public class Launcher
	{
		public static async Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			var command = $"Uno.UI.WindowManager.current.open(\"{uri.OriginalString}\");";
			var result = WebAssemblyRuntime.InvokeJS(command);
			return result == "True";
		}
	}
}
#endif
