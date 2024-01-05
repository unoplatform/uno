using System;
using System.Linq;
using System.Net;
using System.Security;
using Android;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Telecom;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.Extensions;
using AndroidConnectivityManager = Android.Net.ConnectivityManager;

namespace Windows.Networking.Connectivity
{
	/// <summary>
	/// Current Android implementation covers the active network connection (Internet connection profile).
	/// </summary>
	public partial class ConnectionProfile
	{
		private AndroidConnectivityManager _connectivityManager;

		internal static ConnectionProfile GetInternetConnectionProfile() => new ConnectionProfile();

		private ConnectionProfile()
		{
			NetworkInformation.VerifyNetworkStateAccess();
			_connectivityManager = (AndroidConnectivityManager)ContextHelper.Current.GetSystemService(Context.ConnectivityService)!;

			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.LollipopMr1)
			{
				var activeNetwork = _connectivityManager.ActiveNetwork;
				if (activeNetwork != null)
				{
					var netCaps = _connectivityManager.GetNetworkCapabilities(activeNetwork);
					if (netCaps != null)
					{
						IsWlanConnectionProfile = netCaps.HasTransport(Android.Net.TransportType.Wifi);
						IsWwanConnectionProfile = netCaps.HasTransport(Android.Net.TransportType.Cellular);
					}
				}
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
				NetworkInfo? info = _connectivityManager.ActiveNetworkInfo;
				if (info?.IsConnected == true)
				{
					IsWwanConnectionProfile = IsConnectionWwan(info.Type);
					IsWlanConnectionProfile = IsConnectionWlan(info.Type);
				}
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
			}
		}

		public ConnectionCost GetConnectionCost()
		{
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
			{ // below API 16, we don't have IsActiveNetworkMetered method
				return new ConnectionCost(NetworkCostType.Unknown);
			}

			return new ConnectionCost(
				_connectivityManager.IsActiveNetworkMetered ?
					NetworkCostType.Fixed : // we currently don't make distinction between variable and fixed metered connection on Android
					NetworkCostType.Unrestricted);
		}

		/// <summary>
		/// Code based on Xamarin.Essentials implementation with some modifications.
		/// </summary>
		/// <returns>Connectivity level.</returns>
		private NetworkConnectivityLevel GetNetworkConnectivityLevelImpl()
		{
			try
			{
				var connectivityLevel = NetworkConnectivityLevel.None;
				var manager = _connectivityManager;

				if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
				{
#pragma warning disable CS0618 // ConnectivityManager.GetAllNetworks() is obsolete in API 31
#pragma warning disable CA1422 // Validate platform compatibility
					var networks = manager.GetAllNetworks();
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // ConnectivityManager.GetAllNetworks() is obsolete in API 31

					// some devices running 21 and 22 only use the older api.
					if (networks.Length == 0 && (int)Build.VERSION.SdkInt < 23)
					{
						return GetConnectivityFromManager();
					}

					foreach (var network in networks)
					{
						if (connectivityLevel == NetworkConnectivityLevel.InternetAccess)
						{
							// Internet access is the highest possible connectivity, short circuit
							return connectivityLevel;
						}

						try
						{
							var capabilities = manager.GetNetworkCapabilities(network);

							if (capabilities == null)
								continue;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
							var info = manager.GetNetworkInfo(network);
							var isInternetNetworkConnectionType = info?.Type is
								ConnectivityType.Ethernet or
								ConnectivityType.Wifi or
								ConnectivityType.Mobile;

							if (info is null || !info.IsAvailable || !isInternetNetworkConnectionType)
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
								continue;

							// Check to see if it has the internet capability
							if (!capabilities.HasCapability(NetCapability.Internet))
							{
								// Doesn't have internet, but local is possible
								if (NetworkConnectivityLevel.LocalAccess > connectivityLevel)
								{
									connectivityLevel = NetworkConnectivityLevel.LocalAccess;
								}
								continue;
							}

							var networkConnectivity = GetConnectivityFromNetworkInfo(info);
							if (networkConnectivity > connectivityLevel)
							{
								connectivityLevel = networkConnectivity;
							}
						}
						catch
						{
							if (this.Log().IsEnabled(LogLevel.Debug))
							{
								this.Log().LogDebug($"Failed to access network information.");
							}
						}
					}
				}
				else
				{
					connectivityLevel = GetConnectivityFromManager();
				}

				return connectivityLevel;
			}
			catch (Exception)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Unable to get connected state, ensure network access permission is declared in manifest.");
				}
				return NetworkConnectivityLevel.None;
			}
		}

		private static bool IsConnectionWlan(ConnectivityType connectivityType)
		{
			return connectivityType == ConnectivityType.Wifi;
		}

		private static bool IsConnectionWwan(ConnectivityType connectivityType)
		{
			return
				connectivityType == ConnectivityType.Wimax ||
				connectivityType == ConnectivityType.Mobile ||
				connectivityType == ConnectivityType.MobileDun ||
				connectivityType == ConnectivityType.MobileHipri ||
				connectivityType == ConnectivityType.MobileMms ||
				connectivityType == ConnectivityType.MobileSupl;
		}

		private NetworkConnectivityLevel GetConnectivityFromManager()
		{
			NetworkConnectivityLevel bestConnectivityLevel = NetworkConnectivityLevel.None;
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
			foreach (var info in _connectivityManager.GetAllNetworkInfo())
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
			{
				var networkConnectivityLevel = GetConnectivityFromNetworkInfo(info);
				if (networkConnectivityLevel > bestConnectivityLevel)
				{
					bestConnectivityLevel = networkConnectivityLevel;
				}
			}
			return bestConnectivityLevel;
		}

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
		private NetworkConnectivityLevel GetConnectivityFromNetworkInfo(NetworkInfo info)
		{
			if (info == null || !info.IsAvailable)
			{
				return NetworkConnectivityLevel.None;
			}
			if (info.IsConnected)
			{
				return NetworkConnectivityLevel.InternetAccess;
			}
			else if (info.IsConnectedOrConnecting)
			{
				return NetworkConnectivityLevel.ConstrainedInternetAccess;
			}
			return NetworkConnectivityLevel.None;
		}
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
	}

}
