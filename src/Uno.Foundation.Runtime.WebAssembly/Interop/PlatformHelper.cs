using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.Foundation.Runtime.WebAssembly.Interop
{
	internal class PlatformHelper
	{
		internal static bool IsWebAssembly { get; }
			// Origin of the value : https://github.com/mono/mono/blob/a65055dbdf280004c56036a5d6dde6bec9e42436/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs#L115
			= RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")) // Legacy Value (Bootstrapper 1.2.0-dev.29 or earlier).
			|| RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));

		internal static bool IsNetCore { get; } = Type.GetType("System.Runtime.Loader.AssemblyLoadContext") != null;

	}
}
