#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Uno.UI.SourceGenerators.BindableTypeProviders;

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

		public bool EnableXamlTrimmingIntegration { get; set; }

		[Required]
		public string? OutputFile { get; set; }

		private static Regex _suffixRegex = new(@"\.Strings\..+\.upri", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		public override bool Execute()
		{
			// Debugger.Launch();

			if (!string.IsNullOrEmpty(AssemblyName) && Resources?.Length > 0 && !string.IsNullOrEmpty(OutputFile))
			{
				var xml = new XmlDocument();

				var linkerNode = xml.CreateElement(string.Empty, "linker", string.Empty);

				xml.AppendChild(linkerNode);

				if (EnableXamlTrimmingIntegration)
				{
					var groups =
						Resources
							.Where(r => !r.ItemSpec.StartsWith("Resources", StringComparison.Ordinal) &&
										!r.ItemSpec.StartsWith("Microsoft." + /* UWP don't rename */ "UI.Xaml.Controls.WinUIResources", StringComparison.Ordinal))
							.Select(r => (Key: RewriteKey(r.ItemSpec), Value: r.ItemSpec))
							.ToLookup(kv => kv.Key, kv => kv.Value);

					foreach (var group in groups)
					{
						var assemblyNode = xml.CreateElement(string.Empty, "assembly", string.Empty);
						assemblyNode.SetAttribute("fullname", AssemblyName);
						assemblyNode.SetAttribute("feature", group.Key);
						assemblyNode.SetAttribute("featurevalue", "false");

						linkerNode.AppendChild(assemblyNode);

						foreach (var resource in group)
						{
							var resourceNode = xml.CreateElement(string.Empty, "resource", string.Empty);
							resourceNode.SetAttribute("name", resource);
							resourceNode.SetAttribute("action", "remove");

							assemblyNode.AppendChild(resourceNode);
						}
					}
				}

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

		private string RewriteKey(string key)
		{
			if (key.StartsWith("UI.Xaml.DragDrop.", StringComparison.Ordinal))
			{
				key = key.Replace("UI.Xaml.DragDrop", "Microsoft.UI.Xaml.DragView");
			}
			else if (key.StartsWith("UI.Xaml.", StringComparison.Ordinal))
			{
				key = key.Replace("UI.Xaml", "Microsoft.UI.Xaml");
			}

			return LinkerHintsHelpers.GetPropertyAvailableName(_suffixRegex.Replace(key, string.Empty));
		}
	}
}
