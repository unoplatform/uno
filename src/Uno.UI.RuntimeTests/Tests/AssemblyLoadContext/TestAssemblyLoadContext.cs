#if HAS_UNO
#nullable enable

using System;
using System.IO;
using System.Reflection;
using Uno.Foundation.Logging;

namespace Uno.UI.RuntimeTests.Tests.AssemblyLoadContext;

/// <summary>
/// Custom <see cref="System.Runtime.Loader.AssemblyLoadContext"/> used by runtime tests
/// to drive secondary-ALC scenarios. Loads project-owned assemblies from a base path
/// while letting Uno framework / Microsoft.UI / Windows / Microsoft.Extensions / SkiaSharp
/// resolve from the default ALC (shared).
/// </summary>
internal class TestAssemblyLoadContext : System.Runtime.Loader.AssemblyLoadContext
{
	private readonly string _basePath;

	public TestAssemblyLoadContext(string basePath) : base(name: "TestALC", isCollectible: true)
	{
		_basePath = basePath;
	}

	protected override Assembly? Load(AssemblyName assemblyName)
	{
		this.Log().Debug($"Searching assembly: {assemblyName}");

		var name = assemblyName.Name;

		// Let Uno assemblies be loaded from the default ALC (shared)
		if (name != null && (
			name.StartsWith("Uno.", StringComparison.OrdinalIgnoreCase) ||
			name.Equals("Uno", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("Microsoft.UI.", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("Microsoft.Extensions.", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("SkiaSharp", StringComparison.OrdinalIgnoreCase) ||
			name.StartsWith("HarfBuzzSharp", StringComparison.OrdinalIgnoreCase))
		)
		{
			this.Log().Debug($"Assembly skipped: {assemblyName}");
			return null; // Use default ALC
		}

		// Try to load from the secondary app's directory
		var assemblyPath = Path.Combine(_basePath, name + ".dll");
		if (File.Exists(assemblyPath))
		{
			this.Log().Debug($"Loading assembly from: {assemblyPath}");
			return LoadFromAssemblyPath(assemblyPath);
		}

		this.Log().Debug($"Assembly not found: {assemblyName}");

		// Fall back to default resolution
		return null;
	}
}

#endif
