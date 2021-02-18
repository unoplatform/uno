#nullable enable

using Windows.Storage.Pickers;
using static System.Environment;

namespace Uno.Extensions.Storage.Pickers
{
	internal static class PickerHelpers
    {
		// Special CLSID for the "virtual" Computer folder (https://www.autohotkey.com/docs/misc/CLSID-List.htm)
		private const string ComputerFolderClsid = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

		public static string GetInitialDirectory(PickerLocationId location)
		{
			switch (location)
			{
				case PickerLocationId.DocumentsLibrary:
					return GetFolderPath(SpecialFolder.MyDocuments);
				case PickerLocationId.ComputerFolder:
					return ComputerFolderClsid;
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

		public static SpecialFolder GetInitialSpecialFolder(PickerLocationId location)
		{
			switch (location)
			{
				case PickerLocationId.DocumentsLibrary:
					return SpecialFolder.MyDocuments;
				case PickerLocationId.Desktop:
					return SpecialFolder.Desktop;
				case PickerLocationId.MusicLibrary:
					return SpecialFolder.MyMusic;
				case PickerLocationId.PicturesLibrary:
					return SpecialFolder.MyPictures;
				case PickerLocationId.VideosLibrary:
					return SpecialFolder.MyVideos;
				default:
					return SpecialFolder.MyComputer;
			}
		}
    }
}
