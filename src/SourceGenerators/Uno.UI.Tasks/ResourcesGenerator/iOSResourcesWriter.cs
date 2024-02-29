using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Uno.UI.Tasks.ResourcesGenerator
{
	public static class iOSResourcesWriter
	{
		public static void Write(Dictionary<string, string> resources, string path, string? comment = null)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));

			using (var streamWriter = new StreamWriter(path, false, Encoding.UTF8))
			{
				if (comment != null)
				{
					streamWriter.WriteLine($"/* {comment} */");
				}

				resources
					.Select(resource => $"\"{resource.Key}\" = \"{Sanitize(resource.Value)}\";")
					.ToList()
					.ForEach(streamWriter.WriteLine);
			}
		}

		private static string Sanitize(string value)
		{
			return value
				// Character escaping
				.Replace(@"'", @"\'")
				.Replace(@"""", @"\""")

				// Character decoding
				.Replace("\n", @"\n")
				.Replace("\u00A0", @"\U00A0");
		}
	}
}
