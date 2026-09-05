#if NET10_0_OR_GREATER
using System;
using System.Reflection;
using System.Runtime.InteropServices;

using WebView2Utilities = WebView2.Utilities.WebView2Utilities;

namespace Uno.UI.Runtime.Skia.Win32;

internal static class Win32WebView2Loader
{
	private static readonly object _gate = new();
	private static bool _loaded;

	/// <summary>
	/// Loads WebView2Loader.dll, at most once per process.
	/// </summary>
	/// <remarks>
	/// Deliberately not a static constructor: a type initializer caches its failure, so the first
	/// <see cref="DllNotFoundException"/> would be wrapped in a TypeInitializationException and replayed for the
	/// rest of the process. An explicit guard reports the real error on every attempt. This is reachable without
	/// any WebView2 instance, because <see cref="Microsoft.Web.WebView2.Core.CoreWebView2Environment"/>'s statics
	/// answer whether a browser is installed at all.
	/// </remarks>
	internal static void Ensure()
	{
		lock (_gate)
		{
			if (_loaded)
			{
				return;
			}

			// WebView2Utilities.Initialize probes only next to Environment.ProcessPath, which is the
			// dotnet host's directory when launched as `dotnet app.dll`. In that case, fall back to
			// resolving through the runtime, which honors deps.json and AppContext.BaseDirectory.
			if (WebView2Utilities.Initialize(Assembly.GetEntryAssembly(), throwOnError: false).IsError
				&& !NativeLibrary.TryLoad("WebView2Loader.dll", typeof(WebView2.Functions).Assembly, null, out _))
			{
				throw new DllNotFoundException("Cannot load WebView2Loader.dll. Make sure it's deployed next to the application or resolvable through its deps.json.");
			}

			_loaded = true;
		}
	}
}
#endif
