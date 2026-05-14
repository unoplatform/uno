#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Uno.Extensions;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Storage;

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

		internal static async Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			if (_launcherExtension.Value != null)
			{
				return await _launcherExtension.Value.LaunchUriAsync(uri);
			}
			return await LaunchUriFallbackAsync(uri);
		}

		internal static async Task<bool> LaunchFilePlatformAsync(IStorageFile file)
		{
			if (_launcherExtension.Value is ILauncherExtension ext)
			{
				return await ext.LaunchFileAsync(file);
			}
			return LaunchFileFallback(file);
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

		private static bool LaunchFileFallback(IStorageFile file)
		{
			try
			{
				var processStartInfo = new ProcessStartInfo(file.Path)
				{
					UseShellExecute = true,
				};

				var process = new Process();
				process.StartInfo = processStartInfo;
				return process.Start();
			}
			catch (Exception ex)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().LogError($"Could not launch file - {ex}");
				}
				return false;
			}
		}

		internal static async Task<LaunchQuerySupportStatus> QueryUriSupportPlatformAsync(
			Uri uri,
			LaunchQuerySupportType launchQuerySupportType)
		{
			if (_launcherExtension.Value != null)
			{
				return await _launcherExtension.Value.QueryUriSupportAsync(uri, launchQuerySupportType);
			}
			throw new NotImplementedException("QueryUriSupportAsync is not implemented on this platform");
		}
	}
}
