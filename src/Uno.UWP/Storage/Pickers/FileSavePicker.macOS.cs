using System;
using System.Linq;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Windows.Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		public PickerLocationId SuggestedStartLocation { get; set; }
		public IAsyncOperation<StorageFile> PickSaveFileAsync() => PickFileTaskAsync().AsAsyncOperation();


		private async Task<StorageFile> PickFileTaskAsync()
		{
			var savePicker = new NSSavePanel();
			savePicker.DirectoryUrl = new NSUrl(GetStartPath(), true);
			savePicker.AllowedFileTypes = GetFileTypes();
			if(SuggestedFileName != null)
			{
				savePicker.NameFieldStringValue = SuggestedFileName;
			}
			if (savePicker.RunModal() == 1)
			{
				return await StorageFile.GetFileFromPathAsync(savePicker.Url.Path);
			}
			else
			{
				return null;
			}
		}

		private string GetStartPath()
		{
			var specialFolder = SuggestedStartLocation switch
			{
				PickerLocationId.DocumentsLibrary => Environment.SpecialFolder.Personal,
				PickerLocationId.ComputerFolder => Environment.SpecialFolder.MyComputer,
				PickerLocationId.Desktop => Environment.SpecialFolder.Desktop,
				PickerLocationId.MusicLibrary => Environment.SpecialFolder.MyMusic,
				PickerLocationId.PicturesLibrary => Environment.SpecialFolder.MyPictures,
				PickerLocationId.VideosLibrary => Environment.SpecialFolder.MyVideos,
				_ => Environment.SpecialFolder.Personal

			};

			return Environment.GetFolderPath(specialFolder);
		}

		private string[] GetFileTypes() => FileTypeChoices.SelectMany(x => x.Value.Select(val=>val.TrimStart(new[] { '.' }))).ToArray();
	}
}
