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

		public static string Build(string input, HashAlgorithm algoritm = null)
		{
			using (algoritm = algoritm ?? MD5.Create())
			{
				// Convert the input string to a byte array and compute the hash.
				var data = algoritm.ComputeHash(Encoding.UTF8.GetBytes(input));

				// Create a new Stringbuilder to collect the bytes
				// and create a string.
				var sBuilder = new StringBuilder();

				// Loop through each byte of the hashed data 
				// and format each one as a hexadecimal string.
				for (int i = 0; i < data.Length; i++)
				{
					sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
				}

				// Return the hexadecimal string.
				return sBuilder.ToString();
			}
		}
	}
}
