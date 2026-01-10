using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Extensions.Specialized;
using Uno.UI.DataBinding;
using Uno.UI.Helpers;
using System.Linq;
using System.Reflection;

namespace Microsoft.UI.Xaml.Navigation;

partial class PageStackEntry
{
	// Note: LEGACY, not used by MUX (_useWinUIBehavior)
	internal Page Instance { get; set; }

	/// <summary>
	/// Builds a descriptor string for the specified page type, including assembly context information if the type's
	/// assembly is loaded in a non-default context.
	/// </summary>
	/// <remarks>The descriptor can be used to uniquely identify a page type, especially in scenarios involving
	/// multiple assembly load contexts.</remarks>
	/// <param name="pageType">The type of the page for which to build the descriptor. Must have a valid assembly-qualified name.</param>
	/// <returns>A string representing the descriptor for the specified page type. If the type's assembly is loaded in a non-default
	/// assembly load context, the descriptor includes the context name.</returns>
	/// <exception cref="ArgumentException">Thrown if <paramref name="pageType"/> does not have an assembly-qualified name.</exception>
	internal static string BuildDescriptor(Type pageType)
	{
		var assemblyQualifiedName = pageType.AssemblyQualifiedName
			?? throw new ArgumentException($"Type {pageType.FullName} does not have an assembly-qualified name.", nameof(pageType));

		var alc = AssemblyLoadContext.GetLoadContext(pageType.Assembly);
		if (alc is not null && alc != AssemblyLoadContext.Default)
		{
			return $"{assemblyQualifiedName}{AlcDescriptorDelimiter}{alc.Name}";
		}

		return assemblyQualifiedName;
	}

	/// <summary>
	/// Resolves a type descriptor string to a Type instance.
	/// </summary>
	/// <param name="descriptor">The descriptor string, either an assembly-qualified name or a name with ALC suffix (##ALCName).</param>
	/// <returns>The resolved Type, or null if the type cannot be found or the ALC is not loaded.</returns>
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the type exists.")]
	internal static Type ResolveDescriptor(string descriptor)
	{
		if (!descriptor.Contains(AlcDescriptorDelimiter))
		{
			// Type.GetType returns null if the type is not found
			return Type.GetType(descriptor);
		}

		var descriptorParts = descriptor.Split(AlcDescriptorDelimiter, StringSplitOptions.None);
		if (descriptorParts.Length != 2 || string.IsNullOrWhiteSpace(descriptorParts[0]) || string.IsNullOrWhiteSpace(descriptorParts[1]))
		{
			throw new InvalidOperationException(
				$"Failed to resolve type descriptor '{descriptor}': expected format '<Type>{AlcDescriptorDelimiter}<ALCName>'.");
		}

		var typeDescriptor = descriptorParts[0];
		var alcName = descriptorParts[1];

		if (AssemblyLoadContext.All.FirstOrDefault(alc => alc.Name == alcName) is { } alc)
		{
			using (alc.EnterContextualReflection())
			{
				return ResolveTypeInAssemblyLoadContext(typeDescriptor, alc);
			}
		}

		throw new InvalidOperationException(
			$"Failed to resolve type descriptor '{descriptor}': AssemblyLoadContext '{alcName}' was not found.");
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the type exists.")]
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the type exists.")]
	private static Type ResolveTypeInAssemblyLoadContext(string assemblyQualifiedTypeName, AssemblyLoadContext alc)
	{
		var resolvedType = Type.GetType(
			assemblyQualifiedTypeName,
			assemblyName => ResolveAssemblyFromContext(assemblyName, alc),
			(assembly, typeName, ignoreCase) => assembly?.GetType(typeName, throwOnError: false, ignoreCase: ignoreCase));

		if (resolvedType != null)
		{
			return resolvedType;
		}

		resolvedType = ResolveTypeUsingAssemblyLookup(assemblyQualifiedTypeName, alc);
		if (resolvedType != null)
		{
			return resolvedType;
		}

		throw new InvalidOperationException(
			$"Failed to resolve type '{assemblyQualifiedTypeName}' within AssemblyLoadContext '{alc.Name}'.");
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
	[RequiresUnreferencedCode("Assembly.GetType(string) requires RequiresUnreferencedCode")]
	private static Type ResolveTypeUsingAssemblyLookup(string assemblyQualifiedTypeName, AssemblyLoadContext alc)
	{
		if (!TrySplitAssemblyQualifiedName(assemblyQualifiedTypeName, out var typeName, out var assemblyNameSpec))
		{
			return null;
		}

		var requestedAssembly = new AssemblyName(assemblyNameSpec);
		var assembly = ResolveAssemblyFromContext(requestedAssembly, alc);
		return assembly?.GetType(typeName, throwOnError: false, ignoreCase: false);
	}

	private static Assembly ResolveAssemblyFromContext(AssemblyName requestedAssembly, AssemblyLoadContext alc)
	{
		foreach (var loadedAssembly in alc.Assemblies)
		{
			if (AssemblyName.ReferenceMatchesDefinition(loadedAssembly.GetName(), requestedAssembly))
			{
				return loadedAssembly;
			}
		}

		return null;
	}

	private static bool TrySplitAssemblyQualifiedName(string assemblyQualifiedTypeName, out string typeName, out string assemblyName)
	{
		var bracketDepth = 0;
		for (var i = 0; i < assemblyQualifiedTypeName.Length; i++)
		{
			var current = assemblyQualifiedTypeName[i];
			switch (current)
			{
				case '[':
					bracketDepth++;
					break;
				case ']':
					if (bracketDepth > 0)
					{
						bracketDepth--;
					}
					break;
				case ',' when bracketDepth == 0:
					typeName = assemblyQualifiedTypeName.Substring(0, i).Trim();
					assemblyName = assemblyQualifiedTypeName.Substring(i + 1).Trim();
					return true;
			}
		}

		typeName = assemblyQualifiedTypeName;
		assemblyName = string.Empty;
		return false;
	}
}
