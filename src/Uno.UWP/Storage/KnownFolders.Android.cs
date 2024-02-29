namespace Windows.Storage
{
	public static partial class KnownFolders
	{
		internal static StorageFolder FolderFromAndroidName(string name)
#pragma warning disable CS0618 // Type or member is obsolete
			=> new StorageFolder(Android.OS.Environment.GetExternalStoragePublicDirectory(name)!.CanonicalPath);
#pragma warning restore CS0618 // Type or member is obsolete

		public static StorageFolder MusicLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMusic!);
		public static StorageFolder VideosLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMovies!);

	}
}
