#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Provider;

namespace Windows.Storage;

public static partial class CachedFileManager
{
	private static Task<FileUpdateStatus> CompleteUpdatesTaskAsync(IStorageFile file, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		return (file as StorageFile)?.ExportAsync(token) ?? Task.FromResult(FileUpdateStatus.Complete);
	}
}
