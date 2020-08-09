#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.SourceGeneration;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	public class PlatformHelper
	{
		public static bool IsValidPlatform(SourceGeneratorContext context)
		{
			var projectInstance = context.GetProjectInstance();

			var evaluatedValue = projectInstance
				.GetProperty("TargetPlatformIdentifier")
				?.EvaluatedValue ?? "";

			var isUAP = evaluatedValue.Equals("UAP", StringComparison.OrdinalIgnoreCase);
			var isNetCoreWPF = !string.IsNullOrEmpty(projectInstance.GetProperty("UseWPF")?.EvaluatedValue);
			var isNetCoreDesktop = projectInstance
				.GetProperty("ProjectTypeGuids")
				?.EvaluatedValue == "{60dc8134-eba5-43b8-bcc9-bb4bc16c2548};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

			return !isUAP && !isNetCoreWPF && !isNetCoreDesktop;
		}

		public static bool IsReferenceUnoRuntimeIdentifier(SourceGeneratorContext context)
		{
			return context
				.GetProjectInstance()
				.GetProperty("UnoRuntimeIdentifier")?.EvaluatedValue?.Equals("Reference", StringComparison.OrdinalIgnoreCase) ?? false;
		}

	}
}
