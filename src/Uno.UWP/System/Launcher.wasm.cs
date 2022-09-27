#if __WASM__
using System;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			var command = $"Uno.UI.WindowManager.current.open(\"{uri.OriginalString}\");";
			var result = WebAssemblyRuntime.InvokeJS(command);
			return Task.FromResult(result == "True");
		}
	}
}
#endif
