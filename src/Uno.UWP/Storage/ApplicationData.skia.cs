using System;
using System.IO;

namespace Windows.Storage
{
	partial class ApplicationData 
	{
		private static string GetLocalCacheFolder()
			=> Path.GetTempPath();

		private static string GetTemporaryFolder()
			=> Path.GetTempPath();

		private static string GetLocalFolder()
			// Uses XDG_DATA_HOME on Unix: https://github.com/dotnet/runtime/blob/b5705587347d29d79cec830dc22b389e1ad9a9e0/src/libraries/System.Private.CoreLib/src/System/Environment.GetFolderPathCore.Unix.cs#L105
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		private static string GetRoamingFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

		private static string GetSharedLocalFolder()
			=> Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
	}
}
