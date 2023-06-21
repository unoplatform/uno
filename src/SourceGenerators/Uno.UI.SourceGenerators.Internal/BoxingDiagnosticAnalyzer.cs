#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
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
		// TODO: Report even in generated code.
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze /*| GeneratedCodeAnalysisFlags.ReportDiagnostics*/);
		context.RegisterOperationAction(context =>
		{
			var conversionOperation = (IConversionOperation)context.Operation;
			var conversion = conversionOperation.GetConversion();
			if (!conversion.IsBoxing ||
				!HasSpecialBox(conversionOperation) ||
				conversionOperation.Syntax.Parent.IsKind(SyntaxKind.AttributeArgument))
			{
				return;
			}

			context.ReportDiagnostic(Diagnostic.Create(s_descriptor, conversionOperation.Syntax.GetLocation()));
		}, OperationKind.Conversion);
	}

	private static bool HasSpecialBox(IConversionOperation operation)
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

		return false;
	}
}
