#nullable enable

using System.Collections.Generic;
using System.Linq;
using MobileCoreServices;

namespace Uno.Storage.Pickers.Internal
{
	internal static class UTTypeMapper
	{
		/// <summary>
		/// Gets UTTypes for given list of extensions.
		/// </summary>
		/// <param name="extensions">File extensions.</param>
		/// <returns>UTTypes.</returns>
		public static string[] GetDocumentTypes(IEnumerable<string> extensions)
		{
			var utTypes = new HashSet<string>();
			foreach (var extension in extensions)
			{
				if (extension == "*")
				{
					// Return the default "catch" all
					utTypes.Add(UTType.Content);
					utTypes.Add(UTType.Item);
					utTypes.Add("public.data");
				}
				else
				{
					utTypes.Add(GetFromExtension(extension));
				}
			}
			return utTypes.ToArray();
		}

		/// <summary>
		/// Gets a UTType for a given file extension.
		/// Uses <see cref="WinRTFeatureConfiguration.FileTypes.FileTypeToUTTypeMapping"/> for custom mappings.
		/// </summary>
		/// <param name="fileExtension">File extension.</param>
		/// <returns>UTType identifier.</returns>
		private static string GetFromExtension(string? fileExtension)
		{
			if (WinRTFeatureConfiguration.FileTypes.FileTypeToUTTypeMapping.TryGetValue(fileExtension, out var customUTType))
			{
				return customUTType;
			}

			return GetBuiltInType(fileExtension);
		}

		/// <summary>
		/// Gets a UTType from a list of known types.
		/// Mainly from <see cref="https://developer.apple.com/documentation/uniformtypeidentifiers/uttype/system_declared_types">Apple docs</see>.
		/// </summary>
		private static string GetBuiltInType(string? extension)
			=> extension?.ToLowerInvariant() switch
			{
				".avi" => UTType.AVIMovie,
				".bin" => "com.apple.binhex-archive",
				".bmp" => UTType.BMP,
				".bz2" => UTType.Bzip2Archive,
				".h" => UTType.CHeader,
				".hpp" => UTType.CPlusPlusHeader,
				".c" => UTType.CSource,
				".cpp" => UTType.CPlusPlusSource,
				".css" => "public.css",
				".csv" => UTType.CommaSeparatedText,
				".doc" => "com.microsoft.word.doc",
				".docx" => "org.openxmlformats.wordprocessingml.document",
				".epub" => UTType.ElectronicPublication,
				".gz" => UTType.GNUZipArchive,
				".gif" => UTType.GIF,
				".htm" => UTType.HTML,
				".html" => UTType.HTML,
				".ico" => UTType.ICO,
				".ics" => UTType.CalendarEvent,
				".jar" => UTType.JavaArchive,
				".jpeg" => UTType.JPEG,
				".jpg" => UTType.JPEG,
				".js" => UTType.JavaScript,
				".json" => UTType.JSON,
				".mid" => UTType.MIDIAudio,
				".midi" => UTType.MIDIAudio,
				".mp3" => UTType.MP3,
				".mp4" => UTType.MPEG4,
				".mpeg" => UTType.MPEG,
				".otf" => UTType.Font,
				".png" => UTType.PNG,
				".pdf" => UTType.PDF,
				".php" => UTType.PHPScript,
				".ppt" => "com.microsoft.powerpoint.​ppt",
				".pptx" => "org.openxmlformats.presentationml.presentation",
				".rtf" => UTType.RTF,
				".sh" => UTType.ShellScript,
				".svg" => UTType.ScalableVectorGraphics,
				".tif" => UTType.TIFF,
				".tiff" => UTType.TIFF,
				".ttf" => UTType.Font,
				".txt" => UTType.Text,
				".wav" => UTType.WaveformAudio,
				".woff" => UTType.Font,
				".woff2" => UTType.Font,
				".xhtml" => UTType.HTML,
				".xls" => "com.microsoft.excel.xls",
				".xlsx" => "org.openxmlformats.spreadsheetml.sheet",
				".xml" => UTType.XML,
				".zip" => UTType.ZipArchive,
				string unknownExtension => unknownExtension.TrimStart(new char[] { '.' }),
				null => UTType.Content
			};
	}
}
