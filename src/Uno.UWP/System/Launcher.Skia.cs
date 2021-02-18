#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;

namespace Windows.System
{
	public static partial class Launcher
	{
		private static readonly Lazy<ILauncherExtension?> _launcherExtension = new Lazy<ILauncherExtension?>(() =>
		{
			if (ApiExtensibility.CreateInstance<ILauncherExtension>(typeof(Launcher), out var launcherExtension))
			{
				return launcherExtension;
			}
			return null;
		});

		public static async Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			if (_launcherExtension.Value != null)
			{
				return await _launcherExtension.Value.LaunchUriAsync(uri);
			}
			return await LaunchUriFallbackAsync(uri);
		}

		private static Task<bool> LaunchUriFallbackAsync(Uri uri)
		{
			try
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
			catch (Exception ex)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().LogError($"Could not launch URI - {ex}");
				}
				return Task.FromResult(false);
			}
		}
	}
}
