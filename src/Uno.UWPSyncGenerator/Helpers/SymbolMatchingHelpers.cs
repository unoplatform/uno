using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.Helpers;

internal static class SymbolMatchingHelpers
{
	public static bool AreMatching(ISymbol uapSymbol, ISymbol unoSymbol)
	{
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
		else
		{
			throw new ArgumentException($"Unexpected symbol '{uapSymbol?.Kind.ToString() ?? "<null>"}'");
		}
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

		if (uapSymbol.Name == "Brush" || uapSymbol.Name == "Transform")
		{
			// They're abstract in Uno but not in UWP. Skip for now.
			return true;
		}

		// Skipping accessibility check for now. It causes two issues:
		// 1. For some reason, Roslyn is returning private accessibility for some public properties (Specifically, for some interface implementations).
		// 2. For types declared without explicit accessibility, it's going to be considered internal (however, the generated file later will have the correct accessibility)
		var result = /*uapSymbol.DeclaredAccessibility == unoSymbol.DeclaredAccessibility &&*/
			uapSymbol.IsAbstract == unoSymbol.IsAbstract &&
			uapSymbol.IsOverride == unoSymbol.IsOverride &&
			uapSymbol.Name == unoSymbol.Name &&
			// Temporary skip named type: Until we match seal-ness and static-ness with UWP.
			(uapSymbol.IsSealed == unoSymbol.IsSealed || uapSymbol.Kind == SymbolKind.NamedType) &&
			(uapSymbol.IsStatic == unoSymbol.IsStatic || uapSymbol.Kind == SymbolKind.NamedType) &&
			(uapSymbol.IsVirtual == unoSymbol.IsVirtual || SkipVirtualMatching(uapSymbol));
		return result;
	}

	private static bool SkipVirtualMatching(ISymbol uapSymbol)
	{
		return
			(uapSymbol.Name == "Margin" && uapSymbol.ContainingType.Name == "FrameworkElement") ||
			(uapSymbol.Name == "Child" && uapSymbol.ContainingType.Name == "Border") ||
			uapSymbol.Name == "ContentTemplateRoot" ||
			uapSymbol.Name == "Content" ||
			// For Uno, IsEnabled is defined in FrameworkElement as virtual, which is inherited in Control.
			// For UWP, IsEnabled is defined directly in Control.
			uapSymbol.Name == "IsEnabled";
	}

	private static bool AreEventsMatching(IEventSymbol uapEvent, IEventSymbol unoEvent)
	{
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
		if (uapProperty.Name == "Name" && uapProperty.ContainingType.Name == "FrameworkElement")
		{
			// Skip for now. We don't have a setter while UWP has.
			return true;
		}

		if (uapProperty.Name == "GradientStops" && uapProperty.ContainingType.Name == "RadialGradientBrush")
		{
			// Skip for now. Delete when https://github.com/unoplatform/uno/pull/7170 is merged.
			return true;
		}

		if (uapProperty.Name == "ProtectedCursor" && uapProperty.ContainingType.Name == "UIElement")
		{
			// https://github.com/unoplatform/uno/pull/7249#issuecomment-1032660134
			return true;
		}
		if (uapProperty.Name == "WindowActivationState" && uapProperty.ContainingType.Name == "WindowActivatedEventArgs")
		{
			// Skip for now. Property types diverges.
			// https://docs.microsoft.com/en-us/uwp/api/windows.ui.core.corewindowactivationstate?view=winrt-22000 (Uno)
			// vs.
			// https://docs.microsoft.com/en-us/windows/winui/api/microsoft.ui.xaml.windowactivationstate?view=winui-3.0 (WinUI)
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
			// PowerEase.Power is int in Uno but double in UWP.
			uapProperty.Name != "Power" &&
			// object vs UIElement
			uapProperty.Name != "Content" &&
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

		return AreMatchingCommon(uapMethod, unoMethod) &&
			(uapMethod.AssociatedSymbol != null || AreParametersMatching(uapMethod.Parameters, unoMethod.Parameters)) &&
			uapMethod.Arity == unoMethod.Arity &&
			uapMethod.IsAsync == unoMethod.IsAsync &&
			uapMethod.IsExtensionMethod == unoMethod.IsExtensionMethod &&
			uapMethod.IsReadOnly == unoMethod.IsReadOnly &&
			uapMethod.IsVararg == unoMethod.IsVararg &&
			uapMethod.MethodKind == unoMethod.MethodKind &&
			AreMatching(uapMethod.ReturnType, unoMethod.ReturnType) &&
			uapMethod.TypeArguments == unoMethod.TypeArguments;
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

	private static bool AreParametersMatching(IParameterSymbol uapParameters, IParameterSymbol unoParameters) =>
		uapParameters.IsOptional == unoParameters.IsOptional &&
		uapParameters.Name == unoParameters.Name &&
		uapParameters.IsParams == unoParameters.IsParams &&
		uapParameters.RefKind == unoParameters.RefKind &&
		AreMatching(uapParameters.Type, unoParameters.Type);
}
