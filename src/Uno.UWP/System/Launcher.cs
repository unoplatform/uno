using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Logging;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Windows.UI.Core;

namespace Windows.System
{
	public static partial class Launcher
	{
		private const string MicrosoftUriPrefix = "ms-";

		private static bool IsSpecialUri(Uri uri) => uri.Scheme.StartsWith(MicrosoftUriPrefix, StringComparison.InvariantCultureIgnoreCase);


		public static Task<bool> LaunchUriAsync(Uri uri)
		{
#if __IOS__ || __ANDROID__ || __WASM__

			if (uri == null)
			{
				// this exception might not be in line with UWP which seems to throw AccessViolationException... for some reason.
				throw new ArgumentNullException(nameof(uri));
			}

#if !__WASM__
			if (!CoreDispatcher.Main.HasThreadAccess)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().Error($"{nameof(LaunchUriAsync)} must be called on the UI thread");
				}
				// LaunchUriAsync throws the following exception if used on UI thread on UWP
				throw new InvalidOperationException($"{nameof(LaunchUriAsync)} must be called on the UI thread");
			}
#endif

			return LaunchUriPlatformAsync(uri);
#else
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Launcher).Log().Error($"{nameof(LaunchUriAsync)} is not implemented on this platform.");
			}

			return Task.FromResult(false);
#endif
		}

#if __ANDROID__ || __IOS__
		public static IAsyncOperation<LaunchQuerySupportStatus> QueryUriSupportAsync(
			Uri uri,
			LaunchQuerySupportType launchQuerySupportType)
		{
			if (uri == null)
			{
				// counterintuitively, UWP throws plain Exception with HRESULT when passed in Uri is null
				throw new ArgumentNullException(nameof(uri));
			}

			// this method may run on the background thread on UWP
#if !__WASM__
			if (CoreDispatcher.Main.HasThreadAccess)
#else
			if(true)
#endif
			{
				return QueryUriSupportPlatformAsync(uri, launchQuerySupportType).AsAsyncOperation();
			}
			else
			{
				return CoreDispatcher.Main.RunWithResultAsync(
					priority: CoreDispatcherPriority.Normal,
					task: async () => await QueryUriSupportPlatformAsync(uri, launchQuerySupportType)
				).AsAsyncOperation();
			}
		}
#endif
	}
}
