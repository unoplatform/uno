#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
	internal static class CompilationExtensions
	{
		private static ConditionalWeakTable<Compilation, IReadOnlyDictionary<string, INamedTypeSymbol[]>> _table
			= new ConditionalWeakTable<Compilation, IReadOnlyDictionary<string, INamedTypeSymbol[]>>();

		/// <summary>
		/// Provides a dictionary to perform ISymbol.Name lookups from all types reachable in a <see cref="Compilation"/>.
		/// </summary>
		/// <param name="compilation"></param>
		public static IReadOnlyDictionary<string, INamedTypeSymbol[]> GetSymbolNameLookup(this Compilation compilation)
			=> _table.GetValue(compilation, BuildLookup);

		private static IReadOnlyDictionary<string, INamedTypeSymbol[]> BuildLookup(Compilation compilation)
		{
			var refs = from metadataReference in compilation.References
					   let assembly = compilation.GetAssemblyOrModuleSymbol(metadataReference) as IAssemblySymbol
					   where assembly != null
					   from type in assembly.GlobalNamespace.GetNamespaceTypes()
					   group type by type.Name into names
					   select new { names.Key, names };

			return refs.ToDictionary(k => k.Key, p => p.names.AsEnumerable().ToArray());
		}
	}
}
