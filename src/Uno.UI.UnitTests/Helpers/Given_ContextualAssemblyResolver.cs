#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

namespace Uno.UI.Tests.Helpers;

[TestClass]
public class Given_ContextualAssemblyResolver
{
	[TestMethod]
	public void When_NoContextualScope_ReturnsAppDomainAssemblies()
	{
		// Sanity baseline: with no EnterContextualReflection scope active, the helper
		// must return the same set of assemblies as AppDomain.GetAssemblies — this is
		// the documented fallback path and ensures pre-existing callers see no change.
		Assert.IsNull(AssemblyLoadContext.CurrentContextualReflectionContext);

		var fromAppDomain = AppDomain.CurrentDomain.GetAssemblies();
		var fromHelper = ContextualAssemblyResolver.GetRelevantAssemblies().ToArray();

		CollectionAssert.AreEquivalent(fromAppDomain, fromHelper);
	}

	[TestMethod]
	public void When_ContextualScopeOnDefaultAlc_ReturnsAppDomainAssemblies()
	{
		// EnterContextualReflection on a default-ALC assembly leaves
		// CurrentContextualReflectionContext == Default. The helper falls back to
		// AppDomain.GetAssemblies in that case (no consumer-visible change).
		using (AssemblyLoadContext.Default.EnterContextualReflection())
		{
			Assert.AreSame(AssemblyLoadContext.Default, AssemblyLoadContext.CurrentContextualReflectionContext);

			var fromAppDomain = AppDomain.CurrentDomain.GetAssemblies();
			var fromHelper = ContextualAssemblyResolver.GetRelevantAssemblies().ToArray();

			CollectionAssert.AreEquivalent(fromAppDomain, fromHelper);
		}
	}

	[TestMethod]
	public void When_ContextualScopeOnNonDefaultAlc_ReturnsContextualPlusDefault()
	{
		// Loading an assembly into a custom collectible ALC and entering its contextual
		// reflection scope should make the helper return the custom ALC's loaded
		// assembly first, followed by every default-ALC assembly. The custom assembly
		// MUST appear in the result; default-ALC assemblies (e.g. mscorlib) MUST also
		// appear — this is what XAML resolvers need so system types still resolve.
		var customAlc = new AssemblyLoadContext("Given_ContextualAssemblyResolver.custom", isCollectible: true);
		try
		{
			var loadedAsm = customAlc.LoadFromAssemblyPath(typeof(Given_ContextualAssemblyResolver).Assembly.Location);

			using (customAlc.EnterContextualReflection())
			{
				Assert.AreSame(customAlc, AssemblyLoadContext.CurrentContextualReflectionContext);

				var result = ContextualAssemblyResolver.GetRelevantAssemblies().ToArray();

				Assert.IsTrue(result.Contains(loadedAsm), "Custom ALC's loaded assembly must be present.");

				var defaultAlcAssemblies = AssemblyLoadContext.Default.Assemblies.ToArray();
				foreach (var defaultAsm in defaultAlcAssemblies)
				{
					Assert.IsTrue(result.Contains(defaultAsm), $"Default ALC assembly missing from result: {defaultAsm.GetName().Name}");
				}
			}
		}
		finally
		{
			customAlc.Unload();
		}
	}

	[TestMethod]
	public void When_ContextualScopeOnNonDefaultAlc_ExcludesSiblingAlcs()
	{
		// Two custom ALCs each load their own copy of the same assembly. Entering the
		// contextual scope of the first must NOT return the sibling ALC's copy — that
		// is exactly the stale/sibling pollution the helper is designed to avoid.
		var alc1 = new AssemblyLoadContext("Given_ContextualAssemblyResolver.alc1", isCollectible: true);
		var alc2 = new AssemblyLoadContext("Given_ContextualAssemblyResolver.alc2", isCollectible: true);
		try
		{
			var assemblyPath = typeof(Given_ContextualAssemblyResolver).Assembly.Location;
			var asm1 = alc1.LoadFromAssemblyPath(assemblyPath);
			var asm2 = alc2.LoadFromAssemblyPath(assemblyPath);

			Assert.AreNotSame(asm1, asm2, "Each ALC should hold its own assembly instance.");

			using (alc1.EnterContextualReflection())
			{
				var result = ContextualAssemblyResolver.GetRelevantAssemblies().ToArray();

				Assert.IsTrue(result.Contains(asm1), "alc1's loaded assembly must be present when alc1 is the contextual scope.");
				Assert.IsFalse(result.Contains(asm2), "alc2's loaded assembly must NOT be present when alc1 is the contextual scope.");
			}

			using (alc2.EnterContextualReflection())
			{
				var result = ContextualAssemblyResolver.GetRelevantAssemblies().ToArray();

				Assert.IsTrue(result.Contains(asm2), "alc2's loaded assembly must be present when alc2 is the contextual scope.");
				Assert.IsFalse(result.Contains(asm1), "alc1's loaded assembly must NOT be present when alc2 is the contextual scope.");
			}
		}
		finally
		{
			alc1.Unload();
			alc2.Unload();
		}
	}
}
