using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Uno.UI.Tasks.ResourcesGenerator
{
	internal class UnoPRIResourcesWriter
	{
		internal static void Write(string language, Dictionary<string, string> resources, string actualTargetPath, string comment)
		{
			using (var file = File.OpenWrite(actualTargetPath))
			{
				using (var writer = new BinaryWriter(file, Encoding.UTF8))
				{
					writer.Write(new byte[] { 0x75, 0x6E, 0x6F }); // Magic
					writer.Write(1); // version

					writer.Write(language);
					writer.Write(resources.Count);

					foreach (var pair in resources)
					{
						writer.Write(pair.Key);
						writer.Write(pair.Value);
					}
				}
			}
		}
	}
}
