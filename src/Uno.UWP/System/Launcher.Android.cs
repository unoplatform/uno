#if __ANDROID__
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static async Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			try
			{
				if (Uno.UI.ContextHelper.Current == null)
				{
					throw new InvalidOperationException(
						"LaunchUriAsync was called too early in application lifetime. " +
						"App context needs to be initialized");
				}

				if (IsSpecialUri(uri) && CanHandleSpecialUri(uri))
				{
					return await HandleSpecialUriAsync(uri);
				}

				return await LaunchUriActivityAsync(uri);
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

		public static Task<LaunchQuerySupportStatus> QueryUriSupportPlatformAsync(
			Uri uri,
			LaunchQuerySupportType launchQuerySupportType)
		{
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

			return Task.FromResult(supportStatus);
		}

		private static Task<bool> LaunchUriActivityAsync(Uri uri)
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
