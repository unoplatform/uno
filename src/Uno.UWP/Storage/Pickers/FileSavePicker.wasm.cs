#if __WASM__
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FileSavePicker
	{
		public PickerLocationId SuggestedStartLocation { get; set; }

		public IAsyncOperation<StorageFile> PickSaveFileAsync() => PickFilesTask().AsAsyncOperation();

		//private async Task<StorageFile> PickFilesTask()
		//{
		//	var test = DictionnaryToString(FileTypeChoices);
		//	Console.WriteLine(test);

		//	var location = @"C:\Users\Alex\Documents\Test.txt";
		//	//	WebAssemblyRuntime.InvokeJS($"Windows.Storage.Pickers.FileSavePicker.SelectFileLocation(" +
		//	//	$"{test}, " +
		//	//	$"{SuggestedStartLocation}, " +
		//	//	$"{SuggestedFileName } ) ");
		//	Console.WriteLine(location);

		//	var what = File.Create(location);
		//	what.Close();

		//	return await StorageFile.GetFileFromPathAsync(location);
		//}

		private async Task<StorageFile> PickFilesTask()
		{
			var temporaryFolder = ApplicationData.Current.LocalFolder;
			var file = await temporaryFolder.CreateFileAsync(SuggestedFileName, CreationCollisionOption.ReplaceExisting);
			return file;
		}

		private string DictionnaryToString(IDictionary<string, IList<string>> fileTypeChoices)
		{
			var result = new StringBuilder();
			result.Append("{");

			foreach (var fileType in fileTypeChoices)
			{
				result.Append($"{fileType.Key}: [");
				foreach (var extensions in fileType.Value)
				{
					result.Append($"{extensions},");
				}
				result.Length--;
				result.Append($"],");
			}
				result.Length--;

			return result.Append("}").ToString();
		}
	}
}
#endif
