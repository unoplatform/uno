using System.Collections;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Uno.UI.Tasks.Tests;

/// <summary>
/// Builds a NuGet-cache-shaped tree of Cecil-authored assemblies, so the runtime asset tasks can be driven
/// without packing or restoring anything.
/// </summary>
internal sealed class PackageCacheFixture : IDisposable
{
	private const string UnoUIAssemblyName = "Uno.UI";

	public PackageCacheFixture(string name)
	{
		Root = Path.Combine(Path.GetTempPath(), "Uno.UI.Tasks.Tests", name, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Root);
	}

	public string Root { get; }

	public string NuGetPackageRoot => Root + Path.DirectorySeparatorChar;

	/// <summary>
	/// Writes lib/&lt;platformTargetFramework&gt; and lib/&lt;neutralTargetFramework&gt; assets for a package.
	/// </summary>
	/// <returns>The path of the platform-specific asset.</returns>
	public string AddPackage(
		string packageId,
		string version,
		string platformTargetFramework,
		string neutralTargetFramework,
		string[] unoUITypeReferences,
		bool referencesUnoUI = true)
	{
		var platformAsset = Path.Combine(Root, packageId, version, "lib", platformTargetFramework, packageId + ".dll");
		var neutralAsset = Path.Combine(Root, packageId, version, "lib", neutralTargetFramework, packageId + ".dll");

		WriteAssembly(platformAsset, packageId, unoUITypeReferences, referencesUnoUI, []);
		WriteAssembly(neutralAsset, packageId, [], referencesUnoUI, []);

		return platformAsset;
	}

	public string AddAssembly(
		string packageId,
		string version,
		string targetFramework,
		string[] unoUITypeReferences,
		(string Key, string Value)[] assemblyMetadata)
	{
		var path = Path.Combine(Root, packageId, version, "lib", targetFramework, packageId + ".dll");
		WriteAssembly(path, packageId, unoUITypeReferences, referencesUnoUI: true, assemblyMetadata);
		return path;
	}

	/// <summary>
	/// An assembly standing in for Uno.UI, defining exactly the given types.
	/// </summary>
	public string AddUnoUI(params string[] typeNames)
	{
		var path = Path.Combine(Root, "unoui", UnoUIAssemblyName + ".dll");
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		var assembly = AssemblyDefinition.CreateAssembly(
			new AssemblyNameDefinition(UnoUIAssemblyName, new Version(255, 255, 255, 255)), UnoUIAssemblyName, ModuleKind.Dll);

		foreach (var typeName in typeNames)
		{
			var (@namespace, name) = SplitTypeName(typeName);
			assembly.MainModule.Types.Add(new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Class)
			{
				BaseType = assembly.MainModule.TypeSystem.Object,
			});
		}

		assembly.Write(path);
		return path;
	}

	/// <summary>
	/// Builds a package shaped like Uno.WinRT: a platform-neutral compile surface under lib/&lt;neutral&gt;, a
	/// per-platform implementation under lib/&lt;platform&gt;, and uno-runtime/&lt;tfm&gt;/&lt;rid&gt; folders.
	/// </summary>
	/// <returns>The PackageBasePath the package's own props would pass, i.e. its buildTransitive folder.</returns>
	public string AddRuntimeEnabledPackage(
		string packageId,
		string version,
		string neutralTargetFramework,
		string platformTargetFramework,
		string[] winRTAssemblies,
		string[] otherAssemblies,
		string[] runtimeIdentifiers)
	{
		var packageRoot = Path.Combine(Root, packageId, version);

		foreach (var assembly in winRTAssemblies.Concat(otherAssemblies))
		{
			// The union compile surface every consumer binds against.
			WriteAssembly(Path.Combine(packageRoot, "lib", neutralTargetFramework, assembly + ".dll"), assembly, [], false, []);

			foreach (var runtimeIdentifier in runtimeIdentifiers)
			{
				WriteAssembly(
					Path.Combine(packageRoot, "uno-runtime", neutralTargetFramework, runtimeIdentifier, assembly + ".dll"),
					assembly, [], false, []);
			}
		}

		// Only the WinRT assemblies carry a per-platform implementation.
		foreach (var assembly in winRTAssemblies)
		{
			WriteAssembly(Path.Combine(packageRoot, "lib", platformTargetFramework, assembly + ".dll"), assembly, [], false, []);
		}

		var packageBasePath = Path.Combine(packageRoot, "buildTransitive");
		Directory.CreateDirectory(packageBasePath);
		return packageBasePath;
	}

	private static void WriteAssembly(
		string path,
		string name,
		string[] unoUITypeReferences,
		bool referencesUnoUI,
		(string Key, string Value)[] assemblyMetadata)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		var assembly = AssemblyDefinition.CreateAssembly(
			new AssemblyNameDefinition(name, new Version(1, 0, 0, 0)), name, ModuleKind.Dll);
		var module = assembly.MainModule;

		var unoUI = new AssemblyNameReference(UnoUIAssemblyName, new Version(255, 255, 255, 255));
		if (referencesUnoUI)
		{
			module.AssemblyReferences.Add(unoUI);
		}

		var probe = new TypeDefinition(name, "Probe", TypeAttributes.Public | TypeAttributes.Class)
		{
			BaseType = module.TypeSystem.Object,
		};
		module.Types.Add(probe);

		// A field is the cheapest way to force the type into the TypeRef table.
		for (var i = 0; i < unoUITypeReferences.Length; i++)
		{
			var (@namespace, typeName) = SplitTypeName(unoUITypeReferences[i]);
			var typeReference = new TypeReference(@namespace, typeName, module, unoUI);
			module.ImportReference(typeReference);
			probe.Fields.Add(new FieldDefinition("_field" + i, FieldAttributes.Public, typeReference));
		}

		AddAssemblyMetadata(assembly, assemblyMetadata);

		assembly.Write(path);
	}

	private static void AddAssemblyMetadata(AssemblyDefinition assembly, (string Key, string Value)[] metadata)
	{
		if (metadata.Length == 0)
		{
			return;
		}

		var module = assembly.MainModule;
		var stringType = module.TypeSystem.String;
		var attributeType = new TypeReference("System.Reflection", "AssemblyMetadataAttribute", module, module.TypeSystem.CoreLibrary);
		var constructor = new MethodReference(".ctor", module.TypeSystem.Void, attributeType) { HasThis = true };
		constructor.Parameters.Add(new ParameterDefinition(stringType));
		constructor.Parameters.Add(new ParameterDefinition(stringType));

		foreach (var (key, value) in metadata)
		{
			var attribute = new CustomAttribute(module.ImportReference(constructor));
			attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringType, key));
			attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringType, value));
			assembly.CustomAttributes.Add(attribute);
		}
	}

	private static (string Namespace, string Name) SplitTypeName(string typeName)
	{
		var lastDot = typeName.LastIndexOf('.');
		return lastDot < 0 ? ("", typeName) : (typeName.Substring(0, lastDot), typeName.Substring(lastDot + 1));
	}

	public static ITaskItem Item(string itemSpec, params (string Key, string Value)[] metadata)
	{
		var item = new TaskItem(itemSpec);
		foreach (var (key, value) in metadata)
		{
			item.SetMetadata(key, value);
		}

		return item;
	}

	public void Dispose()
	{
		try
		{
			Directory.Delete(Root, recursive: true);
		}
		catch (IOException)
		{
			// Leaving a temp folder behind must never fail a test run.
		}
	}
}

/// <summary>
/// Captures the diagnostics a task raises.
/// </summary>
internal sealed class RecordingBuildEngine : IBuildEngine
{
	public List<BuildWarningEventArgs> Warnings { get; } = [];

	public List<BuildErrorEventArgs> Errors { get; } = [];

	public bool ContinueOnError => false;

	public int LineNumberOfTaskNode => 0;

	public int ColumnNumberOfTaskNode => 0;

	public string ProjectFileOfTaskNode => "Uno.UI.Tasks.Tests.proj";

	public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;

	public void LogCustomEvent(CustomBuildEventArgs e) { }

	public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);

	public void LogMessageEvent(BuildMessageEventArgs e) { }

	public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
}
