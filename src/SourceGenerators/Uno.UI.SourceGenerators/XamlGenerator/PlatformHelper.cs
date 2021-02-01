
#nullable enable
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

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	public class PlatformHelper
	{
		public static bool IsValidPlatform(GeneratorExecutionContext context)
		{
			var evaluatedValue = context.GetMSBuildPropertyValue("TargetPlatformIdentifier");
			var useWPF = context.GetMSBuildPropertyValue("UseWPF");
			var projectTypeGuids = context.GetMSBuildPropertyValue("ProjectTypeGuidsProperty");

			var isUAP = evaluatedValue?.Equals("UAP", StringComparison.OrdinalIgnoreCase) ?? false;
			var isNetCoreWPF = useWPF?.Equals("True", StringComparison.OrdinalIgnoreCase) ?? false;
			var isNetCoreDesktop = projectTypeGuids == "{60dc8134-eba5-43b8-bcc9-bb4bc16c2548};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

			return !isUAP && !isNetCoreWPF && !isNetCoreDesktop;
		}
	}
}
