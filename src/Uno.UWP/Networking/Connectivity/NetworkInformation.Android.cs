using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Networking.Connectivity.Internal;
using Uno.UI;
using Windows.Extensions;
using Windows.Networking.Connectivity.Internal;
using AndroidConnectivityManager = Android.Net.ConnectivityManager;

namespace Windows.Networking.Connectivity
{
	public partial class NetworkInformation
	{
		private static ConnectivityChangeBroadcastReceiver? _connectivityChangeBroadcastReceiver;
		private static AndroidConnectivityManager? _connectivityManager;
		private static NetworkCallbackListener? _networkCallbackListener;

		private static void StartNetworkStatusChanged()
		{
			VerifyNetworkStateAccess();

			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)
			{
				// Use NetworkCallback method
				_connectivityManager = (AndroidConnectivityManager)ContextHelper.Current.GetSystemService(Context.ConnectivityService)!;
				_networkCallbackListener = new NetworkCallbackListener();
				_connectivityManager.RegisterDefaultNetworkCallback(_networkCallbackListener);
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
				_connectivityChangeBroadcastReceiver = new ConnectivityChangeBroadcastReceiver();

				Application.Context.RegisterReceiver(
					_connectivityChangeBroadcastReceiver,
					new IntentFilter(AndroidConnectivityManager.ConnectivityAction));
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
			}


		}

		private static void StopNetworkStatusChanged()
		{
			if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.N)
			{
				_connectivityManager!.UnregisterNetworkCallback(_networkCallbackListener!);
				_networkCallbackListener?.Dispose();
				_networkCallbackListener = null;
				_connectivityManager?.Dispose();
				_connectivityManager = null;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				if (_connectivityChangeBroadcastReceiver == null)
				{
					return;
				}

				Application.Context.UnregisterReceiver(
					_connectivityChangeBroadcastReceiver);
				_connectivityChangeBroadcastReceiver?.Dispose();
				_connectivityChangeBroadcastReceiver = null;
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public static IReadOnlyList<HostName> GetHostNames()
		{

			Android.OS.StrictMode.ThreadPolicy? prevPolicy = null;

			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.Gingerbread)
			{
				// required to access interfaceAddress.Address.CanonicalHostName (below), without creating new task, outside UI thread
				prevPolicy = Android.OS.StrictMode.GetThreadPolicy();
				var policy = new Android.OS.StrictMode.ThreadPolicy.Builder().PermitAll().Build();
				Android.OS.StrictMode.SetThreadPolicy(policy);
			}


			var uwpList = new List<HostName>();

			var netInterfacesEnum = Java.Net.NetworkInterface.NetworkInterfaces!;
			while (netInterfacesEnum.HasMoreElements)
			{

				var netInterface = (netInterfacesEnum.NextElement() as Java.Net.NetworkInterface)!;
				if (netInterface.InterfaceAddresses != null)
				{
					string? androDisplayName = netInterface.DisplayName;

					// another name, netInterface.Name, would be ignored (seems like == androDisplayName)
					foreach (var interfaceAddress in netInterface.InterfaceAddresses)
					{
						int androPrefixLength = interfaceAddress.NetworkPrefixLength;
						if (interfaceAddress.Address != null)
						{
							string androCanonical = interfaceAddress.Address.CanonicalHostName;
							// seems like == androCanonical
							string androHostName = interfaceAddress.Address.HostName;
							bool androIPv46 = (interfaceAddress.Address.GetAddress()!.Length == 4);

							// we have all required data from Android, and we can use them
							HostName newHost = new HostName();
							IPInformation newInfo = new IPInformation();
							newInfo.PrefixLength = (byte)androPrefixLength;

							newHost.IPInformation = newInfo;

							// only these two types; UWP has also 'DomainName' and 'Bluetooth'
							newHost.Type = (androIPv46) ? HostNameType.Ipv4 : HostNameType.Ipv6;

							newHost.CanonicalName = androCanonical;
							newHost.RawName = androHostName;
							newHost.DisplayName = androDisplayName;
							// assuming there would be no duplicates
							uwpList.Add(newHost);
						}
					}
				}
			}

			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.Gingerbread)
			{
				// returning old policy
				Android.OS.StrictMode.SetThreadPolicy(prevPolicy);
			}

			return uwpList;
		}

		/// <summary>
		/// This raises the NetworkStatusChanged event with a delay.
		/// Delay is necessary as network information is not updated before
		/// the connectivity broadcast on Android is received.
		/// </summary>
		internal static async Task OnDelayedNetworkStatusChanged()
		{
			try
			{
				// await 300ms to ensure that the the connection manager updates
				await Task.Delay(300);
				OnNetworkStatusChanged();
			}
			catch (Exception ex)
			{
				// Task delay should never crash, but just to be sure.
				if (typeof(NetworkInformation).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					typeof(NetworkInformation).Log().LogError($"Could not raise NetworkStatusChanged", ex);

				}
			}
		}

		internal static void VerifyNetworkStateAccess()
		{
			if (!PermissionsHelper.IsDeclaredInManifest(Manifest.Permission.AccessNetworkState))
			{
				throw new UnauthorizedAccessException(
					"To access network information, please add " +
					"android.permission.ACCESS_NETWORK_STATE to application manifest " +
					"or the following attribute in the Android platform head: " +
					"[assembly: UsesPermission(\"android.permission.ACCESS_NETWORK_STATE\")]");
			}
		}
	}
}
