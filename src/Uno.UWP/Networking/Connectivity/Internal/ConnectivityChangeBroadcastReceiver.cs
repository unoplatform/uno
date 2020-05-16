using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;

namespace Windows.Networking.Connectivity.Internal
{
	class ConnectivityChangeBroadcastReceiver : BroadcastReceiver
	{
		public override async void OnReceive(Context context, Intent intent)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			if (intent.Action != Android.Net.ConnectivityManager.ConnectivityAction)
#pragma warning restore CS0618 // Type or member is obsolete
			{
				return;
			}

			// await 300ms to ensure that the the connection manager updates
			await Task.Delay(300);
			NetworkInformation.OnNetworkStatusChanged();
		}
	}
}
