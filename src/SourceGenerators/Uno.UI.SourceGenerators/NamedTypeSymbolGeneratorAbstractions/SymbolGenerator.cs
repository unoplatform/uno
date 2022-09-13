#nullable enable

using Microsoft.CodeAnalysis;
using Uno.UI.SourceGenerators.Helpers;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators
{
	internal abstract class SymbolGenerator<TInitializationDataCollector, TExecutionDataCollector>
#if NETFRAMEWORK
		: SymbolVisitor
#endif
	{
		protected GeneratorExecutionContext Context { get; }
		protected TInitializationDataCollector InitializationDataCollector { get; }
		protected TExecutionDataCollector ExecutionDataCollector { get; }
		private readonly AbstractNamedTypeSymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> _generator;

		protected SymbolGenerator(
			GeneratorExecutionContext context,
			TInitializationDataCollector initCollector,
			TExecutionDataCollector execCollector,
			AbstractNamedTypeSymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> generator)
		{
			Context = context;
			InitializationDataCollector = initCollector;
			ExecutionDataCollector = execCollector;
			_generator = generator;
		}

		private protected abstract string GetGeneratedCode(INamedTypeSymbol typeSymbol);

		public void ProcessType(INamedTypeSymbol typeSymbol)
		{
#if NETFRAMEWORK
			if (_generator.IsCandidateSymbolInRoslynInitialization(typeSymbol, InitializationDataCollector) &&
				_generator.IsCandidateSymbolInRoslynExecution(Context, typeSymbol, ExecutionDataCollector))
#else
			if (_generator.IsCandidateSymbolInRoslynExecution(Context, typeSymbol, ExecutionDataCollector))
#endif
			{
				Context.AddSource(
					HashBuilder.BuildIDFromSymbol(typeSymbol),
					GetGeneratedCode(typeSymbol));
			}

		}

#if NETFRAMEWORK
			public override void VisitNamedType(INamedTypeSymbol type)
			{
				Context.CancellationToken.ThrowIfCancellationRequested();

				foreach (var t in type.GetTypeMembers())
				{
					VisitNamedType(t);
				}

				ProcessType(type);
			}

			public override void VisitModule(IModuleSymbol symbol)
			{
				Context.CancellationToken.ThrowIfCancellationRequested();

				VisitNamespace(symbol.GlobalNamespace);
			}

			public override void VisitNamespace(INamespaceSymbol symbol)
			{
				Context.CancellationToken.ThrowIfCancellationRequested();

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
