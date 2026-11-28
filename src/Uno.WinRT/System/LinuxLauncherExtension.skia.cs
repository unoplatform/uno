using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Runtime.Extensions.System.LauncherHelpers;
using Windows.Storage;
using Windows.System;
using Uno.Extensions.System;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Extensions.System
{
	internal class LinuxLauncherExtension : ILauncherExtension
	{
		private static readonly TimeSpan XdgOpenExitTimeout = TimeSpan.FromSeconds(5);

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

		public async Task<bool> LaunchFileAsync(IStorageFile file)
		{
			try
			{
				using var process = new Process
				{
					StartInfo = new ProcessStartInfo("xdg-open")
					{
						ArgumentList = { file.Path },
						UseShellExecute = false,
					}
				};

				process.Start();

				// xdg-open reports a missing file or handler through its exit code, but some
				// desktop environments keep it alive until the opened app quits.
				using var timeout = new CancellationTokenSource(XdgOpenExitTimeout);
				try
				{
					await process.WaitForExitAsync(timeout.Token);
				}
				catch (OperationCanceledException)
				{
					return true;
				}

				if (process.ExitCode != 0 && this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"xdg-open could not open '{file.Path}' (exit code {process.ExitCode}).");
				}

				return process.ExitCode == 0;
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Could not launch file '{file.Path}'.", ex);
				}

				return false;
			}
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
