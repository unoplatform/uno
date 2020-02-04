#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Networking.Connectivity
{

	public partial class ConnectionProfile
	{
		public bool IsWwanConnectionProfile { get; internal set; }
		public bool IsWlanConnectionProfile { get; internal set; }

		internal ConnectionProfile(bool isInternet)
		{
			// using such constructor, we can fill data only when getting object from Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile()

			// test Manifest (Access_Network_State)
			var context = Android.App.Application.Context;
			var packageInfo = context.PackageManager.GetPackageInfo(
									context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
			var requestedPermissions = packageInfo?.RequestedPermissions;
			if (requestedPermissions is null)
			{
				throw new UnauthorizedAccessException("no ACCESS_NETWORK_STATE permission defined in Manifest");
			}

			bool bInManifest = requestedPermissions.Any(p => p.Equals(Android.Manifest.Permission.AccessNetworkState, StringComparison.OrdinalIgnoreCase));
			if (!bInManifest)
			{
				throw new UnauthorizedAccessException("no ACCESS_NETWORK_STATE permission defined in Manifest");
			}

			// access network data

			var cm = (Android.Net.ConnectivityManager)context.GetSystemService(Android.Content.Context.ConnectivityService);
			Android.Net.NetworkInfo info = cm.ActiveNetworkInfo;
			if (info == null) return;
			if (!info.IsConnected) return;

			switch (info.Subtype)
			{   // mapping Android types to UWP types
				case Android.Net.ConnectivityType.Mobile:       // =0
				case Android.Net.ConnectivityType.MobileMms:    // =2
				case Android.Net.ConnectivityType.MobileSupl:   // =3
				case Android.Net.ConnectivityType.MobileDun:    // =4
				case Android.Net.ConnectivityType.MobileHipri:  // =5
					IsWwanConnectionProfile = true;
					break;
				case Android.Net.ConnectivityType.Wifi:         // =1
				case Android.Net.ConnectivityType.Wimax:        // =6
					IsWlanConnectionProfile = true;
					break;
			}
			// other Android types: Bluetooth=7, Dummy=8, Ethernet=9, Vpn=17

		}
	}

}

#endif
