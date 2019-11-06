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


		public static IAsyncOperation<bool> LaunchUriAsync(Uri uri)
		{
#if __IOS__ || __ANDROID__ || __WASM__

			if (uri == null)
			{
				// counterintuitively, UWP throws AccessViolationException when passed in Uri is null
				throw new AccessViolationException("Attempted to read or write protected memory. This is often an indication that other memory is corrupt.");
			}

			if (!CoreDispatcher.Main.HasThreadAccess)
			{
				// LaunchUriAsync throws the following exception if used on UI thread on UWP
				throw new InvalidOperationException("A method was called at an unexpected time.");
			}

			return LaunchUriPlatformAsync(uri).AsAsyncOperation();
#else
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Launcher).Log().Error($"{nameof(LaunchUriAsync)} is not implemented on this platform.");
			}

			return Task.FromResult(false).AsAsyncOperation();
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
				throw new Exception("The remote procedure call failed. (Exception from HRESULT: 0x800706BE)");
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
