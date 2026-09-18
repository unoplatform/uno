using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CallKit;
using Foundation;
using UIKit;
using Windows.System;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static readonly CXCallObserver _callObserver;

		static PhoneCallManager()
		{
			_callObserver = new CXCallObserver();
			_callObserver.SetDelegate(new CallObserverDelegate(), null);
		}


		public static event EventHandler<object> CallStateChanged;

		public static bool IsCallActive =>
			_callObserver.Calls?.Any(
				c => c != null &&
					 c.HasConnected &&
					 !c.HasEnded) == true;

		public static bool IsCallIncoming =>
			_callObserver.Calls?.Any(
					c => c != null &&
						 !c.HasEnded &&
						 !c.HasConnected &&
						 !c.Outgoing) == true;

		internal static void RaiseCallStateChanged() => CallStateChanged?.Invoke(null, null);

		private static void ShowPhoneCallUIImpl(string phoneNumber, string displayName)
			=> Task.Run(() => Launcher.LaunchUriAsync(new Uri($"tel:{phoneNumber}")));

	}
}
