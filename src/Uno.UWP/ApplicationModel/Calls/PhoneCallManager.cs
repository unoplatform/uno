#if __ANDROID__ || __IOS__ || __WASM__
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		internal PhoneCallManager()
		{
		}

		public static void ShowPhoneCallUI(string phoneNumber, string displayName)
		{
			if (phoneNumber == null)
			{
				throw new ArgumentNullException(nameof(phoneNumber));
			}

			if ( string.IsNullOrWhiteSpace(phoneNumber))
			{
				throw new ArgumentOutOfRangeException(nameof(phoneNumber), "Phone number must be provided");
			}

			if (displayName == null)
			{
				throw new ArgumentNullException(nameof(displayName));
			}

			ShowPhoneCallUIImpl(phoneNumber, displayName);
		}
	}
}
#endif
