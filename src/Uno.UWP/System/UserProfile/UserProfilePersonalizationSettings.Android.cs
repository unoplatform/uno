using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Uno.UI;
using Windows.Foundation;
using Windows.Storage;

namespace Windows.System.UserProfile
{
	public partial class UserProfilePersonalizationSettings
	{
		public static UserProfilePersonalizationSettings Current { get; } = new UserProfilePersonalizationSettings();

		public static bool IsSupported()
		{
			using var wallpaperManager = GetWallpaperManager();
			return wallpaperManager.IsWallpaperSupported && wallpaperManager.IsSetWallpaperAllowed;
		}

		public IAsyncOperation<bool> TrySetLockScreenImageAsync(StorageFile imageFile) =>
			TrySetImageAsync(imageFile, WallpaperManagerFlags.Lock).AsAsyncOperation();

		public IAsyncOperation<bool> TrySetWallpaperImageAsync(StorageFile imageFile) =>
			TrySetImageAsync(imageFile, WallpaperManagerFlags.System).AsAsyncOperation();

		private static WallpaperManager GetWallpaperManager()
		{
			if (ContextHelper.Current == null)
			{
				throw new InvalidOperationException("Operation called too early in application lifecycle.");
			}

			return WallpaperManager.GetInstance(ContextHelper.Current)!;
		}

		private Task<bool> TrySetImageAsync(StorageFile imageFile, WallpaperManagerFlags target)
		{
			using var wallpaperManager = GetWallpaperManager();
			using var stream = File.OpenRead(imageFile.Path);
			var id = wallpaperManager.SetStream(stream, null, true, target);
			return Task.FromResult(id != 0); //as per docs - if 0 is returned, setting wallpaper failed			
		}
	}
}
