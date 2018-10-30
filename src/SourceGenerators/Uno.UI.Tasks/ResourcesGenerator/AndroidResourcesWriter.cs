using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Uno.Extensions;

namespace Uno.UI.Tasks.ResourcesGenerator
{
	public static class AndroidResourcesWriter
	{
		public static void Write(Dictionary<string, string> resources, string path, string comment = null)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));

			var document = new XDocument(
				new XDeclaration("1.0", "utf-8", null),
				new XElement("resources", resources.Select(resource =>
					new XElement("string",
						new XAttribute("formatted", "false"), // allows special characters (%, $, etc.)
						new XAttribute("name", AndroidResourceNameEncoder.Encode(resource.Key)),
						Sanitize(resource.Value)
					)
				))
			);

			comment.Maybe(c => document.AddFirst(new XComment(c)));
			
			document.Save(path);
		}

		/// <summary>
		/// http://developer.android.com/guide/topics/resources/string-resource.html
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string Sanitize(string value)
		{
			return value
				// Character escaping
				.Replace(@"'", @"\'")
				.Replace(@"""", @"\""")

				// Character decoding
				.Replace("\n", @"\n")
				.Replace("\u00A0", @"\u00A0");
		}
	}
}
