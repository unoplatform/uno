#if __ANDROID__

namespace Windows.Storage
{
	public static partial class KnownFolders
	{
		internal static StorageFolder FolderFromAndroidName(string name) => new StorageFolder(Android.OS.Environment.GetExternalStoragePublicDirectory(name).CanonicalPath);
		public static StorageFolder MusicLibrary { get => FolderFromAndroidName(Android.OS.Environment.DirectoryMusic); }
		public static StorageFolder VideosLibrary { get => FolderFromAndroidName(Android.OS.Environment.DirectoryMovies); }

	}
}

#endif 
