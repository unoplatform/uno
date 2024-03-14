#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Uno.UI.SourceGenerators.Internal.Extensions;

namespace Uno.UI.SourceGenerators.Internal.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AvoidStrongEventSubscription : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor _descriptor = new(
		"UnoInternal0003",
		"Use weak event",
		"Use weak event",
		"Correctness",
		DiagnosticSeverity.Info,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(_descriptor);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.RegisterCompilationStartAction(context =>
		{
			var compilation = context.Compilation;
			var uiElementSymbol = compilation.GetTypeByMetadataName("Microsoft.UI.Xaml.UIElement");
			if (uiElementSymbol is null)
			{
				return;
			}

			context.RegisterOperationAction(context =>
			{
				var operation = (IEventAssignmentOperation)context.Operation;
				var eventReference = (IEventReferenceOperation)operation.EventReference;

				bool isStaticEvent = eventReference.Event.IsStatic;
				if (isStaticEvent)
				{
					return;
				}

				var instance = ((IEventReferenceOperation)operation.EventReference).Instance.WalkDownConversion();
				if (instance is IInvocationOperation)
				{
					// TODO: Can/should we do something here?
					return;
				}


				var instanceSymbol = GetSymbolFromOperation(instance);
				if (!IsUIElement(instance, uiElementSymbol))
				{
					return;
				}

				var handlerValue = operation.HandlerValue;
				if (handlerValue is IDelegateCreationOperation delegateCreationOperation)
				{
					handlerValue = delegateCreationOperation.Target;
				}

				if (handlerValue is IMemberReferenceOperation memberReferenceOperation)
				{
					if (memberReferenceOperation.Instance is { } capturedInstance)
					{
						capturedInstance = capturedInstance.WalkDownConversion();
						if (!IsUIElement(capturedInstance, uiElementSymbol))
						{
							return;
						}

						var capturedSymbol = GetSymbolFromOperation(capturedInstance);
						if (!SymbolEqualityComparer.Default.Equals(instanceSymbol, capturedSymbol))
						{
							context.ReportDiagnostic(Diagnostic.Create(_descriptor, operation.Syntax.GetLocation()));
						}
					}
				}
				else if (handlerValue is IAnonymousFunctionOperation anonymousFunctionOperation)
				{
					var dataflow = CreateDataFlowAnalysis(anonymousFunctionOperation);
					foreach (var capturedSymbol in dataflow.CapturedInside)
					{
						var capturedSymbolType = GetTypeFromSymbol(capturedSymbol);
						if (!IsUIElement(capturedSymbolType, uiElementSymbol))
						{
							continue;
						}

						if (capturedSymbol is IParameterSymbol { IsThis: true } && instanceSymbol is null)
						{
							// This is the case for capturing "this", it's not related to extension methods.
							continue;
						}

						if (!SymbolEqualityComparer.Default.Equals(instanceSymbol, capturedSymbol))
						{
							context.ReportDiagnostic(Diagnostic.Create(_descriptor, operation.Syntax.GetLocation()));
							break;
						}
					}

				}
				else if (handlerValue is ILocalReferenceOperation or IParameterReferenceOperation)
				{
					// Can't handle easily.
				}
				else
				{
					// Handle in future if we hit this.
					throw new Exception($"Unexpected operation: {operation.HandlerValue.Kind}");
				}
			}, OperationKind.EventAssignment);
		});
	}

	private static bool IsUIElement(IOperation operation, INamedTypeSymbol uiElement)
		=> IsUIElement(operation.Type, uiElement);

	private static bool IsUIElement(ITypeSymbol? type, INamedTypeSymbol uiElement)
		=> type is INamedTypeSymbol instanceType && instanceType.Is(uiElement);

	private static DataFlowAnalysis CreateDataFlowAnalysis(IAnonymousFunctionOperation anonymousFunctionOperation)
	{
		var syntax = anonymousFunctionOperation.Syntax;
		var model = anonymousFunctionOperation.SemanticModel!;
		if (syntax is SimpleLambdaExpressionSyntax simpleLambdaExpressionSyntax)
		{
			return model.AnalyzeDataFlow(simpleLambdaExpressionSyntax);
		}
		else if (syntax is ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpressionSyntax)
		{
			return model.AnalyzeDataFlow(parenthesizedLambdaExpressionSyntax);
		}
		else if (syntax is AnonymousMethodExpressionSyntax anonymousMethodExpressionSyntax)
		{
			var childNodes = anonymousMethodExpressionSyntax.Block.ChildNodes();
			return model.AnalyzeDataFlow(childNodes.First(), childNodes.Last());
		}

		throw new Exception($"Can't handle syntax {syntax}");
	}

	private static ISymbol? GetSymbolFromOperation(IOperation operation)
	{
		return operation switch
		{
			IFieldReferenceOperation fieldReferenceOperation => fieldReferenceOperation.Field,
			ILocalReferenceOperation localReferenceOperation => localReferenceOperation.Local,
			IParameterReferenceOperation parameterReferenceOperation => parameterReferenceOperation.Parameter,
			IPropertyReferenceOperation propertyReferenceOperation => propertyReferenceOperation.Property,
			IMemberReferenceOperation memberReferenceOperation => memberReferenceOperation.Member,
			IInstanceReferenceOperation instanceReferenceOperation => null, // We use null to indicate that `this` is being captured.
			_ => throw new Exception($"Can't get symbol from operation {operation.Kind}"),
		};
	}

	private static ITypeSymbol GetTypeFromSymbol(ISymbol symbol)
	{
		return symbol switch
		{
			ILocalSymbol localSymbol => localSymbol.Type,
			IParameterSymbol parameterSymbol => parameterSymbol.Type,
			_ => throw new Exception($"Cannot get type from symbol {symbol.Kind}"),
		};
	}
}
