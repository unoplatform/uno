using System;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Storage;

using NativeMethods = __Windows.__System.Launcher.NativeMethods;

namespace Windows.System
{
	public static partial class Launcher
	{
		internal static Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			var result = NativeMethods.Open(uri.OriginalString);
			return Task.FromResult(result == "True");
		}

		internal static Task<bool> LaunchFilePlatformAsync(IStorageFile file)
		{
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(Launcher).Log().Warn($"{nameof(LaunchFileAsync)} is not supported on WebAssembly.");
			}

			return Task.FromResult(false);
		}
	}
}
