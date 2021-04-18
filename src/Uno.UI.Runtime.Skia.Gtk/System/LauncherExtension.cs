using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno.Extensions.System;
using Windows.System;

namespace Uno.UI.Runtime.Skia.GTK.Extensions.System
{
	internal class LauncherExtension : ILauncherExtension
	{
		public LauncherExtension(object owner)
		{
		}

		public Task<bool> LaunchUriAsync(Uri uri)
		{
			var processStartInfo = new ProcessStartInfo(uri.OriginalString)
			{
				UseShellExecute = true,
				Verb = "open"
			};

			var process = new Process()
			{
				StartInfo = processStartInfo
			};

			return Task.FromResult(process.Start());
		}

		public Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType)
		{
			return Task.Run(() =>
			{
				var canOpenUri = CheckXdgSettings(uri);
				return canOpenUri ?
					LaunchQuerySupportStatus.Available : LaunchQuerySupportStatus.NotSupported;
			});
		}

		private bool CheckXdgSettings(Uri uri)
		{
			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "xdg-settings",
					Arguments = $"get default-url-scheme-handler {uri.Scheme}",
					RedirectStandardOutput = true
				}
			};
			process.Start();
			var response = process.StandardOutput.ReadToEnd().Trim();
			return !string.IsNullOrEmpty(response);
		}
	}
}
