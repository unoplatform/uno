#nullable enable

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using IOPath = System.IO.Path;

namespace Windows.Storage.Pickers;

public partial class FileSavePicker
{
	private const int MaxFileNameBytes = 255;
	private const int MaxExtensionBytes = 16;

	// No iOS picker returns a new writable file before its content exists, so the file is staged in the temporary directory, and
	// CachedFileManager.CompleteUpdatesAsync saves a copy of it with the system's export sheet.
	private StorageFile PickExportFile(CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		return StorageFile.CreateForExportPicker(GetExportFileName());
	}

	// Dots around the name are removed as well as whitespace, since Files hides names that start with a dot. As on Android, the first
	// FileTypeChoices extension is added unless the name already ends with one of them.
	private string GetExportFileName()
	{
		var name = SuggestedSaveFile?.Name is { Length: > 0 } saveFileName ? saveFileName : SuggestedFileName;
		name = TrimWhiteSpaceAndDots(new string(name.Select(c => IsReplacedInName(c) ? '_' : c).ToArray()));

		var extensions = FileTypeChoices.Values
			.SelectMany(choice => choice)
			.Where(IsUsableExtension)
			.ToArray();

		string extension;

		if (extensions.FirstOrDefault(e => name.EndsWith(e, StringComparison.OrdinalIgnoreCase)) is { } present)
		{
			extension = name[^present.Length..];
		}
		else if (extensions.Length > 0)
		{
			extension = extensions[0];
			name += extension;
		}
		else
		{
			// Without choices, only what looks like an extension (16 bytes at most, including the dot, and no whitespace) is kept as one.
			extension = IOPath.GetExtension(name);
			if (Encoding.UTF8.GetByteCount(extension) > MaxExtensionBytes || extension.Any(char.IsWhiteSpace))
			{
				extension = string.Empty;
			}
		}

		var stem = TrimWhiteSpaceAndDots(TrimToBytes(name[..^extension.Length], MaxFileNameBytes - Encoding.UTF8.GetByteCount(extension)));
		if (stem.Length == 0)
		{
			stem = Guid.NewGuid().ToString();
		}

		return TrimToBytes(stem + extension, MaxFileNameBytes);
	}

	private static bool IsReplacedInName(char c) => c is '/' or ':' || char.IsControl(c);

	// FileTypeChoices accepts any extension that starts with a dot, so one that could take the file out of its staging folder, or that
	// has a character the name can't have, is skipped.
	private static bool IsUsableExtension(string extension)
		=> extension.Length > 1
			&& extension[0] == '.'
			&& !extension.Contains('*')
			&& !extension.Contains("..", StringComparison.Ordinal)
			&& !extension.Any(c => c == '\\' || IsReplacedInName(c));

	private static string TrimWhiteSpaceAndDots(string text)
	{
		var start = 0;
		var end = text.Length;

		while (start < end && (char.IsWhiteSpace(text[start]) || text[start] == '.'))
		{
			start++;
		}

		while (end > start && (char.IsWhiteSpace(text[end - 1]) || text[end - 1] == '.'))
		{
			end--;
		}

		return text[start..end];
	}

	private static string TrimToBytes(string text, int maxBytes)
	{
		if (Encoding.UTF8.GetByteCount(text) <= maxBytes)
		{
			return text;
		}

		var trimmed = new StringBuilder();
		var bytes = 0;
		var elements = StringInfo.GetTextElementEnumerator(text);

		while (elements.MoveNext())
		{
			var element = elements.GetTextElement();
			bytes += Encoding.UTF8.GetByteCount(element);

			if (bytes > maxBytes)
			{
				break;
			}

			trimmed.Append(element);
		}

		return trimmed.ToString();
	}
}
