using Windows.Storage.Pickers;
using static System.Environment;

namespace Uno.Extensions.Storage.Pickers
{
	internal static class PickerHelpers
    {
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
				default:
					return string.Empty;
			}
		}
    }
}
