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
					string androDisplayName = netInterface.DisplayName;
					// another name, netInterface.Name, would be ignored (seems like == androDisplayName)

					foreach (var interfaceAddress in netInterface.InterfaceAddresses)
					{
						int androPrefixLength = interfaceAddress.NetworkPrefixLength;
						if (interfaceAddress.Address != null)
						{
							string androCanonical = interfaceAddress.Address.CanonicalHostName;
							string androHostName = interfaceAddress.Address.HostName;	// seems like == androCanonical
							bool androIPv46 = (interfaceAddress.Address.GetAddress().Count() == 4);

							// we have all required data from Android, and we can use them
							HostName newHost = new HostName();
							IPInformation newInfo = new IPInformation();
							newInfo.PrefixLength = (byte)androPrefixLength;

							newHost.IPInformation = newInfo;
							newHost.Type = (androIPv46) ? HostNameType.Ipv4 : HostNameType.Ipv6;
							// only these two types; UWP has also 'DomainName' and 'Bluetooth'

							newHost.CanonicalName = androCanonical;
							newHost.RawName = androHostName;
							newHost.DisplayName = androDisplayName;
							uwpList.Add(newHost);    // assuming there would be no duplicates
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
/*﻿
using Android.Content;
#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Connectivity.Internal;
using Android.App;

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
		private static ConnectivityChangeBroadcastReceiver _connectivityChangeBroadcastReceiver;

		private static void StartNetworkStatusChanged()
		{
			_connectivityChangeBroadcastReceiver = new ConnectivityChangeBroadcastReceiver();

#pragma warning disable CS0618 // Type or member is obsolete
			Application.Context.RegisterReceiver(
				_connectivityChangeBroadcastReceiver,
				new IntentFilter(Android.Net.ConnectivityManager.ConnectivityAction));
#pragma warning restore CS0618 // Type or member is obsolete
		}

		private static void StopNetworkStatusChanged()
		{
			if( _connectivityChangeBroadcastReceiver == null)
			{
				return;
			}

			Application.Context.UnregisterReceiver(
				_connectivityChangeBroadcastReceiver);
			_connectivityChangeBroadcastReceiver?.Dispose();
			_connectivityChangeBroadcastReceiver = null;
		}
	}
}
#endif
*/
