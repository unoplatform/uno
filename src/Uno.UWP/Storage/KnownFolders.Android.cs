#if __ANDROID__

namespace Windows.Storage
{
	public static partial class KnownFolders
	{

		internal static void CheckPermission()
		{
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
            {	// older Android - permission is not required
				return;
			}

			string permissionName = "";

			if(Windows.Extensions.PermissionHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadExternalStorage))
			{
					permissionName = Android.Manifest.Permission.ReadExternalStorage;
			}

			if(Windows.Extensions.PermissionHelper.IsDeclaredInManifest(Android.Manifest.Permission.WriteExternalStorage)
			{
					permissionName = Android.Manifest.Permission.WriteExternalStorage; // // READ is included in WRITE
			}

			if (string.IsNullOrEmpty(permissionName))
			{
				throw new UnauthorizedAccessException("KnownFolders: you have to request at least one (Read or Write ExternalStorage) permission in Manifest");
			}

			// have to re-implement Windows.Extensions.PermissionHelper.CheckPermission without async/await
			Android.Content.Context context = Android.App.Application.Context;
			if (context.PackageManager.CheckPermission(permissionName, context.PackageName) != Android.Content.PM.Permission.Granted)
			{
				// should ask for it - but asking for this requires async/await, and UWP API is not async here (MusicLibrary, not MusicLibraryAsync)
				throw new NotImplementedException("KnownFolders: permission is not granted - asking for it is unimplemented (yet)");
			}

		}

		internal static StorageFolder FolderFromAndroidName(string name, bool createDir)
		{
			// createDir = true if directory should be created if doesn't exist. All UWP libraries exist by definition, on Android - such folders could not exist.
			CheckPermission();

#pragma warning disable CS0618 // Type or member is obsolete
			string folderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(name).CanonicalPath;
#pragma warning restore CS0618 // Type or member is obsolete

			if (createDir && !Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			return new StorageFolder(folderPath);
		}

		public static StorageFolder MusicLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMusic, true); 
		public static StorageFolder VideosLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMovies, true);
		public static StorageFolder DocumentsLibrary { get => FolderFromAndroidName(Android.OS.Environment.DirectoryDocuments, true); }
		public static StorageFolder CameraRoll { get => GetCameraFolder(); }

		internal static StorageFolder GetCameraFolder()
		{
			CheckPermission();

			var dcimFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim);
			string dcimCanonicalPath = dcimFolder.CanonicalPath;

			foreach (var folderName in Directory.GetDirectories(dcimCanonicalPath))
			{
				if(folderName.ToLower().EndsWith("/camera"))
				{
					return new StorageFolder(folderName);
				}
			}

			// UWP: "If the Camera Roll folder doesn't exist, reading the value of this property creates it"
			Directory.CreateDirectory(dcimCanonicalPath + "/Camera");
			return new StorageFolder(dcimCanonicalPath + "/Camera");
		}

	}
}

#endif
