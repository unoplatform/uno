using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.BindableTypeProviders
{
	internal static class LinkerHintsHelpers
	{
		internal static string GetPropertyAvailableName(string name)
			=> "Is_" + name
				.Replace("`", "_")
				.Replace("<", "_")
				.Replace(">", "_")
				.Replace("+", "_")
				.Replace("[", "_")
				.Replace("]", "_")
				.Replace(".", "_")
				.Replace(",", "_")
			+ "_Available";

		internal static string GetLinkerHintsClassName(string defaultNamespace)
			=> $"{defaultNamespace}.__LinkerHints";
		internal static string GetLinkerHintsClassName()
			=> $"__LinkerHints";
	}
}
