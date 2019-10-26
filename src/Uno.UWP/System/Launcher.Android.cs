
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;
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
			try
			{
				if (uri == null)
				{
					throw new ArgumentNullException(nameof(uri));
				}

				if (Uno.UI.ContextHelper.Current == null)
				{
					throw new InvalidOperationException(
						"LaunchUriAsync was called too early in application lifetime. " +
						"App context needs to be initialized");
				}

				if (IsSpecialUri(uri) && CanHandleInternalUri(uri))
				{
					return await HandleSpecialUriAsync(uri);
				}

				return await LaunchUriInternalAsync(uri);
			}
			catch (Exception exception)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().Error($"Failed to {nameof(LaunchUriAsync)}.", exception);
				}

				return false;
			}
		}

		public static IAsyncOperation<LaunchQuerySupportStatus> QueryUriSupportAsync(
			Uri uri,
			LaunchQuerySupportType launchQuerySupportType)
		{
			if (uri == null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			if (Uno.UI.ContextHelper.Current == null)
			{
				throw new InvalidOperationException(
					"LaunchUriAsync was called too early in application lifetime. " +
					"App context needs to be initialized");
			}

			bool canOpenUri;
			if (!IsSpecialUri(uri))
			{
				var androidUri = Android.Net.Uri.Parse(uri.OriginalString);
				var intent = new Intent(Intent.ActionView, androidUri);

				var manager = Uno.UI.ContextHelper.Current.PackageManager;
				var supportedResolvedInfos = manager.QueryIntentActivities(
						intent,
						PackageInfoFlags.MatchDefaultOnly);
				canOpenUri = supportedResolvedInfos.Any();
			}
			else
			{
				canOpenUri = CanHandleSpecialUri(uri);
			}

			var supportStatus = canOpenUri ?
				LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;

			return Task.FromResult(supportStatus).AsAsyncOperation();
		}

		private static Task<bool> LaunchUriInternalAsync(Uri uri)
		{
			var androidUri = Android.Net.Uri.Parse(uri.OriginalString);
			var intent = new Intent(Intent.ActionView, androidUri);

			StartActivity(intent);
			return Task.FromResult(true);
		}

		private static void StartActivity(Intent intent) => ((Activity)Uno.UI.ContextHelper.Current).StartActivity(intent);
	}
}
#endif
