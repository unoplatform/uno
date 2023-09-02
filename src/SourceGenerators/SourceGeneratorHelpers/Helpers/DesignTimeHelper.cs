#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Uno.Roslyn;

namespace Uno.UI.SourceGenerators.Helpers
{
	public static class DesignTimeHelper
	{
		public static bool IsDesignTime(GeneratorExecutionContext context)
		{
			var isBuildingProjectValue = context.GetMSBuildPropertyValue("BuildingProject"); // legacy projects
			var isDesignTimeBuildValue = context.GetMSBuildPropertyValue("DesignTimeBuild"); // sdk-style projects

			return string.Equals(isBuildingProjectValue, "false", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(isDesignTimeBuildValue, "true", StringComparison.OrdinalIgnoreCase);
		}
	}
}
