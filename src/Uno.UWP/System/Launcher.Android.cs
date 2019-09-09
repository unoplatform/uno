#if __ANDROID__
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Windows.Foundation;

namespace Windows.System
{
	public partial class Launcher
	{
		public static async Task<bool> LaunchUriAsync(Uri uri)
		{
			if ( uri == null)
			{
				throw new ArgumentNullException(nameof(uri));
			}
			if (uri.Scheme.StartsWith("ms-", StringComparison.InvariantCultureIgnoreCase))
			{
				return await HandleSpecialUriAsync(uri);
			}
		}

		public static IAsyncOperation<LaunchQuerySupportStatus> QueryUriSupportAsync(
		Uri uri,
		LaunchQuerySupportType launchQuerySupportType)
		{
			var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(uri.OriginalString));

			if (Application.Context == null)
			{
				throw new InvalidOperationException(
					"Launcher was used too early in the application lifetime. " +
					"Android app context needs to be available.");
			}

			var manager = Application.Context.PackageManager;
			var supportedResolvedInfos = manager.QueryIntentActivities(
				intent,
				PackageInfoFlags.MatchDefaultOnly);
			if (supportedResolvedInfos.Any())
			{
				return Task.FromResult(LaunchQuerySupportStatus.Available)
					.AsAsyncOperation();
			}
			else
			{
				return Task.FromResult(LaunchQuerySupportStatus.NotSupported)
					.AsAsyncOperation();
			}
		}

		private static Task<bool> HandleSpecialUriAsync(Uri uri)
		{
			switch(uri.Scheme.ToLowerInvariant())
			{
				case "ms-settings": return HandleSettingsUriAsync(uri);
				default: return LaunchUriInternalAsync();
			}
		}

		private static Task<bool> HandleSettingsUriAsync(Uri uri)
		{
			string launchAction;
			switch (uri.AbsolutePath.ToLowerInvariant())
			{
				case "bluetooth":
					launchAction = Android.Provider.Settings.ActionBluetoothSettings;
					break;
				default:
					launchAction = Android.Provider.Settings.ActionSettings;
					break;
			}
			var intent = new Intent(launchAction);
			((Android.App.Activity)Uno.UI.ContextHelper.Current).StartActivity(intent);
		}
	}
}
#endif
