using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia.Extensions.System.LauncherHelpers;
using Windows.System;
using Uno.Extensions.System;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Extensions.System
{
	internal class LinuxLauncherExtension : ILauncherExtension
	{
		public LinuxLauncherExtension(object owner)
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
				bool? canOpenUri = null;

				try
				{
					// Easiest way:
					canOpenUri = CheckXdgSettings(uri);
				}
				catch (Exception exception)
				{
					// Failure here does not affect the the query.
					if (typeof(LinuxLauncherExtension).Log().IsEnabled(LogLevel.Error))
					{
						typeof(LinuxLauncherExtension).Log().Error($"Failed to invoke xdg-settings", exception);
					}
				}

				// Should work with most Linux environments
				canOpenUri ??= CheckMimeTypeAssociations(uri);

				return canOpenUri.Value ?
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

		private bool CheckMimeTypeAssociations(Uri uri)
		{
			var list = new MimeAppsList();
			var mimeType = $"x-scheme-handler/{uri.Scheme}";
			return list.Supports(mimeType);
		}
	}
}
