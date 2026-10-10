#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Uno.UI.SourceGenerators.Internal;

/// <summary>
/// Flags boxing conversions on the dependency property value path for which <c>Uno.UI.Helpers.Boxes</c> already
/// keeps a cached instance, and calls that bind to a typed <c>SetValue</c> overload through an implicit numeric conversion
/// (e.g. a <c>float</c> reaching a <c>double</c> overload), which would store the wrong boxed type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BoxingDiagnosticAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor s_descriptorBoxing = new(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"UnoInternal0002",
#pragma warning restore RS2008 // Enable analyzer release tracking
		"Avoid boxing allocation",
		"Avoid boxing allocation, use 'Uno.UI.Helpers.Boxes' instead",
		"Performance",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	private static readonly DiagnosticDescriptor s_descriptorConversion = new(
#pragma warning disable RS2008 // Enable analyzer release tracking
		"UnoInternal0003",
#pragma warning restore RS2008 // Enable analyzer release tracking
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
					!IsDependencyPropertyValue(conversionOperation, context.ContainingSymbol))
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
					targetMethod.Parameters[1].Type.SpecialType != SpecialType.System_Object &&
					GetArgumentForParameter(invocationOperation, ordinal: 1) is { } valueArgument)
				{
					var argumentOperation = valueArgument.Value;
					while (argumentOperation is IConversionOperation conversion && conversion.IsImplicit)
					{
						argumentOperation = conversion.Operand;
					}

					if (argumentOperation.Type is { } argumentType &&
						!argumentType.Equals(targetMethod.Parameters[1].Type, SymbolEqualityComparer.Default))
					{
						context.ReportDiagnostic(Diagnostic.Create(
							s_descriptorConversion,
							valueArgument.Syntax.GetLocation(),
							targetMethod.Parameters[1].Type.ToDisplayString()));
					}
				}
			}, OperationKind.Invocation);
		});
	}

	/// <summary>
	/// Whether the boxed value flows into the property system: an argument of a call that takes a
	/// <c>DependencyProperty</c> (<c>SetValue</c>, <c>SetCurrentValue</c>...) or builds a <c>PropertyMetadata</c>, or the
	/// result of a DP callback (coercion, <c>PropMethodCall</c>, default value). Boxes anywhere else are left alone.
	/// </summary>
	private static bool IsDependencyPropertyValue(IOperation operation, ISymbol containingSymbol)
	{
		var value = operation;
		while (value.Parent is IConversionOperation ||
			(value.Parent is IConditionalOperation conditional && conditional.Condition != value))
		{
			value = value.Parent;
		}

		return value.Parent switch
		{
			IArgumentOperation { Parent: IInvocationOperation invocation } => IsDependencyPropertyApi(invocation.TargetMethod),
			IArgumentOperation { Parent: IObjectCreationOperation { Constructor: { } constructor } } => IsDependencyPropertyApi(constructor),
			IReturnOperation => GetEnclosingFunction(value, containingSymbol) is { ReturnType.SpecialType: SpecialType.System_Object } function &&
				function.Parameters.Any(p => IsPropertySystemType(p.Type)),
			// GetDefaultValue2(DependencyProperty, out object) hands the value back through its out parameter.
			ISimpleAssignmentOperation { Target: IParameterReferenceOperation { Parameter.RefKind: RefKind.Out } } assignment when assignment.Value == value =>
				GetEnclosingFunction(value, containingSymbol) is { } function && function.Parameters.Any(p => IsPropertySystemType(p.Type)),
			_ => false,
		};
	}

	private static bool IsDependencyPropertyApi(IMethodSymbol method)
	{
		foreach (var parameter in method.Parameters)
		{
			if (IsType(parameter.Type, "Microsoft.UI.Xaml", "DependencyProperty"))
			{
				return true;
			}
		}

		for (var type = method.ContainingType; type is not null; type = type.BaseType)
		{
			if (IsType(type, "Microsoft.UI.Xaml", "PropertyMetadata"))
			{
				return true;
			}
		}

		return false;
	}

	private static IMethodSymbol? GetEnclosingFunction(IOperation operation, ISymbol containingSymbol)
	{
		for (var current = operation.Parent; current is not null; current = current.Parent)
		{
			switch (current)
			{
				case IAnonymousFunctionOperation lambda:
					return lambda.Symbol;
				case ILocalFunctionOperation localFunction:
					return localFunction.Symbol;
			}
		}

		return containingSymbol as IMethodSymbol;
	}

	// The parameter types that make an object-returning function a DP callback: coercion
	// (DependencyPropertyValuePrecedences), PropMethodCall (DependencyObject) or a per-property lookup.
	private static bool IsPropertySystemType(ITypeSymbol type)
		=> IsType(type, "Microsoft.UI.Xaml", "DependencyProperty") ||
			IsType(type, "Microsoft.UI.Xaml", "DependencyObject") ||
			IsType(type, "Microsoft.UI.Xaml", "DependencyPropertyValuePrecedences");


	// Arguments are in evaluation order, which differs from parameter order when named arguments are reordered.
	private static IArgumentOperation? GetArgumentForParameter(IInvocationOperation invocation, int ordinal)
	{
		foreach (var argument in invocation.Arguments)
		{
			if (argument.Parameter?.Ordinal == ordinal)
			{
				return argument;
			}
		}

		return null;
	}

	// Boxer.Box(RoutedEventFlag) caches every declared member, but not their combinations.
	private static bool IsDeclaredEnumMember(ITypeSymbol enumType, object? value)
	{
		foreach (var member in enumType.GetMembers())
		{
			if (member is IFieldSymbol { HasConstantValue: true } field && Equals(field.ConstantValue, value))
			{
				return true;
			}
		}

		return false;
	}

	internal static bool IsType(ITypeSymbol? type, string containingNamespace, string name)
		=> type?.Name == name && type.ContainingNamespace?.ToDisplayString() == containingNamespace;

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
			// Keep the values to check against (ie, -1, 0, 1) synchronized with Boxer.Box(int).
			return !operation.Operand.ConstantValue.HasValue || operation.Operand.ConstantValue.Value is -1 or 0 or 1;
		}
		else if (operandSpecialType == SpecialType.System_Double)
		{
			// Keep the values to check against synchronized with Boxer.Box(double), which compares bit patterns:
			// -0.0 == 0.0, yet it keeps its own box, so it has nothing cached to use.
			return !operation.Operand.ConstantValue.HasValue ||
				operation.Operand.ConstantValue.Value is double value && (BitConverter.DoubleToInt64Bits(value) == 0 || value == 1.0);
		}
		else if (IsType(operandType, "Uno.UI.Xaml", "RoutedEventFlag") && !IsOptimizedHasFlagCall(operation, hasFlagMethod))
		{
			return !operation.Operand.ConstantValue.HasValue || IsDeclaredEnumMember(operandType, operation.Operand.ConstantValue.Value);
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
