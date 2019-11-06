#if __IOS__
using System;
using System.Threading.Tasks;
using UIKit;
using AppleUrl = global::Foundation.NSUrl;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static async Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			if (IsSpecialUri(uri) && CanHandleSpecialUri(uri))
			{
				return await HandleSpecialUriAsync(uri);
			}

			return UIApplication.SharedApplication.OpenUrl(
				new AppleUrl(uri.OriginalString));
		}


		public static Task<LaunchQuerySupportStatus> QueryUriSupportPlatformAsync(
			Uri uri,
			LaunchQuerySupportType launchQuerySupportType)
		{            
			bool canOpenUri;
			if (!IsSpecialUri(uri))
			{
				canOpenUri = UIApplication.SharedApplication.CanOpenUrl(
					new AppleUrl(uri.OriginalString));
			}
			else
			{
				canOpenUri = CanHandleSpecialUri(uri);
			}

			var supportStatus = canOpenUri ?
				LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;

			return Task.FromResult(supportStatus);
		}
	}
}
#endif
