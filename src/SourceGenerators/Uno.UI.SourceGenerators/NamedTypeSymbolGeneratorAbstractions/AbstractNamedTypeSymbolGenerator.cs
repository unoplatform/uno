#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.Helpers;

#if NETFRAMEWORK
using Uno.SourceGeneration;
#endif

namespace Uno.UI.SourceGenerators
{
	public abstract class AbstractNamedTypeSymbolGenerator
#if !NETFRAMEWORK
		<TInitializationDataCollector, TExecutionDataCollector>
#endif
		: ISourceGenerator
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
				var generator = GetGenerator(context);
#if NETFRAMEWORK
				generator.Visit(context.Compilation.SourceModule);
#else
				var collector = GetExecutionDataCollector(context);
				if (context.SyntaxContextReceiver is ClassSyntaxReceiver receiver)
				{
					foreach (var symbol in receiver.NamedTypeSymbols)
					{
						if (IsCandidateSymbolInRoslynExecution(context, symbol, collector))
						{
							generator.ProcessType(symbol);
						}
					}
				}
#endif
			}
		}

		private protected abstract SymbolGenerator GetGenerator(GeneratorExecutionContext context);
#if !NETFRAMEWORK
		public abstract bool IsCandidateSymbolInRoslynInitialization(GeneratorSyntaxContext context, INamedTypeSymbol symbol, TInitializationDataCollector collector);
		public abstract bool IsCandidateSymbolInRoslynExecution(GeneratorExecutionContext context, INamedTypeSymbol symbol, TExecutionDataCollector collector);
		public abstract TInitializationDataCollector GetInitializationDataCollector(Compilation compilation);
		public abstract TExecutionDataCollector GetExecutionDataCollector(GeneratorExecutionContext context);

		private sealed class ClassSyntaxReceiver : ISyntaxContextReceiver
		{
			private readonly AbstractNamedTypeSymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> _generator;
			private TInitializationDataCollector? _collector;

			public ClassSyntaxReceiver(AbstractNamedTypeSymbolGenerator<TInitializationDataCollector, TExecutionDataCollector> generator)
			{
				_generator = generator;
			}

			public HashSet<INamedTypeSymbol> NamedTypeSymbols { get; } = new(SymbolEqualityComparer.Default);

			public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
			{
				_collector ??= _generator.GetInitializationDataCollector(context.SemanticModel.Compilation);
				if (context.Node.IsKind(SyntaxKind.ClassDeclaration))
				{
					if (context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol symbol &&
						_generator.IsCandidateSymbolInRoslynInitialization(context, symbol, _collector))
					{
						NamedTypeSymbols.Add(symbol);
					}
				}
			}
		}
#endif
	}
}
