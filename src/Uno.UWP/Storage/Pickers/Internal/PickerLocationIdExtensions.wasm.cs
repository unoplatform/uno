#nullable disable

using Windows.Storage.Pickers;

namespace Uno.Storage.Pickers
{
	internal static class PickerLocationIdExtensions
	{
		/// <summary>
		/// Converts the specified location to a JS Native File System Access
		/// startIn directory.
		/// </summary>
		/// <param name="location">The PickerLocationId to convert.</param>
		/// <returns>The JS Native File System Access startIn directory.</returns>
		/// <remarks>
		/// See https://wicg.github.io/file-system-access/#api-filepickeroptions-starting-directory
		/// for the list of supported starting directories.
		/// </remarks>
		internal static string ToStartInDirectory(this PickerLocationId location) =>
			location switch
			{
				PickerLocationId.DocumentsLibrary => "documents",
				PickerLocationId.Desktop => "desktop",
				PickerLocationId.Downloads => "downloads",
				PickerLocationId.MusicLibrary => "music",
				PickerLocationId.PicturesLibrary => "pictures",
				PickerLocationId.VideosLibrary => "videos",
				_ => ""
			};
	}
}
