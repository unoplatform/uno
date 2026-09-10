#nullable enable

using System;
using System.Linq;

namespace Uno;

internal static partial class AndroidResourceNameEncoder
{
	private const string NumberPrefix = "__";
	private const string NinePatchExtension = ".9.png";
	private const string NinePatchSuffix = ".9";

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

		if (key.EndsWith(NinePatchSuffix, StringComparison.Ordinal))
		{
			// Specific handling of 9-patch extension
			key = key.Substring(0, key.Length - NinePatchSuffix.Length).Replace(".", "_") + NinePatchSuffix;
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

	/// <summary>
	/// Encodes the path of a drawable to the file name it gets bundled under.
	/// </summary>
	/// <remarks>
	/// The original extension is folded into the resource name, so that assets which only differ
	/// by their extension don't end up sharing a single drawable. For example, <c>Assets/logo.png</c>
	/// becomes <c>Assets_logo_png.png</c> and <c>Assets/logo.9.png</c> becomes <c>Assets_logo_png.9.png</c>.
	/// </remarks>
	public static string EncodeDrawablePath(string path)
	{
		var fileName = global::System.IO.Path.GetFileName(AlignPath(path));

		return EncodeDrawableResourceName(path)
			+ (IsNinePatch(fileName) ? NinePatchSuffix : string.Empty)
			+ global::System.IO.Path.GetExtension(fileName);
	}

	/// <summary>
	/// Gets the Android resource name (i.e. the <c>R.drawable</c> field name) of a drawable path.
	/// </summary>
	/// <remarks>
	/// Android derives the resource name from the file name up to its first dot, so the result
	/// never contains one. Encoding an already encoded name returns it unchanged.
	/// </remarks>
	public static string EncodeDrawableResourceName(string path)
	{
		var alignedPath = AlignPath(path);
		var localSeparation = global::System.IO.Path.DirectorySeparatorChar;

		var parts = (global::System.IO.Path.GetDirectoryName(alignedPath) ?? "")
			.Split(new[] { localSeparation }, StringSplitOptions.RemoveEmptyEntries)
			.Select(Encode)
			.Append(EncodeDrawableName(global::System.IO.Path.GetFileName(alignedPath)));

		// Encode keeps a trailing ".9" on a directory segment (e.g. "v1.9"), which Android would
		// treat as the end of the resource name, so no dot may reach the joined result.
		return string.Join("_", parts).Replace('.', '_');
	}

	private static string EncodeDrawableName(string fileName)
	{
		// A 9-patch carries its ".9" marker in the file name, but never in the resource name
		var name = IsNinePatch(fileName)
			? fileName.Substring(0, fileName.Length - NinePatchExtension.Length)
			: global::System.IO.Path.GetFileNameWithoutExtension(fileName);
		var extension = global::System.IO.Path.GetExtension(fileName).TrimStart('.');

		return extension.Length > 0 ? Encode(name + "_" + extension) : Encode(name);
	}

	private static bool IsNinePatch(string fileName)
		=> fileName.EndsWith(NinePatchExtension, StringComparison.OrdinalIgnoreCase);

	private static string AlignPath(string path)
		=> path.Replace('/', global::System.IO.Path.DirectorySeparatorChar);

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
