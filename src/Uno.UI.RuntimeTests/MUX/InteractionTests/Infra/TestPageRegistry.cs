#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MUXControlsTestApp;

namespace Microsoft.UI.Xaml.Tests.MUXControls.InteractionTests.Infra;

/// <summary>
/// Resolves upstream TestSetupHelper names ("Expander Tests", "Expander-Axe") to the SamplesApp pages
/// carrying the matching <see cref="TopLevelTestPageAttribute"/> or <see cref="AxeScanTestPageAttribute"/>.
/// </summary>
internal static class TestPageRegistry
{
	private const string TestsSuffix = " Tests";

	private static Dictionary<string, Type>? _pages;

	public static bool TryGetPageType(string testName, out Type pageType)
		=> (_pages ??= Discover()).TryGetValue(testName, out pageType!);

	private static Dictionary<string, Type> Discover()
	{
		var pages = new Dictionary<string, Type>(StringComparer.Ordinal);
		foreach (var type in AppDomain.CurrentDomain.GetAssemblies().Where(IsCandidate).SelectMany(GetTypes))
		{
			if (type.GetCustomAttribute<TopLevelTestPageAttribute>() is { } topLevel)
			{
				pages[topLevel.Name + TestsSuffix] = type;
			}

			if (type.GetCustomAttribute<AxeScanTestPageAttribute>() is { } axe)
			{
				pages[axe.Name] = type;
			}
		}

		return pages;
	}

	// Test pages live in assemblies that reference the runtime tests (SamplesApp).
	private static bool IsCandidate(Assembly assembly)
	{
		if (assembly.IsDynamic)
		{
			return false;
		}

		var self = typeof(TopLevelTestPageAttribute).Assembly.GetName().Name;
		return assembly.GetName().Name == self || assembly.GetReferencedAssemblies().Any(reference => reference.Name == self);
	}

	private static IEnumerable<Type> GetTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException e)
		{
			return e.Types.Where(t => t is not null)!;
		}
	}
}
