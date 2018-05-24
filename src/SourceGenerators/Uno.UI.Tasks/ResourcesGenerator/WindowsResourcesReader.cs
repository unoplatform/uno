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
				.ToDictionary(
					element => element.Attribute("name").Value,
					element => element.Element("value").Value
			);
		}
	}
}
