using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.Foundation.Runtime.WebAssembly.Interop
{
	internal class PlatformHelper
	{
		private static bool _isNetCore;
		private static bool _initialized;
		private static bool _isWebAssembly;

		/// <summary>
		/// Determines if the platform is runnnig WebAssembly
		/// </summary>
		internal static bool IsWebAssembly
		{
			get
			{
				EnsureInitialized();
				return _isWebAssembly;
			}
		}

		/// <summary>
		/// Determines if the current runtime is running on .NET Core or 5 and later
		/// </summary>
		internal static bool IsNetCore
		{
			get
			{
				EnsureInitialized();
				return _isNetCore;
			}
		}

		/// <summary>
		/// Initialization is performed explicitly to avoid a mono/mono issue regarding .cctor and FullAOT
		/// see https://github.com/unoplatform/uno/issues/5395
		/// </summary>
		private static void EnsureInitialized()
		{
			if (!_initialized)
			{
				_initialized = true;

				_isNetCore = Type.GetType("System.Runtime.Loader.AssemblyLoadContext") != null;

				// Origin of the value : https://github.com/mono/mono/blob/a65055dbdf280004c56036a5d6dde6bec9e42436/mcs/class/corlib/System.Runtime.InteropServices.RuntimeInformation/RuntimeInformation.cs#L115
				_isWebAssembly =
					RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")) // Legacy Value (Bootstrapper 1.2.0-dev.29 or earlier).
					|| RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));
			}
		}
	}
}
