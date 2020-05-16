using System.Threading.Tasks;
using Android.Content;
using Windows.Networking.Connectivity;

namespace Uno.Networking.Connectivity.Internal
{
	internal class ConnectivityChangeBroadcastReceiver : BroadcastReceiver
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
