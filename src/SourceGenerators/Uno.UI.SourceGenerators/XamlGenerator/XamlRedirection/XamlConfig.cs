using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal static class XamlConfig
	{
		static XamlConfig()
		{
			// For now, the Xaml reader from Uno is used only in the VS4Mac
			// context, only for building apps. This may become a default when the 
			// parser is fully compliant.
			IsUnoXaml = IsMono;
		}

		/// <summary>
		/// Returns true if the current assembly is running under Mono
		/// </summary>
		public static bool IsMono => Type.GetType("Mono.Runtime") != null;

		/// <summary>
		/// Determines if the Uno.Xaml assembly should be used.
		/// </summary>
		public static bool IsUnoXaml { get; set; }
	}
}
