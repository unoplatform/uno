#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;

namespace Uno.UI.SourceGenerators.Helpers
{
	public class PlatformHelper
	{
		public static bool IsValidPlatform(GeneratorExecutionContext context)
		{
			// Those two checks are now required since VS 16.9 which enables source generators by default
			// and the uno targets files are not present for uap targets.
			var isWindowsRuntimeApplicationOutput = context.Compilation.Options.OutputKind == OutputKind.WindowsRuntimeApplication;
			var isWindowsRuntimeMetadataOutput = context.Compilation.Options.OutputKind == OutputKind.WindowsRuntimeMetadata;

			return !isWindowsRuntimeMetadataOutput
				&& !isWindowsRuntimeApplicationOutput;
		}

		public static bool IsAndroid(GeneratorExecutionContext context)
			=> context.GetMSBuildPropertyValue("AndroidApplication")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

		public static bool IsIOS(GeneratorExecutionContext context)
			=> context.GetMSBuildPropertyValue("RuntimeIdentifier") is { Length: > 0 } rid
				&& rid.StartsWith("ios", StringComparison.OrdinalIgnoreCase);

		public static bool IsUnoHead(GeneratorExecutionContext context)
			=> context.GetMSBuildPropertyValue("IsUnoHead")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

		public static bool IsApplication(GeneratorExecutionContext context)
			=> IsAndroid(context) || IsUnoHead(context);
	}
}
