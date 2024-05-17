using Windows.Foundation;

namespace Windows.Storage;

/// <summary>
/// Provides access to common locations that contain user content. 
/// This includes content from a user's local libraries (such as Documents, 
/// Pictures, Music, and Videos), HomeGroup, removable devices, and media server devices.
/// </summary>
public static partial class KnownFolders
{
	public static IAsyncOperation<KnownFoldersAccessStatus> RequestAccessAsync(KnownFolderId folderId) => AsyncOperation.FromTask(ct => RequestAccessPlatformAsync(folderId, ct));

	public static IAsyncOperation<StorageFolder> GetFolderAsync(KnownFolderId folderId) => AsyncOperation.FromTask(ct => GetFolderPlatformAsync(folderId, ct));
}
