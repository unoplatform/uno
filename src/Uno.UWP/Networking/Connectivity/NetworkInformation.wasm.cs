#if __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno;

namespace Windows.Networking.Connectivity
{
    public partial class NetworkInformation
    {
		private const string JsType = "Windows.Networking.Connectivity.NetworkInformation";

		private static void StartNetworkStatusChanged()
		{
			var command = $"{JsType}.startStatusChanged()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}

		private static void StopNetworkStatusChanged()
		{
			var command = $"{JsType}.stopStatusChanged()";
			Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
		}
		
		[Preserve]
		public static int DispatchStatusChanged()
		{
			_networkStatusChanged?.Invoke(null);
			return 0;
		}
	}
}
#endif
