#if __ANDROID__

using System;
using System.IO;
using Android.OS;

namespace Windows.Storage
{
	public static partial class KnownFolders
	{
		internal static void AndroidQRemediationCheck()
		{
			// first, check if workaround is needed - if not, return 
			// should be `if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q)`
			// correct in VStudio/Intellisense, but CI reports
			// Error CS0117: 'BuildVersionCodes' does not contain a definition for 'Q'
			if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.P)
			{
				return;
			}


			Android.Content.Context context = context = Android.App.Application.Context;
			var apkInfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);

			// check if workaround is "timeouted", as stated in documentation:
			// https://developer.android.com/training/data-storage/use-cases#opt-out-scoped-storage
			// After you update your app to target Android 11 (API level 30), the system ignores the requestLegacyExternalStorage attribute when your app is running on Android 11 devices
			// should be `if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.Q)`
			if ((int)Android.OS.Build.VERSION.SdkInt > 29)
			{ 
				var apkSdkVers = apkInfo.ApplicationInfo.TargetSdkVersion;
				// same as above, `if(apkSdkVers > Android.OS.BuildVersionCodes.Q)`
				if ((int)apkSdkVers > 29)
				{
					// sorry, our workaround would not work
					throw new UnauthorizedAccessException("KnownFolders: On apps targetting Android 11+, on Android 11+, requestLegacyExternalStorage doesn't work");
				}
			}

			// and now, we should check if workaround is in place, i.e.
			// do we have
			// android:requestLegacyExternalStorage="true"
			// defined in `<application ` node in Manifest file

			// assume we have...
		}


		internal static void CheckPermission()
		{
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.M)
            {	// older Android - permission is not required
				return;
			}

			string permissionName = "";

			if(Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadExternalStorage))
			{
					permissionName = Android.Manifest.Permission.ReadExternalStorage;
			}

			if(Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.WriteExternalStorage))
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

			AndroidQRemediationCheck();

			// createDir = true if directory should be created if doesn't exist. All UWP libraries exist by definition, on Android - such folders could not exist.
			CheckPermission();

			// since Android 10 (Q), it would not work
#pragma warning disable CS0618 // Type or member is obsolete
			string folderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(name).CanonicalPath;
#pragma warning restore CS0618 // Type or member is obsolete

			if (createDir && !Directory.Exists(folderPath))
			{
				Directory.CreateDirectory(folderPath);
			}

			return new StorageFolder(folderPath);
		}

		/// <remarks>
		/// This method would work only on Android < 11,
		/// It requires ReadExternalStorage or WriteExternalStorage permission in Manifest
		/// For Android 10, also add `<application android:requestLegacyExternalStorage="true"` to Manifest 
		/// </remarks>
		public static StorageFolder MusicLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMusic, true);
		/// <remarks>
		/// This method would work only on Android < 11,
		/// It requires ReadExternalStorage or WriteExternalStorage permission in Manifest
		/// For Android 10, also add `<application android:requestLegacyExternalStorage="true"` to Manifest 
		/// </remarks>
		public static StorageFolder VideosLibrary => FolderFromAndroidName(Android.OS.Environment.DirectoryMovies, true);
		/// <remarks>
		/// This method would work only on Android < 11,
		/// It requires ReadExternalStorage or WriteExternalStorage permission in Manifest
		/// For Android 10, also add `<application android:requestLegacyExternalStorage="true"` to Manifest 
		/// </remarks>
		public static StorageFolder DocumentsLibrary { get => FolderFromAndroidName(Android.OS.Environment.DirectoryDocuments, true); }
		/// <remarks>
		/// This method would work only on Android < 11,
		/// It requires ReadExternalStorage or WriteExternalStorage permission in Manifest
		/// For Android 10, also add `<application android:requestLegacyExternalStorage="true"` to Manifest 
		/// </remarks>
		public static StorageFolder CameraRoll { get => GetCameraFolder(); }

		internal static StorageFolder GetCameraFolder()
		{

			AndroidQRemediationCheck();

			CheckPermission();

			// getExternalStoragePublicDirectory(String type)
			// This method was deprecated in API level 29.
			// When an app targets Build.VERSION_CODES.Q, the path returned from this method is no longer directly accessible to apps.

#pragma warning disable CS0618 // Type or member is obsolete
			var dcimFolder = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim);
#pragma warning restore CS0618 // Type or member is obsolete
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
