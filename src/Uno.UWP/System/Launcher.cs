using System;

namespace Windows.System
{
	public static partial class Launcher
	{
		private const string MicrosoftUriPrefix = "ms-";

		private static bool IsSpecialUri(Uri uri) => uri.Scheme.StartsWith(MicrosoftUriPrefix, StringComparison.InvariantCultureIgnoreCase);

#if !__IOS__ && !__ANDROID__ && !__WASM__
		public static async Task<bool> LaunchUriAsync(Uri uri)
		{
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Launcher).Log().Error($"{nameof(LaunchUriAsync)} is not implemented on this platform.");
			}

			return false;
		}
#endif
	}
}
