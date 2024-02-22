using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.Extensions;

using Windows.UI.Core;

namespace Windows.System
{
	public static partial class Launcher
	{
#if __ANDROID__ || __IOS__ || __MACOS__ || __SKIA__
		private const string MicrosoftUriPrefix = "ms-";

		internal static bool IsSpecialUri(Uri uri) => uri.Scheme.StartsWith(MicrosoftUriPrefix, StringComparison.InvariantCultureIgnoreCase);
#endif

		public static IAsyncOperation<bool> LaunchUriAsync(Uri uri)
		{
#if __IOS__ || __ANDROID__ || __WASM__ || __MACOS__ || __SKIA__

			if (uri == null)
			{
				// this exception might not be in line with UWP which seems to throw AccessViolationException... for some reason.
				throw new ArgumentNullException(nameof(uri));
			}

#if !__WASM__
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				return LaunchUriPlatformAsync(uri).AsAsyncOperation();
			}
			else
			{
				return CoreDispatcher.Main.RunWithResultAsync(
					priority: CoreDispatcherPriority.Normal,
					task: async () => await LaunchUriPlatformAsync(uri)
				).AsAsyncOperation();
			}
#else
			return LaunchUriPlatformAsync(uri).AsAsyncOperation();
#endif

#else
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Launcher).Log().Error($"{nameof(LaunchUriAsync)} is not implemented on this platform.");
			}

			return Task.FromResult(false).AsAsyncOperation();
#endif
		}

#if __ANDROID__ || __IOS__ || __MACOS__ || __SKIA__
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
			if (CoreDispatcher.Main.HasThreadAccess)
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
