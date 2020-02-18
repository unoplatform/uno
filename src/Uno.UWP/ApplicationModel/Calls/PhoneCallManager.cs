#if __ANDROID__ || __IOS__ || __WASM__ || __MACOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.ApplicationModel.Calls
{
	public partial class PhoneCallManager
	{
		private static readonly char[] _allowedChars = new char[]
		{
			'+', ';', '=', '?', '#', '-'
		};

		internal PhoneCallManager()
		{
		}

		public static void ShowPhoneCallUI(string phoneNumber, string displayName)
		{
			if (phoneNumber == null)
			{
				throw new ArgumentNullException(nameof(phoneNumber));
			}

			if (string.IsNullOrWhiteSpace(phoneNumber))
			{
				throw new ArgumentOutOfRangeException(nameof(phoneNumber), "Phone number must be provided");
			}

			var disallowed = phoneNumber
				.Where(c =>
					!char.IsDigit(c) &&
					!_allowedChars.Contains(c))
				.Select(c=>(char?)c)
				.FirstOrDefault();

			if (disallowed != null)
			{
				throw new ArgumentOutOfRangeException(nameof(phoneNumber), $"Phone number contains disallowed character {disallowed}");
			}

			if (displayName == null)
			{
				throw new ArgumentNullException(nameof(displayName));
			}

			ShowPhoneCallUIImpl(phoneNumber.Trim(), displayName);
		}
	}
}
#endif
