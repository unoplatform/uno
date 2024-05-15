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
				int i = 0, j = 0;
				while (i < min)
				{
					var b = data[i++];
					var nib = (b >> 4);
					span[j++] = nib > 9 ? (char)(nib + 'W') : (char)(nib + '0');
					nib = (b & 0xf);
					span[j++] = nib > 9 ? (char)(nib + 'W') : (char)(nib + '0');
				}

				return span.Slice(0, min * 2).ToString();
			}
		}
	}
}
