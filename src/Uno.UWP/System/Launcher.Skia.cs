using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			var processStartInfo = new ProcessStartInfo(uri.OriginalString)
			{
				UseShellExecute = true,
				Verb = "open"
			};

			var process = new Process();
			process.StartInfo = processStartInfo;
			return Task.FromResult(process.Start());
		}
	}
}
