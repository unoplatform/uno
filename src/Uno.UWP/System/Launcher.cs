#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using System.Linq;
#if __ANDROID__
using Android.Content;
using Android.Content.PM;
#endif

namespace Windows.System
{
	public static partial class Launcher
	{
		public static async Task<bool> LaunchUriAsync(Uri uri)
		{
			try
			{
#if __IOS__
				return UIKit.UIApplication.SharedApplication.OpenUrl(new global::Foundation.NSUrl(uri.OriginalString));
#elif __ANDROID__
				var androidUri = Android.Net.Uri.Parse(uri.OriginalString);
				var intent = new Intent(Intent.ActionView, androidUri);

				if ( Uno.UI.ContextHelper.Current == null)
				{
					throw new InvalidOperationException(
						"LaunchUriAsync was called too early in application lifetime. " +
						"App context needs to be initialized");
				}
				((Android.App.Activity)Uno.UI.ContextHelper.Current).StartActivity(intent);

				return true;
#elif __WASM__
				var command = $"Uno.UI.WindowManager.current.open(\"{uri.OriginalString}\");";
				var result = Uno.Foundation.WebAssemblyRuntime.InvokeJS(command);
				return result == "True";
#else
				throw new NotImplementedException();
#endif
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
			try
			{
#if __IOS__
				
#elif __ANDROID__
				var androidUri = Android.Net.Uri.Parse(uri.OriginalString);
				var intent = new Intent(Intent.ActionView, androidUri);

				if (Uno.UI.ContextHelper.Current == null)
				{
					throw new InvalidOperationException(
						"LaunchUriAsync was called too early in application lifetime. " +
						"App context needs to be initialized");
				}

				var manager = Uno.UI.ContextHelper.Current.PackageManager;
				var supportedResolvedInfos = manager.QueryIntentActivities(
					intent,
					PackageInfoFlags.MatchDefaultOnly);
				var result = supportedResolvedInfos.Any() ?
					LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;
				return Task.FromResult(result).AsAsyncOperation();
#else
				throw new NotImplementedException();
#endif
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
	}
}
