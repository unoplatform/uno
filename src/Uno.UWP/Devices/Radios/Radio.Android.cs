#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;


	namespace Windows.Devices.Radios
	{

		public partial class Radio
		{

			private static Radio GetRadiosBluetooth()
			{
				Android.Content.Context context = Android.App.Application.Context;
				bool btFull = context.PackageManager.HasSystemFeature(Android.Content.PM.PackageManager.FeatureBluetooth);
				bool btLE = context.PackageManager.HasSystemFeature(Android.Content.PM.PackageManager.FeatureBluetoothLe);
				if (!(btFull || btLE))
				{
					return null;
				}

				var oRadio = new Radio();
				oRadio.Kind = RadioKind.Bluetooth;
				oRadio.Name = "Bluetooth";  // name as in UWP

				if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.Bluetooth))
				{
					oRadio.State = RadioState.Unknown;
					return oRadio;
				}

				if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.JellyBeanMr1)
				{
					var btAdapter = Android.Bluetooth.BluetoothAdapter.DefaultAdapter;
					if (btAdapter == null)
					{
						return null;    // shouldn't happen...
					}
					else if (!btAdapter.IsEnabled)
					{
						oRadio.State = RadioState.Off;
					}
					else
					{
						oRadio.State = RadioState.On;
					}

					return oRadio;
				}
				else
				{
					var btAdapter = ((Android.Bluetooth.BluetoothManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.BluetoothService)).Adapter;
					if (btAdapter == null)
					{
						return null;    // shouldn't happen...
					}
					if (btAdapter.State != Android.Bluetooth.State.On)
					{
						oRadio.State = RadioState.Off;
					}
					else
					{
						oRadio.State = RadioState.On;
					}

					return oRadio;
				}

			}

			private static Radio GetRadiosWiFi()
			{
				Android.Content.Context context = Android.App.Application.Context;
				if (!context.PackageManager.HasSystemFeature(Android.Content.PM.PackageManager.FeatureWifi))
				{
					return null;
				}

				var oRadio = new Radio();
				oRadio.Kind = RadioKind.WiFi;
				oRadio.Name = "Wi-Fi";  // name as in UWP

				if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.AccessNetworkState))
				{
					oRadio.State = RadioState.Unknown;
					return oRadio;
				}

				var connManager = (Android.Net.ConnectivityManager)context.GetSystemService(Android.Content.Context.ConnectivityService);

				if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.LollipopMr1)
				{
					// since API 23
					var activeNetwork = connManager.ActiveNetwork;
					if (activeNetwork is null)
					{
						return null;
					}
					var netCaps = connManager.GetNetworkCapabilities(activeNetwork);
					if (netCaps is null)
					{
						return null;
					}
					if (netCaps.HasTransport(Android.Net.TransportType.Wifi))
					{
						oRadio.State = RadioState.On;
					}
					else
					{
						oRadio.State = RadioState.Off;
					}

					return oRadio;
				}
				else
				{
				// for Android API 1 to 28 (deprecated in 29)
#pragma warning disable CS0618 // Type or member is obsolete
				var netInfo = connManager.ActiveNetworkInfo;
					if (netInfo is null)
					{
						oRadio.State = RadioState.Off;
					}
					else
					{
						if (netInfo.Type == Android.Net.ConnectivityType.Wifi ||
							(netInfo.Type == Android.Net.ConnectivityType.Wimax))
#pragma warning restore CS0618 // Type or member is obsolete
						{
							oRadio.State = RadioState.On;
						}
						else
						{
							oRadio.State = RadioState.Off;
						}
					}
					return oRadio;
				}


			}

			private static Radio GetRadiosMobile()
			{
				Android.Content.Context context = Android.App.Application.Context;
				if (!context.PackageManager.HasSystemFeature(Android.Content.PM.PackageManager.FeatureTelephony))
				{
					return null;
				}

				var oRadio = new Radio();
				oRadio.Kind = RadioKind.MobileBroadband;
				oRadio.Name = "Cellular";  // I have "Cellular 6" (Lumia 532), but maybe "6" doesn't mean anything

				if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.AccessNetworkState))
				{
					oRadio.State = RadioState.Unknown;
					return oRadio;
				}

				var connManager = (Android.Net.ConnectivityManager)context.GetSystemService(Android.Content.Context.ConnectivityService);

				if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.LollipopMr1)
				{
					// available since API 23
					var activeNetwork = connManager.ActiveNetwork;
					if (activeNetwork is null)
					{
						return null;
					}
					var netCaps = connManager.GetNetworkCapabilities(activeNetwork);
					if (netCaps is null)
					{
						return null;
					}
					if (netCaps.HasTransport(Android.Net.TransportType.Cellular))
					{
						oRadio.State = RadioState.On;
					}
					else
					{
						oRadio.State = RadioState.Off;
					}

					return oRadio;
				}
				else
				{
				// for Android API 1 to 28 (deprecated in 29)
#pragma warning disable CS0618 // Type or member is obsolete
				var netInfo = connManager.ActiveNetworkInfo;
					if (netInfo is null)
					{
						oRadio.State = RadioState.Off;
					}
					else
					{
						if (netInfo.Type == Android.Net.ConnectivityType.Mobile)
#pragma warning restore CS0618 // Type or member is obsolete
						{
							oRadio.State = RadioState.On;
						}
						else
						{
							oRadio.State = RadioState.Off;
						}
					}
					return oRadio;
				}


			}


			private async static Task<IReadOnlyList<Radio>> GetRadiosAsyncTask()
			{// this method can be not Async/await, but I don't know how to convert it to IAsyncOperation

				var oRadios = new List<Radio>();

				var oRadio = GetRadiosBluetooth();
				if (oRadio != null) oRadios.Add(oRadio); // yield oRadio;

				oRadio = GetRadiosWiFi();
				if (oRadio != null) oRadios.Add(oRadio); // yield oRadio;

				oRadio = GetRadiosMobile();
				if (oRadio != null) oRadios.Add(oRadio); // yield oRadio;

				return oRadios;
			}

			/// <summary>
			/// Gets info about the phones for a contact.
			/// </summary>
			public static IAsyncOperation<IReadOnlyList<Radio>> GetRadiosAsync()
			{
				return GetRadiosAsyncTask().AsAsyncOperation();
			}

	}

}
#endif
