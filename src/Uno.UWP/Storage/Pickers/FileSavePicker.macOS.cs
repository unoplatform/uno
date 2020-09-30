#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Windows.Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		private const int ModalResponseOk = 1;

		private async Task<StorageFile?> PickSaveFileTaskAsync(CancellationToken token)
		{
			var savePicker = new NSSavePanel();
			savePicker.DirectoryUrl = new NSUrl(GetStartPath(), true);
			savePicker.AllowedFileTypes = GetFileTypes();
			if (!string.IsNullOrEmpty(CommitButtonText))
			{
				savePicker.Prompt = CommitButtonText;
			}
			if (SuggestedFileName != null)
			{
				savePicker.NameFieldStringValue = SuggestedFileName;
			}
			if (savePicker.RunModal() == ModalResponseOk)
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
				PickerLocationId.DocumentsLibrary => NSSearchPathDirectory.DocumentDirectory,
				PickerLocationId.Desktop => NSSearchPathDirectory.DesktopDirectory,
				PickerLocationId.MusicLibrary => NSSearchPathDirectory.MusicDirectory,
				PickerLocationId.PicturesLibrary => NSSearchPathDirectory.PicturesDirectory,
				PickerLocationId.VideosLibrary => NSSearchPathDirectory.MoviesDirectory,
				_ => NSSearchPathDirectory.UserDirectory
			};

			return NSFileManager.DefaultManager.GetUrls(specialFolder, NSSearchPathDomain.User)[0].AbsoluteString;
		}

		private string[] GetFileTypes() => FileTypeChoices.SelectMany(x => x.Value.Select(val => val.TrimStart(new[] { '.' }))).ToArray();
	}
}
