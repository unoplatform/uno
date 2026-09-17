#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.DependencyObject;

partial class DependencyPropertyModelBuilder
{
	/// <summary>
	/// Resolves the property changed callback invocation, in terms of the <c>instance</c> and <c>args</c> lambda parameters.
	/// </summary>
	/// <param name="targetType">The attached property target type, or <see langword="null"/> for an instance property.</param>
	private string? ResolveChangedCallback(string name, ITypeSymbol propertyType, ITypeSymbol? targetType, Location? location)
	{
		var methodName = _arguments.ChangedCallbackMethodName ?? $"On{name}Changed";
		var isRequested = _arguments.ChangedCallback || _arguments.ChangedCallbackMethodName is not null;
		var methods = GetOrdinaryMethods(methodName)
			.Where(m => !m.IsGenericMethod && m.Parameters.All(p => p.RefKind == RefKind.None) && (m.IsStatic || targetType is null))
			.ToArray();

		if (!isRequested && !GetOrdinaryMethods(methodName).Any())
		{
			return null;
		}

		var typeName = propertyType.ToDisplayString(s_fullyQualifiedFormat);
		var escapedName = EscapeIdentifier(methodName);
		var senderType = targetType ?? _containingType;

		foreach (var method in methods)
		{
			if (method.Parameters.Length == 2 && IsXamlType(method.Parameters[1].Type, "DependencyPropertyChangedEventArgs") && IsSenderCompatible(method.Parameters[0].Type, senderType))
			{
				return $"{GetReceiver(method)}{escapedName}({GetSenderArgument(method.Parameters[0].Type)}, args)";
			}
		}

		foreach (var method in methods)
		{
			if (method.Parameters.Length == 1 && IsXamlType(method.Parameters[0].Type, "DependencyPropertyChangedEventArgs"))
			{
				return $"{GetReceiver(method)}{escapedName}(args)";
			}
		}

		foreach (var method in methods)
		{
			if (method.Parameters.Length == 2 &&
				!IsXamlType(method.Parameters[1].Type, "DependencyPropertyChangedEventArgs") &&
				IsImplicitlyConvertible(propertyType, method.Parameters[0].Type) &&
				IsImplicitlyConvertible(propertyType, method.Parameters[1].Type))
			{
				return $"{GetReceiver(method)}{escapedName}(({typeName})args.OldValue, ({typeName})args.NewValue)";
			}
		}

		foreach (var method in methods)
		{
			if (method.Parameters.Length == 0)
			{
				return $"{GetReceiver(method)}{escapedName}()";
			}
		}

		if (methods.Any(HasUnresolvedSignature))
		{
			return null;
		}

		var shownType = propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		Report(
			DependencyPropertyDiagnostics.InvalidChangedCallback,
			_arguments.GetLocation(AttributeArguments.ChangedCallbackNameName) ?? location,
			methodName,
			name,
			$"{(targetType is null ? "" : "a static method taking ")}(DependencyObject, DependencyPropertyChangedEventArgs), (DependencyPropertyChangedEventArgs), ({shownType} oldValue, {shownType} newValue) or ()");

		return null;
	}

	/// <summary>
	/// Resolves the coerce callback invocation, in terms of the <c>instance</c>, <c>baseValue</c> and <c>precedence</c> lambda parameters.
	/// </summary>
	/// <param name="targetType">The attached property target type, or <see langword="null"/> for an instance property.</param>
	private string? ResolveCoerceCallback(string name, ITypeSymbol propertyType, ITypeSymbol? targetType, Location? location)
	{
		var methodName = $"Coerce{name}";
		var allMethods = GetOrdinaryMethods(methodName).ToArray();

		if (!_arguments.CoerceCallback && allMethods.Length == 0)
		{
			return null;
		}

		var escapedName = EscapeIdentifier(methodName);
		var senderType = targetType ?? _containingType;

		foreach (var method in allMethods)
		{
			if (method.IsGenericMethod ||
				method.Parameters.Any(p => p.RefKind != RefKind.None) ||
				(method.ReturnType.SpecialType != SpecialType.System_Object && !IsUnresolved(method.ReturnType)) ||
				(!method.IsStatic && targetType is not null))
			{
				continue;
			}

			var parameters = method.Parameters;
			var baseValueIndex = method.IsStatic ? 1 : 0;
			if (parameters.Length < baseValueIndex + 1 || parameters.Length > baseValueIndex + 2)
			{
				continue;
			}

			if (method.IsStatic && !IsSenderCompatible(parameters[0].Type, senderType))
			{
				continue;
			}

			string baseValueArgument;
			if (parameters[baseValueIndex].Type.SpecialType == SpecialType.System_Object)
			{
				baseValueArgument = "baseValue";
			}
			else if (IsImplicitlyConvertible(propertyType, parameters[baseValueIndex].Type))
			{
				baseValueArgument = $"({propertyType.ToDisplayString(s_fullyQualifiedFormat)})baseValue";
			}
			else
			{
				continue;
			}

			var hasPrecedence = parameters.Length == baseValueIndex + 2;
			if (hasPrecedence && !IsXamlType(parameters[baseValueIndex + 1].Type, "DependencyPropertyValuePrecedences") && !IsUnresolved(parameters[baseValueIndex + 1].Type))
			{
				continue;
			}

			var senderArgument = method.IsStatic ? GetSenderArgument(parameters[0].Type) + ", " : "";
			var precedenceArgument = hasPrecedence ? ", precedence" : "";

			return $"{GetReceiver(method)}{escapedName}({senderArgument}{baseValueArgument}{precedenceArgument})";
		}

		if (allMethods.Any(m => (m.IsStatic || targetType is null) && HasUnresolvedSignature(m)))
		{
			return null;
		}

		Report(
			DependencyPropertyDiagnostics.InvalidCoerceCallback,
			_arguments.GetLocation(AttributeArguments.CoerceCallbackName) ?? location,
			methodName,
			name,
			targetType is null
				? "object (object baseValue[, DependencyPropertyValuePrecedences precedence]) or static object (DependencyObject, object baseValue[, DependencyPropertyValuePrecedences precedence])"
				: "static object (DependencyObject, object baseValue[, DependencyPropertyValuePrecedences precedence])");

		return null;
	}

	private string GetReceiver(IMethodSymbol method)
		=> method.IsStatic ? $"{_hierarchy.FullyQualifiedName}." : $"(({_hierarchy.FullyQualifiedName})instance).";

	/// <summary>
	/// The sender must accept any instance of the containing type (attached: the target type), so the cast in
	/// <see cref="GetSenderArgument"/> can't fail.
	/// </summary>
	private bool IsSenderCompatible(ITypeSymbol parameterType, ITypeSymbol senderType)
		=> parameterType.SpecialType == SpecialType.System_Object || IsImplicitlyConvertible(senderType, parameterType);

	/// <summary>
	/// The <c>instance</c> lambda parameter is typed as DependencyObject, so a more derived parameter needs a cast.
	/// </summary>
	private static string GetSenderArgument(ITypeSymbol parameterType)
		=> parameterType.SpecialType == SpecialType.System_Object || IsXamlType(parameterType, "DependencyObject")
			? "instance"
			: $"({parameterType.ToDisplayString(s_fullyQualifiedFormat)})instance";
}
