using System.Threading;
using System.Threading.Tasks;

namespace Windows.Storage;

partial class KnownFolders
{
	internal static StorageFolder FolderFromAndroidName(string name)
#pragma warning disable CS0618 // Type or member is obsolete
		=> new StorageFolder(Android.OS.Environment.GetExternalStoragePublicDirectory(name).CanonicalPath);
#pragma warning restore CS0618 // Type or member is obsolete

	public static StorageFolder MusicLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMusic);
	public static StorageFolder VideosLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMovies);

	private static async Task<KnownFoldersAccessStatus> RequestAccessPlatformAsync(KnownFolderId knownFolderId, CancellationToken ct)
	{
		if (knownFolderId == KnownFolderId.DocumentsLibrary)
		{

		}
		else
		{

		}
	}

	private static async Task<StorageFolder> GetFolderPlatformAsync(KnownFolderId knownFolderId, CancellationToken ct)
	{
		if (knownFolderId == KnownFolderId.DocumentsLibrary)
		{
			return new StorageFolder(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).CanonicalPath);
		}
		else
		{
			return null;
		}
	}
}
