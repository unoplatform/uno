// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Win32.UI.XamlHost/MetadataProviderDiscovery.cs

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml.Markup;
using WUX = Microsoft.UI.Xaml;

namespace Uno.UI.XamlHost;

/// <summary>
/// MetadataProviderDiscovery is responsible for loading all metadata providers for custom UWP XAML
/// types. In this implementation, reflection is used at runtime to probe for metadata providers in
/// the working directory, allowing any type that includes metadata (compiled in to a .NET framework
/// assembly) to be used without explicit metadata handling by the application developer.  This
/// internal class will be amended or removed when additional type loading support is available.
/// </summary>
internal static class MetadataProviderDiscovery
{
	private static readonly List<Type> FilteredTypes = new List<Type>
		{
			typeof(XamlApplication),
			typeof(WUX.Markup.IXamlMetadataProvider)
		};

	internal static Func<Type, IXamlMetadataProvider> MetadataProviderFactory { get; set; }


	private static IXamlMetadataProvider[] _metadataProviders;

	/// <summary>
	/// Probes working directory for all available metadata providers
	/// </summary>
	/// <returns>List of UWP XAML metadata providers</returns>
	[RequiresUnreferencedCode("Loads assemblies from various directories")]
	internal static IEnumerable<WUX.Markup.IXamlMetadataProvider> DiscoverMetadataProviders()
	{
		if (MetadataProviderFactory is null)
		{
			throw new InvalidOperationException("MetadataProviderFactory is not set");
		}

		_metadataProviders ??= DiscoverMetadataProvidersPrivate()?.ToArray();
		return _metadataProviders;
	}

	[RequiresUnreferencedCode("Loads assemblies from various directories; see also GetAssemblies")]
	private static IEnumerable<WUX.Markup.IXamlMetadataProvider> DiscoverMetadataProvidersPrivate()
	{
		// Get all assemblies loaded in app domain and placed side-by-side from all DLL and EXE
		var loadedAssemblies = GetAssemblies();
#if NET462
		var uniqueAssemblies = new HashSet<Assembly>(loadedAssemblies, EqualityComparerFactory<Assembly>.CreateComparer(
			a => a.GetName().FullName.GetHashCode(),
			(a, b) => a.GetName().FullName.Equals(b.GetName().FullName, StringComparison.OrdinalIgnoreCase)));
#else
		var uniqueAssemblies = new HashSet<Assembly>(loadedAssemblies, EqualityComparerFactory<Assembly>.CreateComparer(
			a => a.GetName().FullName.GetHashCode(),
			(a, b) => a.GetName().FullName.Equals(b.GetName().FullName, StringComparison.OrdinalIgnoreCase)));
#endif

		// Load all types loadable from the assembly, ignoring any types that could not be resolved due to an issue in the dependency chain
		foreach (var assembly in uniqueAssemblies)
		{
			foreach (var provider in LoadTypesFromAssembly(assembly))
			{
				yield return provider;

				// TODO: Yield break is weird here, investigate in WCT issues. https://github.com/unoplatform/uno/issues/8978
				//if (typeof(WUX.Application).IsAssignableFrom(provider.GetType()))
				//{
				//	System.Diagnostics.Debug.WriteLine("Xaml application has been created");
				//	yield break;
				//}
			}
		}
	}

	[RequiresUnreferencedCode("Loads assemblies from directories")]
	private static IEnumerable<Assembly> GetAssemblies()
	{
		yield return Assembly.GetExecutingAssembly();

		// Get assemblies already loaded in the current app domain
		foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
		{
			yield return a;
		}

		// Reflection-based runtime metadata probing
		var currentDirectory = new FileInfo(typeof(MetadataProviderDiscovery).Assembly.Location).Directory;

		foreach (var assembly in GetAssemblies(currentDirectory, "*.exe"))
		{
			yield return assembly;
		}

		foreach (var assembly in GetAssemblies(currentDirectory, "*.dll"))
		{
			yield return assembly;
		}
	}

	[RequiresUnreferencedCode("When loading assemblies from a directory, 'unreferenced code' is kinda the point, no?")]
	private static IEnumerable<Assembly> GetAssemblies(DirectoryInfo folder, string fileFilter)
	{
		foreach (var file in folder.EnumerateFiles(fileFilter))
		{
			Assembly a = null;

			try
			{
				a = Assembly.LoadFrom(file.FullName);
			}
			catch (FileLoadException)
			{
				// These exceptions are expected
			}
			catch (BadImageFormatException)
			{
				// DLL is not loadable by CLR (e.g. Native)
			}

			if (a != null)
			{
				yield return a;
			}
		}
	}

	/// <summary>
	/// Loads all types from the specified assembly and caches metadata providers
	/// </summary>
	/// <param name="assembly">Target assembly to load types from</param>
	/// <returns>The set of <seealso cref="WUX.Markup.IXamlMetadataProvider"/> found</returns>
	[RequiresUnreferencedCode("LoadTypesFromAssembly() codepath involves reading assemblies on-disk")]
	private static IEnumerable<WUX.Markup.IXamlMetadataProvider> LoadTypesFromAssembly(Assembly assembly)
	{
		// Load types inside the executing assembly
		foreach (var type in GetLoadableTypes(assembly))
		{
			if (typeof(WUX.Markup.IXamlMetadataProvider).IsAssignableFrom(type) &&
				!type.IsAbstract &&
				!type.IsInterface &&
				!type.IsGenericType &&
				type != typeof(XamlApplication))
			{
				var result = MetadataProviderFactory.Invoke(type);
				if (result is not null)
				{
					yield return result;
				}
			}
		}
	}

	// Algorithm from StackOverflow answer here:
	// http://stackoverflow.com/questions/7889228/how-to-prevent-reflectiontypeloadexception-when-calling-assembly-gettypes
	[RequiresUnreferencedCode("GetLoadableTypes() codepath involves reading assemblies on-disk")]
	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		try
		{
			var asmTypes = assembly.DefinedTypes
				.Select(t => t.AsType());
			var filteredTypes = asmTypes.Where(t => !FilteredTypes.Contains(t));
			return filteredTypes;
		}
		catch (ReflectionTypeLoadException)
		{
			return Enumerable.Empty<Type>();
		}
		catch (FileLoadException)
		{
			return Enumerable.Empty<Type>();
		}
	}

	private static class EqualityComparerFactory<T>
	{
		private class MyComparer : IEqualityComparer<T>
		{
			private readonly Func<T, int> _getHashCodeFunc;
			private readonly Func<T, T, bool> _equalsFunc;

			public MyComparer(Func<T, int> getHashCodeFunc, Func<T, T, bool> equalsFunc)
			{
				_getHashCodeFunc = getHashCodeFunc;
				_equalsFunc = equalsFunc;
			}

			public bool Equals(T x, T y) => _equalsFunc(x, y);

			public int GetHashCode(T obj) => _getHashCodeFunc(obj);
		}

		public static IEqualityComparer<T> CreateComparer(Func<T, int> getHashCodeFunc, Func<T, T, bool> equalsFunc)
		{
			if (getHashCodeFunc == null)
			{
				throw new ArgumentNullException(nameof(getHashCodeFunc));
			}

			if (equalsFunc == null)
			{
				throw new ArgumentNullException(nameof(equalsFunc));
			}

			return new MyComparer(getHashCodeFunc, equalsFunc);
		}
	}
}
