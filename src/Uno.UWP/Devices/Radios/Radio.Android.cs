#nullable enable

#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;


namespace Windows.Devices.Radios
{

	public partial class Radio
	{

		public RadioKind Kind { get; internal set; }

		public string Name { get; internal set; }

		public RadioState State { get; internal set; }

		private Radio()
		{
			Name = "";
		}


		private void AddLogMain(Microsoft.Extensions.Logging.LogLevel logLevel, string message)
		{
			if (this.Log().IsEnabled(logLevel))
			{
				this.Log().Debug("Could not get the bluetooth default adapter (older Android), assuming no adapter");
			}
		}
		private static void AddLog(Microsoft.Extensions.Logging.LogLevel logLevel, string message)
		{
			var radio = new Radio();
			radio.AddLogMain(logLevel, message);
		}

		private static Radio? GetRadiosBluetooth()
		{
			var context = Android.App.Application.Context;

			var pkgManager = context.PackageManager;
			if (pkgManager is null)
			{
				// required by #nullable
				return null;
			}

			bool hasBluetooth = pkgManager.HasSystemFeature(Android.Content.PM.PackageManager.FeatureBluetooth);
			bool hasBluetoothLE = pkgManager.HasSystemFeature(Android.Content.PM.PackageManager.FeatureBluetoothLe);
			if (!(hasBluetooth || hasBluetoothLE))
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Debug,
					"GetRadiosBluetooth(): this device doesn't have any Bluetooth interface");
				return null;
			}

			var radio = new Radio();
			radio.Kind = RadioKind.Bluetooth;
			radio.Name = "Bluetooth";  // name as in UWP

			if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.Bluetooth))
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Warning, 
					"GetRadiosBluetooth(): no 'Bluetooth' permission, check app Manifest");
				radio.State = RadioState.Unknown;
				return radio;
			}

			if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.JellyBeanMr1)
			{
				// deprecated in API 31
#pragma warning disable CS0618 // Type or member is obsolete
				var adapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
#pragma warning restore CS0618 // Type or member is obsolete
				if (adapter == null)
				{
					AddLog(Microsoft.Extensions.Logging.LogLevel.Warning,
						"GetRadiosBluetooth(): should not happen: Android inconsistence, device has Bluetooth capability but no Bluetooth adapter (old API)");
					return null;
				}
				else if (!adapter.IsEnabled)
				{
					radio.State = RadioState.Off;
				}
				else
				{
					radio.State = RadioState.On;
				}

				return radio;
			}
			else
			{
				if (Android.App.Application.Context.GetSystemService(Android.Content.Context.BluetoothService) is Android.Bluetooth.BluetoothManager bluetoothManager)
				{
					var adapter = bluetoothManager.Adapter;
					if (adapter == null)
					{
						AddLog(Microsoft.Extensions.Logging.LogLevel.Warning,
							"GetRadiosBluetooth(): should not happen: Android inconsistence, device has Bluetooth capability but no Bluetooth adapter (new API)");
						return null;
					}
					if (adapter.State != Android.Bluetooth.State.On)
					{
						radio.State = RadioState.Off;
					}
					else
					{
						radio.State = RadioState.On;
					}

					return radio;
				}
				else
				{
					AddLog(Microsoft.Extensions.Logging.LogLevel.Warning,
						"GetRadiosBluetooth(): should not happen: Android inconsistence, device has Bluetooth capability but no BluetoothService (new API)");
					return null;
				}
			}

		}

		private static bool? GetRadioStateForNet(Android.Net.ConnectivityManager connManager, RadioKind radioKind)
		{
			var activeNetwork = connManager.ActiveNetwork;
			if (activeNetwork is null)
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Warning,
					"GetRadioStateForNet(): ActiveNetwork is null, assuming no radio");
				return null;
			}

			var netCaps = connManager.GetNetworkCapabilities(activeNetwork);
			if (netCaps is null)
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Debug,
					"GetRadioStateForNet(): GetNetworkCapabilities is null, assuming no radio");
				return null;
			}

			if (radioKind == RadioKind.WiFi)
			{
				return netCaps.HasTransport(Android.Net.TransportType.Wifi);
			}
			else
			{
				return netCaps.HasTransport(Android.Net.TransportType.Cellular);
			}
		}

		private static Radio? GetRadiosWiFiOrCellular(RadioKind radioKind)
		{
			var context = Android.App.Application.Context;

			var pkgManager = context.PackageManager;
			if (pkgManager is null)
			{
				// required by #nullable
				return null;
			}

			string systemFeature;
			if (radioKind == RadioKind.WiFi)
			{
				systemFeature = Android.Content.PM.PackageManager.FeatureWifi;
			}
			else
			{
				systemFeature = Android.Content.PM.PackageManager.FeatureTelephony;
			}

			if (!pkgManager.HasSystemFeature(systemFeature))
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Debug,
					"GetRadiosWiFiOrCellular(" + radioKind.ToString() + "): this device doesn't have any interface of this kind");
				return null;
			}

			var radio = new Radio();
			radio.Kind = radioKind;
			if (radioKind == RadioKind.WiFi)
			{
				radio.Name = "Wi-Fi";  // name as in UWP
			}
			else
			{
				radio.Name = "Cellular";  // I have "Cellular 6" (Lumia 532), but maybe "6" doesn't mean anything
			}

			if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.AccessNetworkState))
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Warning,
					"GetRadiosWiFiOrCellular(" + radioKind.ToString() + "): no 'AccessNetworkState' permission, check app Manifest");
				
				radio.State = RadioState.Unknown;
				return radio;
			}

			var sysService = context.GetSystemService(Android.Content.Context.ConnectivityService);
			if (sysService is null)
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Warning,
					"GetRadiosWiFiOrCellular(" + radioKind.ToString() + "): GetSystemService returns null, assuming no radio");
				return null;
			}

			var connManager = (Android.Net.ConnectivityManager)sysService;
			if (connManager is null)
			{
				AddLog(Microsoft.Extensions.Logging.LogLevel.Warning,
					"GetRadiosWiFiOrCellular(" + radioKind.ToString() + "): Android.Net.ConnectivityManager is null, assuming no WiFi radio");
				return null;
			}

			if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.LollipopMr1)
			{
				// since API 23
				var radiostate = GetRadioStateForNet(connManager, radioKind);
				if (radiostate is null || !radiostate.HasValue)
				{
					// detailed Log entry already done in GetRadioStateForNet
					return null;
				}

				if (radiostate.Value)
				{
					radio.State = RadioState.On;
				}
				else
				{
					radio.State = RadioState.Off;
				}

				return radio;
			}
			else
			{
				// for Android API 1 to 28 (deprecated in 29)
#pragma warning disable CS0618 // Type or member is obsolete
				var netInfo = connManager.ActiveNetworkInfo;
				if (netInfo is null)
				{
					radio.State = RadioState.Off;
				}
				else
				{
					radio.State = RadioState.Off;
					if (radioKind == RadioKind.WiFi)
					{
						if (netInfo.Type == Android.Net.ConnectivityType.Wifi ||
							netInfo.Type == Android.Net.ConnectivityType.Wimax)
						{
							radio.State = RadioState.On;
						}
					}
					else
					{
						if (netInfo.Type == Android.Net.ConnectivityType.Mobile)
						{
							radio.State = RadioState.On;
						}
					}
#pragma warning restore CS0618 // Type or member is obsolete

				}
				return radio;
			}
		}


		private async static Task<IReadOnlyList<Radio>> GetRadiosAsyncTask()
		{
			var radios = new List<Radio>();

			var radio = GetRadiosBluetooth();
			if (radio != null) radios.Add(radio); // yield oRadio;

			radio = GetRadiosWiFiOrCellular(RadioKind.WiFi);
			if (radio != null) radios.Add(radio); // yield oRadio;

			radio = GetRadiosWiFiOrCellular(RadioKind.MobileBroadband);
			if (radio != null) radios.Add(radio); // yield oRadio;

			return radios;
		}

		/// <summary>
		/// Gets info about radio devices which exist on the system
		/// </summary>
		public static IAsyncOperation<IReadOnlyList<Radio>> GetRadiosAsync()
		=> GetRadiosAsyncTask().AsAsyncOperation();

	}

}
#endif
