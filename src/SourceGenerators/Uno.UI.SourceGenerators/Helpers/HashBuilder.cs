using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.Helpers
{
	internal class HashBuilder
	{
		private static readonly string[] _formattedBytes =
		{
			"00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0a", "0b", "0c", "0d", "0e", "0f",
			"10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1a", "1b", "1c", "1d", "1e", "1f",
			"20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2a", "2b", "2c", "2d", "2e", "2f",
			"30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3a", "3b", "3c", "3d", "3e", "3f",
			"40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4a", "4b", "4c", "4d", "4e", "4f",
			"50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5a", "5b", "5c", "5d", "5e", "5f",
			"60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6a", "6b", "6c", "6d", "6e", "6f",
			"70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7a", "7b", "7c", "7d", "7e", "7f",
			"80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8a", "8b", "8c", "8d", "8e", "8f",
			"90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9a", "9b", "9c", "9d", "9e", "9f",
			"a0", "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "aa", "ab", "ac", "ad", "ae", "af",
			"b0", "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "ba", "bb", "bc", "bd", "be", "bf",
			"c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9", "ca", "cb", "cc", "cd", "ce", "cf",
			"d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "d8", "d9", "da", "db", "dc", "dd", "de", "df",
			"e0", "e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "e9", "ea", "eb", "ec", "ed", "ee", "ef",
			"f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "fa", "fb", "fc", "fd", "fe", "ff"
		};

		/// <summary>
		/// Build a stable ID from the provided symbol
		/// </summary>
		public static string BuildIDFromSymbol(INamedTypeSymbol typeSymbol)
			=> typeSymbol.GetFullMetadataNameForFileName() + "_" + Build(typeSymbol.MetadataName + typeSymbol.ContainingAssembly.MetadataName);

		/// <summary>
		/// Creates a non-cryptographically secure hash of the provided string, clamped to 16 bytes.
		/// </summary>
		/// <remarks>
		/// The 16 bytes clamping is required on some platforms combinations to avoid having long file paths. The hash is used
		/// to limit collisions for duplicate file names in different folders, used in the same generator.
		/// </remarks>
		/// <param name="input">The string to hash</param>
		/// <param name="algoritm">The algorithm to use</param>
		/// <returns>A hex-formatted string of the hash</returns>
		public static string Build(string input)
		{
			using (var algorithm = SHA256.Create())
			{
				var data = algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

				var min = Math.Min(data.Length, 16);

				Span<char> span = stackalloc char[32];
				for (int i = 0; i < min; i++)
				{
					var s = _formattedBytes[data[i]];
					span[i * 2] = s[0];
					span[i * 2 + 1] = s[1];
				}


				return span.Slice(0, min).ToString();
			}
		}
	}
}
