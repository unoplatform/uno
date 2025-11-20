using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.VS.Helpers;

internal class EncodingHelpers
{
	public static System.Text.Encoding DetectFileEncoding(string filePath)
	{
		if (!File.Exists(filePath))
		{
			return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
		}

		Encoding encoding;
		using (var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true))
		{
			reader.Peek(); // Need to read at least one char to detect encoding
			encoding = reader.CurrentEncoding;
		}

		// Confirm BOM presence for UTF encodings
		switch (encoding)
		{
			case UTF8Encoding:
				{
					using var file = File.OpenRead(filePath);
					encoding = (file.Length < 3 ? (0x00, 0x00, 0x00) : (file.ReadByte(), file.ReadByte(), file.ReadByte())) switch
					{
						(0xEF, 0xBB, 0xBF) => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
						_ => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
					};
					break;
				}
			case UnicodeEncoding: // UTF16
				{
					using var file = File.OpenRead(filePath);
					encoding = (file.Length < 2 ? (0x00, 0x00) : (file.ReadByte(), file.ReadByte())) switch
					{
						(0xFE, 0xFF) => new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
						(0xFF, 0xFE) => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
						_ => new UnicodeEncoding(bigEndian: encoding.CodePage is 1201, byteOrderMark: false),
					};
					break;
				}
			case UTF32Encoding:
				{
					using var file = File.OpenRead(filePath);
					encoding = (file.Length < 2 ? (0x00, 0x00) : (file.ReadByte(), file.ReadByte())) switch
					{
						(0xFE, 0xFF) => new UTF32Encoding(bigEndian: true, byteOrderMark: true),
						(0xFF, 0xFE) => new UTF32Encoding(bigEndian: false, byteOrderMark: true),
						_ => new UTF32Encoding(bigEndian: encoding.CodePage is 12001, byteOrderMark: false),
					};
					break;
				}
		}

		return encoding;
	}

	private static bool SupportsUnicode(System.Text.Encoding encoding)
	{
		// Check if the encoding supports Unicode characters (including emojis)
		// UTF-8, UTF-16, and UTF-32 all support the full Unicode range
		var isSupported = encoding is UTF8Encoding
			|| encoding is UnicodeEncoding
			|| encoding is UTF32Encoding
			|| encoding.CodePage == 65001  // UTF-8
			|| encoding.CodePage == 1200   // UTF-16 LE
			|| encoding.CodePage == 1201   // UTF-16 BE
			|| encoding.CodePage == 12000  // UTF-32 LE
			|| encoding.CodePage == 12001; // UTF-32 BE

		// Even if supported, we validate if the BOM has been enabled
		if (!isSupported || encoding.GetPreamble() == Array.Empty<byte>())
		{
			return false;
		}

		return true;
	}

	private static bool ContainsSpecialCharacters(string content)
	{
		// Check if content contains characters outside the Basic Multilingual Plane (BMP)
		// This includes emojis and other special Unicode characters
		foreach (var c in content)
		{
			if (char.IsSurrogate(c))
			{
				return true;
			}
		}
		return false;
	}

	public static Encoding GetCompatibleEncoding(Encoding currentEncoding, string newContent)
		// If content has special characters and encoding doesn't support Unicode, upgrade to UTF-8 with BOM
		=> ContainsSpecialCharacters(newContent) && !SupportsUnicode(currentEncoding)
			? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
			: currentEncoding;

}
