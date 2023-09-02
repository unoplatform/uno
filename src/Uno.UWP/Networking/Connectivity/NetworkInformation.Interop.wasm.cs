using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Networking.Connectivity
{
	internal partial class NetworkInformation
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.Networking.Connectivity.NetworkInformation";

			[JSImport($"{JsType}.startStatusChanged")]
			internal static partial void StartStatusChanged();

			[JSImport($"{JsType}.stopStatusChanged")]
			internal static partial void StopStatusChanged();
		}
	}
}
