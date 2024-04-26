#nullable enable

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.UI.Tasks.ResourcesGenerator
{
	/// <summary>
	/// Generates a linker substitution file for UPRI resources.
	/// This allows the linker to trim unused localization resources.
	/// </summary>
	public class UpriSubstitutionsGeneratorTask_v0 : Task
	{
		[Required]
		public string? AssemblyName { get; set; }

		[Required]
		public ITaskItem[]? Resources { get; set; }

		[Required]
		public string? OutputFile { get; set; }

		public override bool Execute()
		{
			// Debugger.Launch();

			if (!string.IsNullOrEmpty(AssemblyName) && Resources?.Length > 0 && !string.IsNullOrEmpty(OutputFile))
			{
				var xml = new XmlDocument();

				var linkerNode = xml.CreateElement(string.Empty, "linker", string.Empty);

				xml.AppendChild(linkerNode);

				foreach (var resourceGroup in Resources.GroupBy(r => r.GetMetadata("Language").Replace('-', '_').ToLowerInvariant()))
				{
					var assemblyNode = xml.CreateElement(string.Empty, "assembly", string.Empty);
					assemblyNode.SetAttribute("fullname", AssemblyName);
					assemblyNode.SetAttribute("feature", $"UPRI_{resourceGroup.Key}");
					assemblyNode.SetAttribute("featurevalue", "false");

					linkerNode.AppendChild(assemblyNode);

					foreach (var resource in resourceGroup)
					{
						var resourceNode = xml.CreateElement(string.Empty, "resource", string.Empty);
						resourceNode.SetAttribute("name", resource.ItemSpec);
						resourceNode.SetAttribute("action", "remove");

						assemblyNode.AppendChild(resourceNode);
					}
				}

				Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));

				using var sw = new StreamWriter(OutputFile, append: false, Encoding.UTF8);

				xml.Save(sw);
			}

			return true;
		}
	}
}
