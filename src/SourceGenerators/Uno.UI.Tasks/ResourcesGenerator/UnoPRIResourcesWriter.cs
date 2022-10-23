using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Uno.UI.Tasks.ResourcesGenerator
{
	internal class UnoPRIResourcesWriter
	{
		internal static void Write(string resourceMapName, string language, Dictionary<string, string> resources, string actualTargetPath, string comment)
		{
			using var file = File.OpenWrite(actualTargetPath);
			using var writer = new BinaryWriter(file, Encoding.UTF8);
			// "Magic" sequence to ensure we'll be reading a proper
			// resource file at runtime
			writer.Write(new byte[] { 0x75, 0x6E, 0x6F });
			writer.Write(3); // version

			writer.Write(resourceMapName);
			writer.Write(language);
			writer.Write(resources.Count);

			StringBuilder sb = new();

			foreach (var pair in resources)
			{
				var key = pair.Key;

				var firstDotIndex = key.IndexOf('.');
				if (firstDotIndex != -1)
				{
					sb.Clear();
					sb.Append(key);

					// Store the key as "uid/propertypath", while keeping the
					// rest of the path with dots as needed (e.g. for attached properties)
					sb[firstDotIndex] = '/';

					key = sb.ToString();
				}

				writer.Write(key);
				writer.Write(pair.Value);
			}
		}
	}
}
