#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.Helpers;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators
{
	public abstract class AbstractNamedTypeSymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> : ISourceGenerator
		where TInitializationDataCollector : struct
		where TExecutionDataCollector : struct
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			DependenciesInitializer.Init();
#if !NETFRAMEWORK
			context.RegisterForSyntaxNotifications(() => new ClassSyntaxReceiver(this));
#endif
		}

		public void Execute(GeneratorExecutionContext context)
		{
			if (!DesignTimeHelper.IsDesignTime(context) && PlatformHelper.IsValidPlatform(context))
			{
#if NETFRAMEWORK
				var initCollector = GetInitializationDataCollector(context.Compilation);
				var execCollector = GetExecutionDataCollector(context);
				var generator = GetGenerator(context, initCollector, execCollector);
				generator.Visit(context.Compilation.SourceModule);
#else
				if (context.SyntaxContextReceiver is ClassSyntaxReceiver receiver && receiver.Collector is TInitializationDataCollector initCollector)
				{
					var execCollector = GetExecutionDataCollector(context);
					var generator = GetGenerator(context, initCollector, execCollector);
					foreach (var symbol in receiver.NamedTypeSymbols)
					{
						generator.ProcessType(symbol);
					}
				}
#endif
			}
		}

		private protected abstract SymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> GetGenerator(GeneratorExecutionContext context, TInitializationDataCollector initializationCollector, TExecutionDataCollector executionCollector);
		public abstract bool IsCandidateSymbolInRoslynInitialization(INamedTypeSymbol symbol, TInitializationDataCollector collector);
		public abstract bool IsCandidateSymbolInRoslynExecution(GeneratorExecutionContext context, INamedTypeSymbol symbol, TExecutionDataCollector collector);
		public abstract TInitializationDataCollector GetInitializationDataCollector(Compilation compilation);
		public abstract TExecutionDataCollector GetExecutionDataCollector(GeneratorExecutionContext context);
#if !NETFRAMEWORK
		private sealed class ClassSyntaxReceiver : ISyntaxContextReceiver
		{
			private readonly AbstractNamedTypeSymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> _generator;
			private TInitializationDataCollector? _collector;

			public ClassSyntaxReceiver(AbstractNamedTypeSymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> generator)
			{
				_generator = generator;
			}

			public HashSet<INamedTypeSymbol> NamedTypeSymbols { get; } = new(SymbolEqualityComparer.Default);
			public TInitializationDataCollector? Collector { get; }

			public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
			{
				_collector ??= _generator.GetInitializationDataCollector(context.SemanticModel.Compilation);
				if (context.Node.IsKind(SyntaxKind.ClassDeclaration))
				{
					if (context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol symbol &&
						_generator.IsCandidateSymbolInRoslynInitialization(symbol, _collector.Value))
					{
						NamedTypeSymbols.Add(symbol);
					}
				}
			}
		}
#endif
	}
}
