#nullable enable

using Microsoft.CodeAnalysis;
using Uno.UI.SourceGenerators.Helpers;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators
{
	internal abstract class SymbolGenerator
#if NETFRAMEWORK
		: SymbolVisitor
#endif
	{
		protected readonly GeneratorExecutionContext _context;

		protected SymbolGenerator(GeneratorExecutionContext context)
		{
			_context = context;
		}

		private protected abstract bool IsCandidateSymbol(INamedTypeSymbol typeSymbol);
		private protected abstract string GetGeneratedCode(INamedTypeSymbol typeSymbol);

		public void ProcessType(INamedTypeSymbol typeSymbol)
		{
#if NETFRAMEWORK
			if (IsCandidateSymbol(typeSymbol))
#endif
			{
				_context.AddSource(
					HashBuilder.BuildIDFromSymbol(typeSymbol),
					GetGeneratedCode(typeSymbol));
			}

		}

#if NETFRAMEWORK
			public override void VisitNamedType(INamedTypeSymbol type)
			{
				_context.CancellationToken.ThrowIfCancellationRequested();

				foreach (var t in type.GetTypeMembers())
				{
					VisitNamedType(t);
				}

				ProcessType(type);
			}

			public override void VisitModule(IModuleSymbol symbol)
			{
				_context.CancellationToken.ThrowIfCancellationRequested();

				VisitNamespace(symbol.GlobalNamespace);
			}

			public override void VisitNamespace(INamespaceSymbol symbol)
			{
				_context.CancellationToken.ThrowIfCancellationRequested();

				foreach (var n in symbol.GetNamespaceMembers())
				{
					VisitNamespace(n);
				}

				foreach (var t in symbol.GetTypeMembers())
				{
					VisitNamedType(t);
				}
			}
#endif
	}
}
