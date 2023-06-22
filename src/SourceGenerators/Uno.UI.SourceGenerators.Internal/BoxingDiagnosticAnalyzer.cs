#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.UI.SourceGenerators.Internal;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class BoxingDiagnosticAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor s_descriptor = new(
		"UnoInternal0001",
		"Avoid boxing allocation",
		"Avoid boxing allocation",
		"Performance",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(s_descriptor);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

		context.RegisterCompilationStartAction(context =>
		{
			// HasFlag no longer have boxing allocations starting with .NET Core 2.1, when both operands are of the same enum type.
			// So we need to special case that in the analyzer.
			var hasFlagMethod = (IMethodSymbol)context.Compilation.GetSpecialType(SpecialType.System_Enum).GetMembers("HasFlag").Single();

			context.RegisterOperationAction(context =>
			{
				var conversionOperation = (IConversionOperation)context.Operation;
				var conversion = conversionOperation.GetConversion();
				if (!conversion.IsBoxing ||
					!HasSpecialBox(conversionOperation, hasFlagMethod) ||
					conversionOperation.Syntax.Parent.IsKind(SyntaxKind.AttributeArgument))
				{
					return;
				}

				context.ReportDiagnostic(Diagnostic.Create(s_descriptor, conversionOperation.Syntax.GetLocation()));
			}, OperationKind.Conversion);
		});
	}

	private static bool HasSpecialBox(IConversionOperation operation, IMethodSymbol hasFlagMethod)
	{
		var operandType = operation.Operand.Type;
		if (operandType is null)
		{
			return false; // TODO: Confirm when this happens.
		}

		// Boxes.DefaultBox<int?> produces a compile-time error.
		// Boxing nullable value types is rare anyway.
		if (operandType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
			!operandType.IsValueType)
		{
			return false;
		}

		if (operation.Kind == OperationKind.DefaultValue)
		{
			return true;
		}

		var operandSpecialType = operandType.SpecialType;
		if (operandSpecialType == SpecialType.System_Boolean)
		{
			return true;
		}
		else if (operandSpecialType == SpecialType.System_Int32)
		{
			// Keep the values to check against (ie, -1, 0, 1) synchronized with Boxes.Box(int).
			return !operation.Operand.ConstantValue.HasValue || operation.Operand.ConstantValue.Value is -1 or 0 or 1;
		}
		else if (operandType.Name == "RoutedEventFlag" && !IsOptimizedHasFlagCall(operation, hasFlagMethod))
		{
			return true;
		}

		return false;
	}

	private static bool IsOptimizedHasFlagCall(IConversionOperation operation, IMethodSymbol hasFlagSymbol)
	{
		if (operation.Parent is IArgumentOperation argument &&
			argument.Parent is IInvocationOperation invocation &&
			invocation.TargetMethod.Equals(hasFlagSymbol, SymbolEqualityComparer.Default) &&
			operation.Operand.Type!.Equals(invocation.Instance?.Type, SymbolEqualityComparer.Default))
		{
			return true;
		}

		return false;
	}
}
