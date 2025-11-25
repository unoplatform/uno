using System;
using System.Reflection;
using SystemVersion = global::System.Version;

namespace Windows.ApplicationModel;

public sealed partial class PackageId
{
	public string Name { get; internal set; } = "";

	public PackageVersion Version { get; internal set; } = GetEntryAssemblyVersion();

	public string Publisher { get; internal set; } = "";

	private static PackageVersion GetEntryAssemblyVersion()
	{
		var assembly = Assembly.GetEntryAssembly();
		if (assembly is not null)
		{
			// Try to get the AssemblyInformationalVersionAttribute first
			// This corresponds to ApplicationDisplayVersion in MSBuild
			var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
			if (informationalVersionAttribute is not null)
			{
				var versionString = informationalVersionAttribute.InformationalVersion;
				// AssemblyInformationalVersion may contain additional metadata (e.g., "+commitHash")
				// Extract only the version part
				var plusIndex = versionString.IndexOf('+');
				if (plusIndex >= 0)
				{
					versionString = versionString.Substring(0, plusIndex);
				}

				if (SystemVersion.TryParse(versionString, out var version))
				{
					return new PackageVersion(version);
				}
			}

			// Fallback to AssemblyVersion
			var assemblyVersion = assembly.GetName().Version;
			if (assemblyVersion is not null)
			{
				return new PackageVersion(assemblyVersion);
			}
		}

		// Return default version if nothing is found
		return default;
	}
}
