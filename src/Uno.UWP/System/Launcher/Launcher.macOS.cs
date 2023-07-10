using System;
using System.Threading.Tasks;
using AppKit;
using Windows.Foundation;
using Windows.Storage;

namespace Windows.System;

public static partial class Launcher
{
	private static Task<bool> LaunchFolderPathPlatformAsync(string path)
	{
		var success = NSWorkspace.SharedWorkspace.OpenFile(path);
		return Task.FromResult(success);
	}

	private static Task<bool> LaunchFilePlatformAsync(IStorageFile file)
	{
		var success = NSWorkspace.SharedWorkspace.OpenFile(file.Path);
		return Task.FromResult(success);
	}

	private static Task<bool> LaunchFolderPlatformAsync(IStorageFolder folder) =>
		LaunchFolderPathPlatformAsync(folder.Path);
}
