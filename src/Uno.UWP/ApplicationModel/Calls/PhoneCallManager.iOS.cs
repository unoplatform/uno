#if __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CallKit;
using Foundation;
using UIKit;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static readonly CXCallObserver _callObserver = InitializeCXCallObserver();

		private static CXCallObserver InitializeCXCallObserver()
		{
			var callObserver = new CXCallObserver();
			callObserver.SetDelegate(new CallObserverDelegate(), null);
			return callObserver;
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
		{
			var url = new NSUrl($"tel:{phoneNumber}");
			UIApplication.SharedApplication.OpenUrl(url);
		}

	}
}
#endif
