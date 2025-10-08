#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;
using static System.Environment;
namespace Uno.UI.Helpers
{
	internal static class PickerHelpers
	{
		// These GUIDs are documented at https://learn.microsoft.com/en-us/windows/win32/shell/knownfolderid
		internal static readonly Guid WindowsComputerFolderGUID = new("0AC0837C-BBF8-452A-850D-79D08E667CA7");
		private static readonly Guid WindowsDownloadsGUID = new("374DE290-123F-4565-9164-39C4925E467B");

		public static string GetInitialDirectory(PickerLocationId location)
		{
			switch (location)
			{
				case PickerLocationId.DocumentsLibrary:
					return GetFolderPath(SpecialFolder.MyDocuments);
				case PickerLocationId.ComputerFolder:
					return @"/";
				case PickerLocationId.Desktop:
					return GetFolderPath(SpecialFolder.Desktop);
				case PickerLocationId.MusicLibrary:
					return GetFolderPath(SpecialFolder.MyMusic);
				case PickerLocationId.PicturesLibrary:
					return GetFolderPath(SpecialFolder.MyPictures);
				case PickerLocationId.VideosLibrary:
					return GetFolderPath(SpecialFolder.MyVideos);
				case PickerLocationId.Downloads:
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					{
						return GetEnvironmentVariable("XDG_DOWNLOAD_DIR") is { } s && !string.IsNullOrEmpty(s) ?
							s :
							$"{GetFolderPath(SpecialFolder.UserProfile)}/Downloads";
					}
					if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					{
						return $"{GetFolderPath(SpecialFolder.UserProfile)}/Downloads";
					}
					if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					{
						// https://stackoverflow.com/a/21953690
						return SHGetKnownFolderPath(WindowsDownloadsGUID, 0);
					}

					return string.Empty;
				default:
					return string.Empty;
			}
		}

		[DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
		private static extern string SHGetKnownFolderPath(
			[MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = 0);
	}
}
