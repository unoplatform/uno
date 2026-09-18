using System;
using System.Threading.Tasks;
using Uno.Foundation;

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
	}
}
