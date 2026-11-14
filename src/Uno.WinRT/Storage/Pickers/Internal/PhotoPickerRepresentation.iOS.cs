#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Uno.Storage.Pickers.Internal;

/// <summary>
/// The representation that a photo or video picked with <c>PHPickerViewController</c> is loaded as: one of the types its item provider
/// registers, and the extension of the file it is copied to. The pure part of copying picked items, split out so it is unit-testable.
/// </summary>
/// <param name="TypeIdentifier">The uniform type identifier to load.</param>
/// <param name="FileExtension">The extension of the file, without the dot, or null if the type has none.</param>
internal sealed record PhotoPickerRepresentation(string TypeIdentifier, string? FileExtension)
{
	// Photos registers private types after the asset's own ones: thumbnails, and a smaller copy of a video made for mail.
	private const string PrivateTypePrefix = "com.apple.private.";
	private const string LivePhotoTypePrefix = "com.apple.live-photo";
	private const string JpegType = "public.jpeg";
	private const string QuickTimeMovieType = "com.apple.quicktime-movie";
	private const int MaxFileNameBytes = 255;

	/// <summary>
	/// Chooses the representation to load a picked photo or video as, among the registered types that are files the filter allows,
	/// such as the JPEG version that iOS converts a HEIC photo to: the JPEG version of a Live Photo, else the QuickTime version of a
	/// movie, else the first one.
	/// </summary>
	/// <param name="registeredTypes">The type identifiers the item provider registers, in its order.</param>
	/// <param name="getType">Gets what iOS declares about a type, or null for a type it doesn't know.</param>
	/// <param name="fileTypeFilter">The file type filter of the picker.</param>
	/// <returns>The representation, or null if the filter allows none of the types.</returns>
	internal static PhotoPickerRepresentation? Choose(
		IReadOnlyList<string> registeredTypes,
		Func<string, PhotoPickerType?> getType,
		IList<string> fileTypeFilter)
	{
		var isLivePhoto = registeredTypes.Any(identifier => identifier.StartsWith(LivePhotoTypePrefix, StringComparison.OrdinalIgnoreCase));
		PhotoPickerRepresentation? first = null;
		PhotoPickerRepresentation? jpeg = null;
		PhotoPickerRepresentation? quickTimeMovie = null;

		foreach (var identifier in registeredTypes)
		{
			if (identifier.StartsWith(PrivateTypePrefix, StringComparison.Ordinal)
				|| getType(identifier) is not { ConformsToData: true } type
				|| !TryGetAllowedExtension(type, fileTypeFilter, out var extension))
			{
				continue;
			}

			var representation = new PhotoPickerRepresentation(identifier, extension);
			first ??= representation;

			if (identifier == JpegType)
			{
				jpeg ??= representation;
			}
			else if (identifier == QuickTimeMovieType)
			{
				quickTimeMovie ??= representation;
			}
		}

		return (isLivePhoto ? jpeg : null) ?? quickTimeMovie ?? first;
	}

	/// <summary>
	/// Gets the name of the file to copy the picked photo or video to, with the extension of this representation.
	/// </summary>
	/// <param name="suggestedName">The name the item provider suggests, usually the name of the original file without its extension.</param>
	/// <param name="fallbackName">The name to use if the item provider suggests none.</param>
	internal string GetFileName(string? suggestedName, string fallbackName)
	{
		var suffix = FileExtension is null ? "" : "." + FileExtension;
		var maxNameBytes = MaxFileNameBytes - GetMaxStoredByteCount(suffix);

		var name = Truncate(StripSuffix(Clean(suggestedName), suffix), maxNameBytes);
		if (name.Length == 0)
		{
			name = Truncate(StripSuffix(Clean(fallbackName), suffix), maxNameBytes);
		}

		return name + suffix;
	}

	private static bool TryGetAllowedExtension(PhotoPickerType type, IList<string> fileTypeFilter, out string? extension)
	{
		if (fileTypeFilter.Contains("*"))
		{
			extension = type.FileExtensions.Count > 0 ? type.FileExtensions[0] : null;
			return true;
		}

		foreach (var candidate in type.FileExtensions)
		{
			if (fileTypeFilter.Any(allowed => allowed.Length == candidate.Length + 1 && allowed.EndsWith(candidate, StringComparison.OrdinalIgnoreCase)))
			{
				extension = candidate;
				return true;
			}
		}

		extension = null;
		return false;
	}

	// Path separators, control characters and lone surrogates can't be in a name, and a leading dot would hide the file.
	private static string Clean(string? name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "";
		}

		var builder = new StringBuilder(name.Length);
		for (var i = 0; i < name.Length; i++)
		{
			var c = name[i];
			if (char.IsHighSurrogate(c) && i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]))
			{
				builder.Append(c).Append(name[++i]);
			}
			else
			{
				builder.Append(c is '/' or ':' || char.IsControl(c) || char.IsSurrogate(c) ? '_' : c);
			}
		}

		var cleaned = builder.ToString();
		string trimmed;
		while ((trimmed = cleaned.Trim().TrimStart('.')) != cleaned)
		{
			cleaned = trimmed;
		}

		return cleaned;
	}

	private static string StripSuffix(string name, string suffix)
		=> suffix.Length > 0 && name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? name[..^suffix.Length] : name;

	// Keeps the whole text elements (such as a letter and its accents, or an emoji sequence) that fit in the given number of bytes.
	private static string Truncate(string name, int maxBytes)
	{
		var bytes = 0;
		for (var index = 0; index < name.Length;)
		{
			var length = StringInfo.GetNextTextElementLength(name.AsSpan(index));
			bytes += GetMaxStoredByteCount(name.AsSpan(index, length));
			if (bytes > maxBytes)
			{
				return name[..index].TrimEnd();
			}

			index += length;
		}

		return name;
	}

	// The file system limits names to 255 bytes in the decomposed form (NFD) that Apple's file APIs hand it. This is an upper bound of
	// that size, without Unicode normalization, which not every runtime has. Text below U+00C0, combining marks, conjoining jamo and
	// CJK ideographs are already decomposed (U+0344 becomes two marks), Hangul syllables decompose to 2 or 3 jamo of 3 bytes, kana to
	// at most a kana and a voicing mark, and decomposing makes nothing else more than 3 times as long in UTF-8
	// (https://www.unicode.org/faq/normalization.html).
	private static int GetMaxStoredByteCount(ReadOnlySpan<char> text)
	{
		var count = 0;
		foreach (var rune in text.EnumerateRunes())
		{
			count += rune.Value switch
			{
				0x0344 => 4,
				< 0xC0 or (>= 0x0300 and <= 0x036F) or (>= 0x1100 and <= 0x11FF) => rune.Utf8SequenceLength,
				(>= 0x3400 and <= 0x4DBF) or (>= 0x4E00 and <= 0x9FFF) => rune.Utf8SequenceLength,
				>= 0xAC00 and <= 0xD7A3 => (rune.Value - 0xAC00) % 28 == 0 ? 6 : 9,
				>= 0x3040 and <= 0x30FF => 2 * rune.Utf8SequenceLength,
				_ => 3 * rune.Utf8SequenceLength,
			};
		}

		return count;
	}
}
