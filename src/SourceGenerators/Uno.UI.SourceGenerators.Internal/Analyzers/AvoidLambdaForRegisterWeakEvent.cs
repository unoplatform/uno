using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.UI.SourceGenerators.Internal.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AvoidLambdaForRegisterWeakEvent : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor _descriptor = new(
		"UnoInternal0002",
		"Don't use lambdas with RegisterWeakXYZ methods",
		"Don't use lambdas with RegisterWeakXYZ methods",
		"Correctness",
		DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(_descriptor);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.RegisterOperationAction(context =>
		{
			var operation = (IInvocationOperation)context.Operation;
			if (operation.TargetMethod.Name.StartsWith("RegisterWeak", StringComparison.Ordinal) &&
				operation.Arguments.Length == 1 &&
				operation.Arguments[0].Value is IDelegateCreationOperation delegateOperation &&
				delegateOperation.Target.Kind == OperationKind.AnonymousFunction)
			{
				context.ReportDiagnostic(Diagnostic.Create(_descriptor, operation.Syntax.GetLocation()));
			}
		}, OperationKind.Invocation);
	}
}
