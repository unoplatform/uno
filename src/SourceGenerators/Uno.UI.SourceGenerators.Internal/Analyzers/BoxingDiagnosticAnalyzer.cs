#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.UI.SourceGenerators.Internal;

/// <summary>
/// Flags boxing conversions for which <c>Uno.UI.Helpers.Boxes</c> already keeps a cached instance,
/// and calls that bind to a typed <c>SetValue</c> overload through an implicit numeric conversion
/// (e.g. a <c>float</c> reaching a <c>double</c> overload), which would store the wrong boxed type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BoxingDiagnosticAnalyzer : DiagnosticAnalyzer
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
				// Only a conversion to object is reported: the cached boxes are object-typed, so boxing to an
				// interface (int -> IComparable, an enum -> Enum) has no fix and must not be flagged.
				if (!conversion.IsBoxing ||
					conversionOperation.Type?.SpecialType != SpecialType.System_Object ||
					!HasSpecialBox(conversionOperation, hasFlagMethod) ||
					conversionOperation.Syntax.Parent is not { } parent ||
					parent.IsKind(SyntaxKind.AttributeArgument) ||
					IsStringConcatenationOperand(conversionOperation) ||
					IsInOmittedConditionalCall(conversionOperation, context.CancellationToken))
				{
					return;
				}

				context.ReportDiagnostic(Diagnostic.Create(s_descriptorBoxing, conversionOperation.Syntax.GetLocation()));
			}, OperationKind.Conversion);

			context.RegisterOperationAction(context =>
			{
				var invocationOperation = (IInvocationOperation)context.Operation;
				if (invocationOperation.TargetMethod is { Name: "SetValue", Parameters.Length: 2 } targetMethod &&
					IsType(targetMethod.Parameters[0].Type, "Microsoft.UI.Xaml", "DependencyProperty") &&
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

	/// <summary>
	/// Whether the conversion is the implicit one the compiler inserts for a string concatenation operand
	/// (<c>s + value</c> or <c>s += value</c>). The compiler lowers those to a <c>ToString()</c> call and the box never
	/// reaches IL, so "fixing" one with <c>Boxes.Box</c> would introduce the very allocation this rule is meant to
	/// remove. An explicit <c>(object)value</c> operand does box, so it is still reported.
	/// </summary>
	private static bool IsStringConcatenationOperand(IConversionOperation operation)
		=> operation.IsImplicit &&
			operation.Parent is IBinaryOperation { OperatorKind: BinaryOperatorKind.Add } or ICompoundAssignmentOperation { OperatorKind: BinaryOperatorKind.Add } &&
			operation.Parent.Type?.SpecialType == SpecialType.System_String;

	internal static bool IsType(ITypeSymbol? type, string containingNamespace, string name)
		=> type?.Name == name && type.ContainingNamespace?.ToDisplayString() == containingNamespace;

	/// <summary>
	/// Whether the conversion is an argument to a <see cref="System.Diagnostics.ConditionalAttribute"/> call that is
	/// omitted at this location. The whole call is dropped at emit, so the boxing never happens - reporting it would
	/// only churn tracing code (REPEATER_TRACE_INFO and friends) for no runtime gain.
	/// </summary>
	private static bool IsInOmittedConditionalCall(IConversionOperation operation, CancellationToken cancellationToken)
	{
		// A params argument is wrapped in an implicit array creation, so walk up rather than
		// expecting the argument to be the direct parent.
		IOperation? current = operation.Parent;
		while (current is IConversionOperation or IArgumentOperation or IArrayInitializerOperation or IArrayCreationOperation)
		{
			current = current.Parent;
		}

		if (current is not IInvocationOperation invocation)
		{
			return false;
		}

		HashSet<string>? definedSymbols = null;
		var isConditional = false;
		foreach (var attribute in invocation.TargetMethod.GetAttributes())
		{
			if (IsType(attribute.AttributeClass, "System.Diagnostics", "ConditionalAttribute") &&
				attribute.ConstructorArguments.Length == 1 &&
				attribute.ConstructorArguments[0].Value is string condition)
			{
				definedSymbols ??= GetDefinedSymbols(operation.Syntax, cancellationToken);

				// Conditions are ORed: a single defined symbol keeps the call.
				if (definedSymbols is null || definedSymbols.Contains(condition))
				{
					return false;
				}

				isConditional = true;
			}
		}

		return isConditional;
	}

	/// <summary>
	/// The preprocessor symbols defined at <paramref name="node"/>, including the file's own <c>#define</c> and
	/// <c>#undef</c> directives, which the parse options do not carry.
	/// </summary>
	private static HashSet<string>? GetDefinedSymbols(SyntaxNode node, CancellationToken cancellationToken)
	{
		if (node.SyntaxTree.Options is not CSharpParseOptions parseOptions)
		{
			return null;
		}

		HashSet<string> symbols = new(parseOptions.PreprocessorSymbolNames);
		var directive = node.SyntaxTree.GetCompilationUnitRoot(cancellationToken).GetFirstDirective();
		while (directive is not null && directive.SpanStart < node.SpanStart)
		{
			if (directive.IsActive)
			{
				switch (directive)
				{
					case DefineDirectiveTriviaSyntax define:
						symbols.Add(define.Name.ValueText);
						break;
					case UndefDirectiveTriviaSyntax undef:
						symbols.Remove(undef.Name.ValueText);
						break;
				}
			}

			directive = directive.GetNextDirective();
		}

		return symbols;
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
			// Keep the values to check against synchronized with Boxes.Box(double), which compares bit patterns:
			// -0.0 == 0.0, yet it keeps its own box, so it has nothing cached to use.
			return !operation.Operand.ConstantValue.HasValue ||
				operation.Operand.ConstantValue.Value is double value && (BitConverter.DoubleToInt64Bits(value) == 0 || value == 1.0);
		}
		else if (IsType(operandType, "Uno.UI.Xaml", "RoutedEventFlag") && !IsOptimizedHasFlagCall(operation, hasFlagMethod))
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
