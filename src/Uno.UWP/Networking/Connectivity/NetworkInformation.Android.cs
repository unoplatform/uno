#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
		public static ConnectionProfile GetInternetConnectionProfile() => new ConnectionProfile(true);

		public static IReadOnlyList<Windows.Networking.HostName> GetHostNames()
		{

			Android.OS.StrictMode.ThreadPolicy prevPolicy = null;

			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.Gingerbread)
			{   // required to access interfaceAddress.Address.CanonicalHostName (below), without creating new task, outside UI thread
				prevPolicy = Android.OS.StrictMode.GetThreadPolicy();
				var policy = new Android.OS.StrictMode.ThreadPolicy.Builder().PermitAll().Build();
				Android.OS.StrictMode.SetThreadPolicy(policy);
			}


			var uwpList = new List<HostName>();

			var netInterfacesEnum = Java.Net.NetworkInterface.NetworkInterfaces;
			while (netInterfacesEnum.HasMoreElements)
			{

				var netInterface = netInterfacesEnum.NextElement() as Java.Net.NetworkInterface;
				if (netInterface.InterfaceAddresses != null)
				{
					string mamDisplayName = netInterface.DisplayName;
					string mamInneName = netInterface.Name;

					foreach (var interfaceAddress in netInterface.InterfaceAddresses)
					{
						int mamPrefixLength = interfaceAddress.NetworkPrefixLength;
						if (interfaceAddress.Address != null)
						{
							string mamCanonical = interfaceAddress.Address.CanonicalHostName;
							string mamHostName = interfaceAddress.Address.HostName;
							bool mamIPv46 = (interfaceAddress.Address.GetAddress().Count() == 4);

							// mamy dane, zrobimy sobie wstawienie ich
							HostName newHost = new HostName();
							IPInformation newInfo = new IPInformation();
							newInfo.PrefixLength = (byte)mamPrefixLength;

							newHost.IPInformation = newInfo;
							newHost.Type = (mamIPv46) ? HostNameType.Ipv4 : HostNameType.Ipv6;
							// ale kiedy domain? kiedy bluetooth?

							newHost.CanonicalName = mamCanonical;
							newHost.RawName = mamHostName;
							newHost.RawName = mamInneName;
							newHost.DisplayName = mamDisplayName;
							uwpList.Add(newHost);    // ale tak, zeby nie bylo powtorek!
						}
					}
				}
			}

			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.Gingerbread)
			{ // returning old policy
				Android.OS.StrictMode.SetThreadPolicy(prevPolicy);
			}

			return uwpList;
		}
	}
}

#endif
