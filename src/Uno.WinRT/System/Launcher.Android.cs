using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.Content.PM;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static Task<bool> LaunchUriPlatformAsync(Uri uri)
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
					return Task.FromResult(HandleSpecialUri(uri));
				}

				return Task.FromResult(LaunchUriActivityAsync(uri));
			}
			catch (Exception exception)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().Error($"Failed to {nameof(LaunchUriAsync)}.", exception);
				}

				return Task.FromResult(false);
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
				var activity = intent.ResolveActivity(manager);

				IList<ResolveInfo> supportedResolvedInfos = null;
				// Use new overload of QueryIntentActivities on API 33 and newer
				// to avoid obsolete warning
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Tiramisu)
				{
					supportedResolvedInfos = manager.QueryIntentActivities(
						intent,
						PackageManager.ResolveInfoFlags.Of((long)PackageInfoFlags.MatchDefaultOnly));
				}
				else
				{
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CA1422 // Validate platform compatibility
					supportedResolvedInfos = manager.QueryIntentActivities(
						intent,
						PackageInfoFlags.MatchDefaultOnly);
#pragma warning restore CA1422 // Validate platform compatibility
#pragma warning restore CS0618 // Type or member is obsolete
				}

				canOpenUri = supportedResolvedInfos?.Any() == true;
			}
			else
			{
				canOpenUri = CanHandleSpecialUri(uri);
			}

			var supportStatus = canOpenUri ?
				LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;

			return Task.FromResult(supportStatus);
		}

		private static bool LaunchUriActivityAsync(Uri uri)
		{
			var androidUri = Android.Net.Uri.Parse(uri.OriginalString);
			var intent = new Intent(Intent.ActionView, androidUri);

			StartActivity(intent);
			return true;
		}

		private static void StartActivity(Intent intent) => ((Activity)Uno.UI.ContextHelper.Current).StartActivity(intent);
	}
}
