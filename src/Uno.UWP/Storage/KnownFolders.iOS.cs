using System.Threading;
using System.Threading.Tasks;

namespace Windows.Storage;

partial class KnownFolders
{
	private static Task<KnownFoldersAccessStatus> RequestAccessPlatformAsync(KnownFolderId knownFolderId, CancellationToken ct)
	{
		return Task.FromResult(KnownFoldersAccessStatus.Allowed);
	}

	private static Task<StorageFolder> GetFolderPlatformAsync(KnownFolderId knownFolderId, CancellationToken ct)
	{
		return Task.FromResult<StorageFolder>(null);
	}
}
