#if __IOS__
using System;
using System.Threading.Tasks;
using UIKit;
using Windows.Foundation;
using AppleUrl = global::Foundation.NSUrl;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static async Task<bool> LaunchUriAsync(Uri uri)
		{
			if (uri == null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			if (IsSpecialUri(uri) && CanHandleSpecialUri(uri))
			{
				return await HandleSpecialUriAsync(uri);
			}

			return UIApplication.SharedApplication.OpenUrl(
				new AppleUrl(uri.OriginalString));
		}


		public static IAsyncOperation<LaunchQuerySupportStatus> QueryUriSupportAsync(
			Uri uri,
			LaunchQuerySupportType launchQuerySupportType)
		{
			if (uri == null)
			{
				throw new ArgumentNullException(nameof(uri));
			}
            
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

			return Task.FromResult(supportStatus).AsAsyncOperation();
		}
	}
}
#endif
