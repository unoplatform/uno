#if __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno;

#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

using NativeMethods = __Windows.Networking.Connectivity.NetworkInformation.NativeMethods;
#endif

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.Networking.Connectivity.NetworkInformation";
#endif

		private static void StartNetworkStatusChanged()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StartStatusChanged();
#else
			var command = $"{JsType}.startStatusChanged()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
#endif
		}

		private static void StopNetworkStatusChanged()
		{
#if NET7_0_OR_GREATER
			NativeMethods.StopStatusChanged();
#else
			var command = $"{JsType}.stopStatusChanged()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
#endif
		}

#if NET7_0_OR_GREATER
		[JSExport]
#endif
		public static int DispatchStatusChanged()
		{
			OnNetworkStatusChanged();
			return 0;
		}
	}
}
#endif
