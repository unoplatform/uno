using System;
using System.Threading.Tasks;
using AppKit;
using Windows.Foundation;
using Windows.Storage;

namespace Windows.System
{
	public static partial class Launcher
	{
		public static IAsyncOperation<bool> LaunchFolderPathAsync(string path)
		{
			if (path is null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			var success = NSWorkspace.SharedWorkspace.OpenFile(path);
			return Task.FromResult(success).AsAsyncOperation();
		}

		public static IAsyncOperation<bool> LaunchFileAsync(IStorageFile file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			var success = NSWorkspace.SharedWorkspace.OpenFile(file.Path);
			return Task.FromResult(success).AsAsyncOperation();
		}

		public static IAsyncOperation<bool> LaunchFolderAsync(IStorageFolder folder)
		{
			if (folder is null)
			{
				throw new ArgumentNullException(nameof(folder));
			}

			return Launcher.LaunchFolderPathAsync(folder.Path);
		}
	}
}
