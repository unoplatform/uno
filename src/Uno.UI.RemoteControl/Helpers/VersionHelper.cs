using System;
using System.Linq;
using System.Reflection;

namespace Uno.UI.RemoteControl.Helpers;

internal static class VersionHelper
{
	public static string GetVersion(Type type)
		=> GetVersion(type.Assembly);

	public static string GetVersion(Assembly assembly)
	{
		if (assembly
			.GetCustomAttributesData()
			.FirstOrDefault(data => data.AttributeType.Name.Contains("AssemblyInformationalVersion", StringComparison.OrdinalIgnoreCase))
			?.ConstructorArguments
			.FirstOrDefault()
			.Value
			?.ToString() is { Length: > 0 } informationalVersion)
		{
			return informationalVersion;
		}

		if (assembly.GetName().Version is { } assemblyVersion)
		{
			return assemblyVersion.ToString();
		}

		return "--unknown--";
	}
}
