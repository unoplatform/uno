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

namespace Uno.UI.SourceGenerators.Helpers
{
	public static class DesignTimeHelper
	{
		public static bool IsDesignTime(GeneratorExecutionContext context)
		{
#if NETFRAMEWORK
			// Uno source generators do not support design-time generation
			return false;
#else
			var isBuildingProjectValue = context.GetMSBuildPropertyValue("BuildingProject"); // legacy projects
			var isDesignTimeBuildValue = context.GetMSBuildPropertyValue("DesignTimeBuild"); // sdk-style projects

			return string.Equals(isBuildingProjectValue, "false", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(isDesignTimeBuildValue, "true", StringComparison.OrdinalIgnoreCase);
#endif
		}
	}
}
