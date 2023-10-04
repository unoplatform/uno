using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.Helpers;

internal static class SymbolMatchingHelpers
{
	public static bool AreMatching(ISymbol uapSymbol, ISymbol unoSymbol)
	{
		if (ShouldSkipSymbol(uapSymbol))
		{
			return true;
		}

		if (uapSymbol is IEventSymbol uapEvent)
		{
			var result = unoSymbol is IEventSymbol unoEvent && AreEventsMatching(uapEvent, unoEvent);
			return result;
		}
		else if (uapSymbol is IFieldSymbol uapField)
		{
			var result = unoSymbol is IFieldSymbol unoField && AreFieldsMatching(uapField, unoField);
			return result;
		}
		else if (uapSymbol is INamedTypeSymbol uapNamedType)
		{
			var result = unoSymbol is INamedTypeSymbol unoNamedType &&
				(
					(AreMatchingCommon(uapNamedType, unoNamedType) && uapNamedType.Name == unoNamedType.Name) ||
					// This happens for not implemented symbols that are annotated as nullable.
					// Since the compiler can't find the type, it considers it as value type, hence wraps it in Nullable<T>.
					unoNamedType.Name == "Nullable"
				);
			return result;
		}
		else if (uapSymbol is IPropertySymbol uapProperty)
		{
			var result = unoSymbol is IPropertySymbol unoProperty && ArePropertiesMatching(uapProperty, unoProperty);
			return result;
		}
		else if (uapSymbol is IMethodSymbol uapMethod)
		{
			var result = unoSymbol is IMethodSymbol unoMethod && AreMethodsMatching(uapMethod, unoMethod);
			return result;
		}
		else if (uapSymbol is ITypeParameterSymbol uapTypeParameter)
		{
			var result = unoSymbol is ITypeParameterSymbol unoTypeParameter && AreTypeParametersMatching(uapTypeParameter, unoTypeParameter);
			return result;
		}
		else if (uapSymbol is IArrayTypeSymbol uapArrayTypeSymbol)
		{
			var result = unoSymbol is IArrayTypeSymbol unoArrayTypeSymbol && AreMatching(uapArrayTypeSymbol.ElementType, unoArrayTypeSymbol.ElementType);
			return result;
		}
		else
		{
			throw new ArgumentException($"Unexpected symbol '{uapSymbol?.Kind.ToString() ?? "<null>"}'");
		}
	}

	private static bool ShouldSkipSymbol(ISymbol uapSymbol)
	{
		if (uapSymbol.Name == "TimeSpan" && uapSymbol.ContainingSymbol.Name == "Duration")
		{
			// field vs property difference between Uno and WinUI.
			return true;
		}

		if (uapSymbol.Name is "Left" or "Top" or "Right" or "Bottom" && uapSymbol.ContainingSymbol.Name == "Thickness")
		{
			// field vs property difference between Uno and WinUI.
			return true;
		}

		if (uapSymbol.Name is "Value" or "GridUnitType" && uapSymbol.ContainingSymbol.Name == "GridLength")
		{
			// field vs property difference between Uno and WinUI.
			return true;
		}

		if (uapSymbol.Name is "TopLeft" or "TopRight" or "BottomRight" or "BottomLeft" && uapSymbol.ContainingSymbol.Name == "CornerRadius")
		{
			// field vs property difference between Uno and WinUI.
			return true;
		}

		if (uapSymbol.ContainingSymbol?.Name is
			"ColorKeyFrameCollection" or
			"Matrix" or
			"KeyTime" or
			"PointCollection" or
			"RepeatBehavior" or
			"Matrix3D" or
			"InlineCollection" or
			"GridLength" or
			"GeneratorPosition" or
			"ToggleSwitch")
		{
			return true;
		}

		return false;
	}

	private static bool AreMatchingCommon(ISymbol uapSymbol, ISymbol unoSymbol)
	{
		if (unoSymbol.Kind == SymbolKind.ErrorType)
		{
			// Some events are marked not implemented, but used in implemented signatures.
			// For example, `HoldingEventHandler` and `UIElement.Holding`.
			// When we're matching `UIElement.Holding`, we get here with `HoldingEventHandler`
			// being an error symbol.
			// TODO: Move such symbols to non-generated files and remove this condition.
			return true;
		}

		if (uapSymbol.Name == "DependencyObject" || uapSymbol.ContainingSymbol.Name == "DependencyObject")
		{
			// We define DependencyObject as an interface, diverging from UWP.
			// This causes checks like IsAbstract to diverge for the type itself
			// as well as its members. Ignore them.
			return true;
		}

		if (uapSymbol.Name == "IGeometrySource2D" ||
			uapSymbol.Name == "SizeInt32" ||
			uapSymbol.Name == "PointInt32")
		{
			// For some reason, these are marked with Kind=ErrorType.
			// This means the matching then fails.
			return true;
		}

		if (uapSymbol.Name == "Transform" && uapSymbol.Kind == SymbolKind.NamedType)
		{
			// In Uno, it's abstract to force all derived classes to implement a specific method.
			// In UWP/WinUI, it's not abstract but it doesn't have any accessible constructors.
			// The divergence here shouldn't be problematic/noticeable.
			return true;
		}
		// Skipping accessibility check for now. It causes two issues:
		// 1. For some reason, Roslyn is returning private accessibility for some public properties (Specifically, for some interface implementations).
		// 2. For types declared without explicit accessibility, it's going to be considered internal (however, the generated file later will have the correct accessibility)
		var result = /*uapSymbol.DeclaredAccessibility == unoSymbol.DeclaredAccessibility &&*/
			uapSymbol.IsAbstract == unoSymbol.IsAbstract &&
			uapSymbol.IsOverride == unoSymbol.IsOverride &&
			StripGlobal(uapSymbol.Name) == StripGlobal(unoSymbol.Name) &&
			// Temporary skip named type: Until we match seal-ness and static-ness with UWP.
			(uapSymbol.IsSealed == unoSymbol.IsSealed || uapSymbol.Kind == SymbolKind.NamedType) &&
			(uapSymbol.IsStatic == unoSymbol.IsStatic) &&
			uapSymbol.IsVirtual == unoSymbol.IsVirtual;
		return result;
	}

	private static string StripGlobal(string s)
	{
		if (s.StartsWith("global::", StringComparison.Ordinal))
		{
			return s.Substring("global::".Length);
		}

		return s;
	}

	private static bool AreEventsMatching(IEventSymbol uapEvent, IEventSymbol unoEvent)
	{
		if (uapEvent.ContainingType.Name == "Window")
		{
			// TODO: Match API with WinUI.
			return true;
		}

		var result = AreMatchingCommon(uapEvent, unoEvent) && AreMatching(uapEvent.Type, unoEvent.Type);
		return result;
	}

	private static bool AreFieldsMatching(IFieldSymbol uapField, IFieldSymbol unoField) =>
		AreMatchingCommon(uapField, unoField) &&
		uapField.IsVolatile == unoField.IsVolatile &&
		(uapField.ConstantValue?.Equals(unoField.ConstantValue) ?? unoField.ConstantValue == null) &&
		uapField.IsConst == unoField.IsConst &&
		uapField.IsReadOnly == unoField.IsReadOnly &&
		AreMatching(uapField.Type, unoField.Type);

	private static bool ArePropertiesMatching(IPropertySymbol uapProperty, IPropertySymbol unoProperty)
	{
		if (uapProperty.Name == "WindowActivationState" && uapProperty.ContainingType.Name == "WindowActivatedEventArgs")
		{
			// TODO: Match API with WinUI.
			return true;
		}

		if (uapProperty.Name == "Name" && uapProperty.ContainingType.Name == "FrameworkElement")
		{
			// TODO: Name shouldn't be virtual.
			return true;
		}

		if (!AreMatchingCommon(uapProperty, unoProperty))
		{
			return false;
		}
		// TODO:
		//if (uapProperty.IsReadOnly != unoProperty.IsReadOnly)
		//{
		//	return false;
		//}
		//if (uapProperty.IsWriteOnly != unoProperty.IsWriteOnly)
		//{
		//	return false;
		//}
		if (uapProperty.IsIndexer != unoProperty.IsIndexer)
		{
			return false;
		}
		if (!AreParametersMatching(uapProperty.Parameters, unoProperty.Parameters))
		{
			return false;
		}

		// Property type of ContentTemplateRoot is diverging from UWP.
		// In Uno we use the native view for each platform Android.Views.View, UIKit.UIView, AppKit.NSView
		if (!AreMatching(uapProperty.Type, unoProperty.Type) && uapProperty.Name != "ContentTemplateRoot" &&
			// object vs UIElement
			uapProperty.Name != "Content" &&
			// IEasingFunction vs EasingFunctionBase
			uapProperty.Name != "EasingFunction" &&
			// string vs object
			uapProperty.Name != "ElementName")
		{
			return false;
		}

		return true;
	}

	private static bool AreMethodsMatching(IMethodSymbol uapMethod, IMethodSymbol unoMethod)
	{
		if (uapMethod == null)
		{
			return unoMethod == null;
		}
		else if (unoMethod == null)
		{
			return false;
		}

		if (uapMethod.Name == "ToString" && uapMethod.Parameters.IsEmpty)
		{
			return true;
		}

		if (uapMethod.Name is "LoadContent" or "Measure" or "Arrange")
		{
			return true;
		}

		return AreMatchingCommon(uapMethod, unoMethod) &&
			(uapMethod.AssociatedSymbol != null || AreParametersMatching(uapMethod.Parameters, unoMethod.Parameters)) &&
			uapMethod.Arity == unoMethod.Arity &&
			uapMethod.IsExtensionMethod == unoMethod.IsExtensionMethod &&
			uapMethod.IsReadOnly == unoMethod.IsReadOnly &&
			uapMethod.IsVararg == unoMethod.IsVararg &&
			uapMethod.MethodKind == unoMethod.MethodKind &&
			AreMatching(uapMethod.ReturnType, unoMethod.ReturnType) &&
			AreTypeArgumentsMatching(uapMethod.TypeArguments, unoMethod.TypeArguments);
	}

	private static bool AreTypeParametersMatching(ITypeParameterSymbol uapTypeParameter, ITypeParameterSymbol unoTypeParameter)
	{
		return AreMatchingCommon(uapTypeParameter, unoTypeParameter) &&
			uapTypeParameter.Ordinal == unoTypeParameter.Ordinal &&
			uapTypeParameter.Variance == unoTypeParameter.Variance &&
			uapTypeParameter.TypeParameterKind == unoTypeParameter.TypeParameterKind &&
			uapTypeParameter.HasReferenceTypeConstraint == unoTypeParameter.HasReferenceTypeConstraint &&
			uapTypeParameter.HasValueTypeConstraint == unoTypeParameter.HasValueTypeConstraint &&
			uapTypeParameter.HasUnmanagedTypeConstraint == unoTypeParameter.HasUnmanagedTypeConstraint &&
			uapTypeParameter.HasNotNullConstraint == unoTypeParameter.HasNotNullConstraint &&
			uapTypeParameter.HasConstructorConstraint == unoTypeParameter.HasConstructorConstraint;

	}

	private static bool AreParametersMatching(ImmutableArray<IParameterSymbol> uapParameters, ImmutableArray<IParameterSymbol> unoParameters)
	{
		if (uapParameters.Length != unoParameters.Length)
		{
			return false;
		}

		for (int i = 0; i < uapParameters.Length; i++)
		{
			if (!AreParametersMatching(uapParameters[i], unoParameters[i]))
			{
				return false;
			}
		}

		return true;
	}

	private static bool AreTypeArgumentsMatching(ImmutableArray<ITypeSymbol> uapTypeArguments, ImmutableArray<ITypeSymbol> unoTypeArguments)
	{
		if (uapTypeArguments.Length != unoTypeArguments.Length)
		{
			return false;
		}

		for (int i = 0; i < uapTypeArguments.Length; i++)
		{
			if (uapTypeArguments[i].Name != unoTypeArguments[i].Name)
			{
				return false;
			}
		}

		return true;
	}

	private static bool AreParametersMatching(IParameterSymbol uapParameters, IParameterSymbol unoParameters) =>
		uapParameters.IsOptional == unoParameters.IsOptional &&
		(uapParameters.Name == unoParameters.Name || IgnoreParameterName(uapParameters)) &&
		uapParameters.IsParams == unoParameters.IsParams &&
		(uapParameters.RefKind == unoParameters.RefKind || IgnoreRefKind(uapParameters)) &&
		AreMatching(uapParameters.Type, unoParameters.Type);

	private static bool IgnoreRefKind(IParameterSymbol uapParameter)
	{
		if (uapParameter.ContainingSymbol.Name == "Equals" && uapParameter.ContainingType.Name == "GuidHelper")
		{
			// GuidHelpers.Equals uses "in" RefKind in WinUI, while it uses "ref" RefKind in UWP.
			// In Uno, we use "ref" in both flavors.
			return true;
		}

		return false;
	}

	private static bool IgnoreParameterName(IParameterSymbol uapParameter)
	{
		var name = uapParameter.Name;
		if (name.StartsWith('_'))
		{
			// The '_' check is to ignore parameter name differences for badly named WinUI parameters.
			// For example, FontWeight constructor parameter in WinUI is called "_Weight", while we name it "weight"
			// Our naming makes more sense, and we want to avoid this breaking change for now.
			// We can revisit in the future if we want to match WinUI naming.
			return true;
		}
		else if (name == "windowsruntimeStream")
		{
			// In Uno, we name it windowsRuntimeStream.
			// Skip for now to avoid breaking changes.
			return true;
		}
		else if (uapParameter.ContainingSymbol.Name == "Equals")
		{
			// Some object.Equals overrides in WinUI use parameter name as "o" while uno uses "obj".
			// There is also CornerRadius.Equals where we name the parameter as "other" while WinUI name it "cornerRadius"
			// Also, Duration.Equals(Duration,Duration) in Uno names the parameters as "first"/"second" while WinUI name it as "t1"/"t2"
			// Skip for now to avoid breaking changes.
			return true;
		}
		else if (uapParameter.ContainingSymbol.Name == "Compare" && uapParameter.ContainingType.Name == "Duration")
		{
			// Duration.Compare(Duration,Duration) in Uno names the parameters as "first"/"second" while WinUI name it as "t1"/"t2"
			// Skip for now to avoid breaking changes.
			return true;
		}
		else if (name == "location" && uapParameter.ContainingType.Name == "Rect")
		{
			// Rect(Point,Size) constructor names the Point parameter as location in WinUI while we name it point in Uno.
			// Skip for now to avoid breaking changes.
			return true;
		}

		return false;
	}
}
