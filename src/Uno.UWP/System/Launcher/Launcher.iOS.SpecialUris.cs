#if __IOS__
using Foundation;
using UIKit;
using System;
using System.Threading.Tasks;

namespace Windows.System
{
	public static partial class Launcher
	{
		private const string MicrosoftSettingsUri = "ms-settings";

		private static bool CanHandleSpecialUri(Uri uri)
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case MicrosoftSettingsUri: return true;
				default: return false;
			}
		}

		private static bool HandleSpecialUri(Uri uri)
		{
			switch (uri.Scheme.ToLowerInvariant())
			{
				case MicrosoftSettingsUri: return HandleSettingsUri(uri);
				default: throw new InvalidOperationException("This special URI is not supported on iOS");
			}
		}

		private static bool HandleSettingsUri(Uri uri) =>
			UIApplication.SharedApplication.OpenUrl(
				new NSUrl(UIApplication.OpenSettingsUrlString));
	}
}
#endif
