#nullable enable

using System;
using System.Threading.Tasks;
using Windows.System;

namespace Uno.Extensions.System;

internal interface ILauncherExtension
{
	Task<bool> LaunchUriAsync(Uri uri);

	Task<bool> LaunchFileAsync(string storageFilePath);

	Task<bool> LaunchFolderAsync(string storageFolderPath);

	Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType);
}
