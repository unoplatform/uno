using Windows.Storage.Pickers;

namespace Uno.Storage.Pickers
{
	internal static class PickerLocationIdExtensions
	{
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
