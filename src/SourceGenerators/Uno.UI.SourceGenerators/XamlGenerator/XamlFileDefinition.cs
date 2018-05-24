using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
    internal class XamlFileDefinition
	{
		public XamlFileDefinition(string file)
		{
			Namespaces = new List<NamespaceDeclaration>();
			Objects = new List<XamlObjectDefinition>();
			FilePath = file;

			UniqueID = SanitizedFileName + "_" + GetMd5Hash(FilePath);
		}

		private string SanitizedFileName => Path
			.GetFileNameWithoutExtension(FilePath)
			.Replace(" ", "_")
			.Replace(".", "_");

		public List<NamespaceDeclaration> Namespaces { get; private set; }
		public List<XamlObjectDefinition> Objects { get; private set; }

		public string FilePath { get; private set; }

		public string UniqueID { get; }

		static string GetMd5Hash(string input)
		{
			using (MD5 md5Hash = MD5.Create())
			{
				// Convert the input string to a byte array and compute the hash.
				var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

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
