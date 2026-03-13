#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Uno.Extensions;
using Uno.Extensions.System;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;

using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Core;

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

		public static IAsyncOperation<bool> LaunchFileAsync(IStorageFile file)
		{
			if (file == null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			return LaunchFileInternalAsync(file.Path).AsAsyncOperation();
		}

		public static IAsyncOperation<bool> LaunchFileAsync(IStorageFile file, LauncherOptions options)
		{
			return LaunchFileAsync(file);
		}

		public static IAsyncOperation<bool> LaunchFolderPathAsync(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentNullException(nameof(path));
			}

			return LaunchFolderPathInternalAsync(path).AsAsyncOperation();
		}

		public static IAsyncOperation<bool> LaunchFolderPathAsync(string path, FolderLauncherOptions options)
		{
			// options are currently not used, but we provide the overload for API compatibility
			return LaunchFolderPathAsync(path);
		}

		public static IAsyncOperation<bool> LaunchFolderAsync(IStorageFolder folder)
		{
			if (folder == null)
			{
				throw new ArgumentNullException(nameof(folder));
			}

			return LaunchFolderPathAsync(folder.Path);
		}

		public static IAsyncOperation<bool> LaunchFolderAsync(IStorageFolder folder, FolderLauncherOptions options)
		{
			return LaunchFolderAsync(folder);
		}

		private static async Task<bool> LaunchFileInternalAsync(string filePath)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					if (typeof(Launcher).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(Launcher).Log().LogWarning($"File does not exist: {filePath}");
					}
					return false;
				}

				var fileUri = new Uri(filePath);
				return await LaunchUriInternalAsync(fileUri);
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

		private static async Task<bool> LaunchFolderPathInternalAsync(string folderPath)
		{
			try
			{
				if (!Directory.Exists(folderPath))
				{
					if (typeof(Launcher).Log().IsEnabled(LogLevel.Warning))
					{
						typeof(Launcher).Log().LogWarning($"Folder does not exist: {folderPath}");
					}
					return false;
				}

				var folderUri = new Uri(folderPath);
				return await LaunchUriInternalAsync(folderUri);
			}
			catch (Exception ex)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().LogError($"Could not launch folder - {ex}");
				}
				return false;
			}
		}

		private static async Task<bool> LaunchUriInternalAsync(Uri uri)
		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				return await LaunchUriPlatformAsync(uri);
			}
			else
			{
				return await CoreDispatcher.Main.RunWithResultAsync(
					priority: CoreDispatcherPriority.Normal,
					task: async () => await LaunchUriPlatformAsync(uri)
				);
			}
		}

		internal static async Task<bool> LaunchUriPlatformAsync(Uri uri)
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
