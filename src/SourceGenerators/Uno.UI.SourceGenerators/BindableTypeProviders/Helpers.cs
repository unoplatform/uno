using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators.BindableTypeProviders
{
	internal static class Helpers
	{
		internal static bool IsApplication(GeneratorExecutionContext context)
		{
			var isAndroidApp = context.GetMSBuildPropertyValue("AndroidApplication")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
			var isiOSApp = context.GetMSBuildPropertyValue("ProjectTypeGuidsProperty")?.Equals("{FEACFBD2-3405-455C-9665-78FE426C6842},{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var ismacOSApp = context.GetMSBuildPropertyValue("ProjectTypeGuidsProperty")?.Equals("{A3F8F2AB-B479-4A4A-A458-A89E7DC349F1},{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var isExe = context.GetMSBuildPropertyValue("OutputType")?.Equals("Exe", StringComparison.OrdinalIgnoreCase) ?? false;
			var isUnoHead = context.GetMSBuildPropertyValue("IsUnoHead")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

			return isAndroidApp
				|| (isiOSApp && isExe)
				|| (ismacOSApp && isExe)
				|| isUnoHead;
		}

	}
}
