#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.UI.SourceGenerators.Internal;

/// <summary>
/// Flags boxing conversions for which <c>Uno.UI.Helpers.Boxes</c> already keeps a cached instance,
/// and calls that bind to a typed <c>SetValue</c> overload through an implicit numeric conversion
/// (e.g. a <c>float</c> reaching a <c>double</c> overload), which would store the wrong boxed type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class BoxingDiagnosticAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor s_descriptorBoxing = new(
		"UnoInternal0002",
		"Avoid boxing allocation",
		"Avoid boxing allocation, use 'Uno.UI.Helpers.Boxes' instead",
		"Performance",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor s_descriptorConversion = new(
		"UnoInternal0003",
		"Possibly incorrect conversion",
		"Argument is implicitly converted to '{0}' before reaching this overload, which would store the wrong boxed type",
		"Correctness",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(s_descriptorBoxing, s_descriptorConversion);

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
					conversionOperation.Syntax.Parent is not { } parent ||
					parent.IsKind(SyntaxKind.AttributeArgument))
				{
					return;
				}

				context.ReportDiagnostic(Diagnostic.Create(s_descriptorBoxing, conversionOperation.Syntax.GetLocation()));
			}, OperationKind.Conversion);

			context.RegisterOperationAction(context =>
			{
				var invocationOperation = (IInvocationOperation)context.Operation;
				if (invocationOperation.TargetMethod is { Name: "SetValue", Parameters.Length: 2 } targetMethod &&
					targetMethod.Parameters[1].Type.SpecialType != SpecialType.System_Object)
				{
					var argumentOperation = invocationOperation.Arguments[1].Value;
					while (argumentOperation is IConversionOperation conversion && conversion.IsImplicit)
					{
						argumentOperation = conversion.Operand;
					}

					if (argumentOperation.Type is { } argumentType &&
						!argumentType.Equals(targetMethod.Parameters[1].Type, SymbolEqualityComparer.Default))
					{
						context.ReportDiagnostic(Diagnostic.Create(
							s_descriptorConversion,
							invocationOperation.Arguments[1].Syntax.GetLocation(),
							targetMethod.Parameters[1].Type.ToDisplayString()));
					}
				}
			}, OperationKind.Invocation);
		});
	}

	private static bool HasSpecialBox(IConversionOperation operation, IMethodSymbol hasFlagMethod)
	{
		var operandType = operation.Operand.Type;
		if (operandType is null)
		{
			return false;
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
		else if (operandSpecialType == SpecialType.System_Double)
		{
			// Keep the values to check against synchronized with Boxes.Box(double).
			return !operation.Operand.ConstantValue.HasValue || operation.Operand.ConstantValue.Value is 0.0;
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
