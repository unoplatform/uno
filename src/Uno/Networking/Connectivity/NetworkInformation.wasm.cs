using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;
using Uno;

using NativeMethods = __Windows.Networking.Connectivity.NetworkInformation.NativeMethods;

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
		private static void StartNetworkStatusChanged()
		{
			NativeMethods.StartStatusChanged();
		}

		private static void StopNetworkStatusChanged()
		{
			NativeMethods.StopStatusChanged();
		}

		[JSExport]
		internal static int DispatchStatusChanged()
		{
			OnNetworkStatusChanged();
			return 0;
		}
	}
}
