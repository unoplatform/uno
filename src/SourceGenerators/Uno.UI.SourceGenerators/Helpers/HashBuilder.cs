using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.Helpers
{
	internal class HashBuilder
	{
		/// <summary>
		/// Build a stable ID from the provided symbol
		/// </summary>
		public static string BuildIDFromSymbol(ITypeSymbol typeSymbol)
			=> typeSymbol.GetFullName() + "_" + Build(typeSymbol.MetadataName + typeSymbol.ContainingAssembly.MetadataName);

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
		public static string Build(string input, HashAlgorithm algoritm = null)
		{
			using (algoritm ??= SHA256.Create())
			{
				// Convert the input string to a byte array and compute the hash.
				var data = algoritm.ComputeHash(Encoding.UTF8.GetBytes(input));

				// Create a new Stringbuilder to collect the bytes
				// and create a string.
				var builder = new StringBuilder();

				// Use the first 16 bytes of the hash
				for (int i = 0; i < Math.Min(data.Length, 16); i++)
				{
					builder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
				}

				// Return the hexadecimal string.
				return builder.ToString();
			}
		}
	}
}
