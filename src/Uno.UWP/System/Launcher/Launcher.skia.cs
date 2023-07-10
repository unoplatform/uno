#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Windows.System;

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

		return TryStartProcessForPath(uri.OriginalString);
	}

	private static async Task<bool> LaunchFolderPathPlatformAsync(string path)
	{
		if (_launcherExtension.Value != null)
		{
			return await _launcherExtension.Value.LaunchFolderAsync(path);
		}

		return false;
	}

	private static Task<bool> LaunchFolderPlatformAsync(IStorageFolder folder) =>
		LaunchFolderPathPlatformAsync(folder.Path);

	private static async Task<bool> LaunchFilePlatformAsync(IStorageFile file)
	{
		if (_launcherExtension.Value != null)
		{
			return await _launcherExtension.Value.LaunchFolderAsync(file.Path);
		}

		return false;
	}

	internal static bool TryStartProcessForPath(string path)
	{
		try
		{
			var processStartInfo = new ProcessStartInfo(path)
			{
				UseShellExecute = true,
				Verb = "open"
			};

			var process = new Process
			{
				StartInfo = processStartInfo
			};

			return process.Start();
		}
		catch (Exception ex)
		{
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Launcher).Log().LogError($"Could not launch path or URI - {ex}");
			}

			return false;
		}
	}

	public static async Task<LaunchQuerySupportStatus> QueryUriSupportPlatformAsync(
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
