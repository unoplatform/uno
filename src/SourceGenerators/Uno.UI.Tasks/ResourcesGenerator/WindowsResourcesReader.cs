#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Uno.UI.Tasks.ResourcesGenerator
{
	public static class WindowsResourcesReader
	{
		public static Dictionary<string, string> Read(string filePath)
		{
			return XDocument
				.Load(filePath)
				.Root
				.Elements("data")
				.Select(element => new KeyValuePair<string, string>(
					element.Attribute("name").Value,
					element.Element("value").Value
				))
				.Distinct()
				.ToDictionary(x => x.Key, x => x.Value);
		}
	}
}
