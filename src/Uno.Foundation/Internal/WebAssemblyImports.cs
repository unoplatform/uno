using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace Uno.Foundation;

internal static partial class WebAssemblyImports
{
	[JSImport("globalThis.eval")]
	internal static partial string EvalString(string js);

	[JSImport("globalThis.eval")]
	internal static partial bool EvalBool(string js);

	[JSImport("globalThis.window.location.href")]
	internal static partial string GetWindowLocationHref();

	[JSImport("globalThis.window.location.protocol")]
	internal static partial string GetWindowLocationProtocol();

	[JSImport("globalThis.window.location.hostname")]
	internal static partial string GetWindowLocationHostname();

	[JSImport("globalThis.window.location.port")]
	internal static partial string GetWindowLocationPort();
}
