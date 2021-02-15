#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation;

namespace Windows.Storage.Pickers
{
	public partial class FileOpenPicker
	{
		private const string JsType = "Windows.Storage.Pickers.FileOpenPicker";
		private const char SelectedFileGuidSeparator = ';';
		private const char FileTypeSeparator = '/';

		private async Task<StorageFile?> PickSingleFileTaskAsync(CancellationToken token)
		{
			var files = await PickFilesAsync(false, token);
			return files.FirstOrDefault();
		}

		private async Task<IReadOnlyList<StorageFile>> PickMultipleFilesTaskAsync(CancellationToken token)
		{
			return await PickFilesAsync(true, token);
		}

		private async Task<FilePickerSelectedFilesArray> PickFilesAsync(bool multiple, CancellationToken token)
		{
			var showAllEntryParameter = FileTypeFilter.Contains("*") ? "true" : "false";
			var multipleParameter = multiple ? "true" : "false";
			var fileTypeMapParameter = $"\"{BuildFileTypesMap()}\"";

			var returnValue = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickFilesAsync({multipleParameter},{showAllEntryParameter},{fileTypeMapParameter})");
			var guids = returnValue.Split(SelectedFileGuidSeparator);
			return new FilePickerSelectedFilesArray(guids.Select(guid => StorageFile.GetFileFromNativePathAsync("", Guid.Parse(guid))).ToArray());

			//if (!Guid.TryParse(returnValue, out var guid))
			//{
			//	throw new InvalidOperationException("GUID could not be parsed");
			//}

			//return StorageFolder.GetFolderFromNativePathAsync("", guid);
		}

		private string BuildFileTypesMap()
		{
			var mimeTypeMap = new Dictionary<string, List<string>>();
			foreach (var fileType in FileTypeFilter)
			{
				if (fileType == "*")
				{
					continue;
				}

				if (!WinRTFeatureConfiguration.FileTypes.FileTypeToMimeMapping.TryGetValue(fileType, out var mimeType))
				{
					mimeType = "unknown/unknown";
				}

				if (!mimeTypeMap.TryGetValue(mimeType, out var extensionList))
				{
					extensionList = new List<string>();
					mimeTypeMap[mimeType] = extensionList;
				}
				extensionList.Add(fileType);
			}

			// Build JSON object with the extensions/MIME types
			var builder = new StringBuilder();
			builder.Append("{");
			bool firstItem = true;
			foreach (var mimeType in mimeTypeMap)
			{
				if (!firstItem)
				{
					builder.Append(",");
				}
				firstItem = false;

				builder.Append("'");
				builder.Append(mimeType.Key.Replace("'", "\'"));
				builder.Append("':[");
				builder.Append(string.Join(",", mimeType.Value.Select(extension => "'" + extension.Replace("'", "\'") + "'")));
				builder.Append("]");
			}
			builder.Append("}");
			return builder.ToString();
		}
	}
}
