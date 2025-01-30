#nullable enable

using System;
using System.Globalization;
using System.Linq;

namespace Uno
{
	internal static partial class AndroidResourceNameEncoder
	{
		private const string NumberPrefix = "__";

		/// <summary>
		/// Encode a resource name to remove characters that are not supported on Android.
		/// </summary>
		/// <param name="key">The original resource name from the UWP Resources.resw file.</param>
		/// <returns>The encoded resource name for the Android Strings.xml file.</returns>
		public static string Encode(string key)
		{
			key ??= string.Empty;

			var charArray = key.ToCharArray();
			for (int i = 0; i < charArray.Length; i++)
			{
				// Checks whether the key contains unsupported characters
				// These characters are not supported on Android, but they're used by the attached property localization syntax.
				// Example: "MyUid.[using:Windows.UI.Xaml.Automation]AutomationProperties.Name"
				if (charArray[i] is not ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '.'))
				{
					charArray[i] = '_';
				}
			}

			key = new string(charArray);

			//Checks if the keys are starting by a number because they are invalid in C#
			if (key.Length > 0 && int.TryParse(key.Substring(0, 1), out _))
			{
				key = $"{NumberPrefix}{key}";
			}

			if (key.EndsWith(".9", StringComparison.Ordinal))
			{
				// Specific handling of 9-patch extension
				key = key.Substring(0, key.Length - 2).Replace(".", "_") + ".9";
			}
			else
			{
				key = key.Replace(".", "_");
			}

			return key;
		}

		public static string EncodeFileSystemPath(string path, string prefix = "Assets")
			// Android assets need to placed in the Assets folder
			=> global::System.IO.Path.Combine(prefix, EncodePath(path, global::System.IO.Path.DirectorySeparatorChar));

		public static string EncodeResourcePath(string path)
			=> EncodePath(path, '/');

		public static string EncodeDrawablePath(string path)
			=> EncodeResourcePath(path).Replace('/', '_');

		private static string EncodePath(string path, char separator)
		{
			var localSeparation = global::System.IO.Path.DirectorySeparatorChar;

			var alignedPath = path.Replace(separator, localSeparation);

			var directoryName = global::System.IO.Path.GetDirectoryName(alignedPath) ?? "";
			var fileName = global::System.IO.Path.GetFileNameWithoutExtension(alignedPath);
			var extension = global::System.IO.Path.GetExtension(alignedPath);

			var encodedDirectoryParts = directoryName
				.Split(new[] { localSeparation }, StringSplitOptions.RemoveEmptyEntries)
				.Select(Encode)
				.ToArray();

			var encodedDirectory = global::System.IO.Path.Combine(encodedDirectoryParts);
			var encodedFileName = Encode(fileName);

			return global::System.IO.Path.Combine(encodedDirectory, encodedFileName + extension).Replace(localSeparation, separator);
		}
	}
}
