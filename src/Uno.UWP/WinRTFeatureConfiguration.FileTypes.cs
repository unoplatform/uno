using System;
using System.Collections.Generic;

namespace Uno
{
	partial class WinRTFeatureConfiguration
	{
		public static class FileTypes
		{
			/// <summary>
			/// Allows definition of custom file type MIME mappings.
			/// </summary>
			public static Dictionary<string, string> FileTypeToMimeMapping { get; } = new Dictionary<string, string>();

			/// <summary>
			/// Allows definition of custom file type UTType mappings.
			/// </summary>
			public static Dictionary<string, string> FileTypeToUTTypeMapping { get; } = new Dictionary<string, string>();
		}

		public static class Storage
		{
			public static class Pickers
			{
#if UNO_REFERENCE_API
				/// <summary>
				/// Gets or sets a value indicating whether the file pickers based on
				/// JS File System Access API are used. When set to false, or if the user's
				/// browser doesn't support the File System Access APIs, FileOpenPicker
				/// and FileSavePicker will default to "upload" and "download"
				/// based pickers.
				/// </summary>
				public static WasmPickerConfiguration WasmConfiguration { get; set; } = WasmPickerConfiguration.FileSystemAccessApiWithFallback;
#endif
			}
		}
	}

#if UNO_REFERENCE_API
	[Flags]
	public enum WasmPickerConfiguration
	{
		FileSystemAccessApi = 1,
		DownloadUpload = 2,
		FileSystemAccessApiWithFallback = FileSystemAccessApi | DownloadUpload,
	}
#endif
}
