#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// Returns the set of assemblies that should be searched when resolving a type by name.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <see cref="AssemblyLoadContext.CurrentContextualReflectionContext"/> is set to a
	/// non-default ALC (i.e. a caller is inside an
	/// <see cref="AssemblyLoadContext.EnterContextualReflection(System.Reflection.Assembly?)"/>
	/// scope rooted in a non-default ALC), the contextual ALC's
	/// <see cref="AssemblyLoadContext.Assemblies"/> are returned, followed by the default ALC's
	/// assemblies. This avoids picking up stale or sibling per-app ALCs that
	/// <see cref="AppDomain.GetAssemblies"/> would otherwise expose long after they have been
	/// <see cref="AssemblyLoadContext.Unload"/>ed but not yet finalised.
	/// </para>
	/// <para>
	/// When no contextual scope is set, or the scope is the default ALC, the behaviour is
	/// identical to <see cref="AppDomain.GetAssemblies"/> — there is no consumer-visible change.
	/// </para>
	/// </remarks>
	internal static class ContextualAssemblyResolver
	{
		/// <summary>
		/// Returns the assemblies relevant for type-by-name resolution at the current
		/// reflection context.
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "AppDomain.GetAssemblies is the documented fallback")]
		public static IEnumerable<Assembly> GetRelevantAssemblies()
		{
			var contextual = AssemblyLoadContext.CurrentContextualReflectionContext;
			if (contextual is not null && contextual != AssemblyLoadContext.Default)
			{
				return contextual.Assemblies.Concat(AssemblyLoadContext.Default.Assemblies);
			}

			return AppDomain.CurrentDomain.GetAssemblies();
		}
	}
}
