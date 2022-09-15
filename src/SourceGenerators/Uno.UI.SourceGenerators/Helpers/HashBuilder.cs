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
		private static uint[] _Lookup32 = Enumerable.Range(0, 256).Select(i =>
		{
			string s = i.ToString("x2");
			return ((uint)s[0]) + ((uint)s[1] << 16);
		}).ToArray();

		private static readonly unsafe uint* _lookup32UnsafeP = (uint*)GCHandle.Alloc(_Lookup32, GCHandleType.Pinned).AddrOfPinnedObject();

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
			using (var algoritm = SHA256.Create())
			{
				// Convert the input string to a byte array and compute the hash.
				var data = algoritm.ComputeHash(Encoding.UTF8.GetBytes(input));

				var min = Math.Min(data.Length, 16);
				var result = new string((char)0, min * 2);
				// efficient implementation to convert a byte array to hex string.
				// See https://github.com/patridge/PerformanceStubs/blob/574c79441c18220f7841bdd1c25db0f6d7752876/README.markdown#converting-a-byte-array-to-a-hexadecimal-string
				unsafe
				{
					var lookupP = _lookup32UnsafeP;
					fixed (byte* bytesP = data)
					fixed (char* resultP = result)
					{
						uint* resultP2 = (uint*)resultP;
						for (int i = 0; i < min; i++)
						{
							resultP2[i] = lookupP[bytesP[i]];
						}
					}
				}

				return result;
			}
		}
	}
}
