#nullable enable

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.DependencyObject.Models;

namespace Uno.UI.SourceGenerators.DependencyObject;

partial class DependencyPropertyModelBuilder
{
	private DefaultValueInfo? ResolveDefaultValue(string name, ITypeSymbol propertyType, Location? location, string displayName)
	{
		var methodName = $"Get{name}DefaultValue";
		var methods = GetOrdinaryMethods(methodName).ToArray();

		if (_arguments.DefaultValue is { } defaultValue)
		{
			if (methods.Length > 0)
			{
				Report(DependencyPropertyDiagnostics.DefaultValueConflict, location, displayName, methodName + "()");
				return null;
			}

			if (TryFormatDefaultValue(defaultValue, propertyType) is { } formatted)
			{
				return formatted;
			}

			// The compiler already reports an unresolved value or type, so fall back to a default that keeps the property.
			if (defaultValue.Kind == TypedConstantKind.Error || IsUnresolved(propertyType))
			{
				return GetDefaultOf(propertyType);
			}

			Report(
				DependencyPropertyDiagnostics.IncompatibleDefaultValue,
				_arguments.GetLocation(AttributeArguments.DefaultValueName) ?? location,
				_arguments.GetExpressionText(AttributeArguments.DefaultValueName) ?? defaultValue.ToCSharpString(),
				propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
				name);
			return null;
		}

		if (methods.Length > 0)
		{
			var method = methods.FirstOrDefault(m =>
				m.IsStatic &&
				m.Parameters.Length == 0 &&
				!m.IsGenericMethod &&
				!m.ReturnsVoid &&
				!m.ReturnsByRef &&
				!m.ReturnsByRefReadonly &&
				!m.ReturnType.IsRefLikeType &&
				m.ReturnType.TypeKind != TypeKind.Pointer);

			if (method is null)
			{
				Report(DependencyPropertyDiagnostics.InvalidDefaultValueMethod, GetSourceLocation(methods[0]) ?? location, $"{_containingType.Name}.{methodName}");
				return null;
			}

			if (!IsBoxCompatible(method.ReturnType, propertyType))
			{
				Report(
					DependencyPropertyDiagnostics.IncompatibleDefaultValueMethod,
					GetSourceLocation(method) ?? location,
					$"{_containingType.Name}.{methodName}",
					method.ReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
					propertyType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
					name);
				return null;
			}

			return new DefaultValueInfo($"{methodName}()", CachedBox: null, BoxableType: method.ReturnType.ToDisplayString(s_fullyQualifiedFormat));
		}

		return GetDefaultOf(propertyType);
	}

	/// <summary>
	/// Whether a boxed <paramref name="valueType"/> can be read back as <paramref name="propertyType"/>, which the generated getter does with a cast.
	/// An <see cref="object"/> result is trusted to hold a pre-boxed value of the right type.
	/// </summary>
	private bool IsBoxCompatible(ITypeSymbol valueType, ITypeSymbol propertyType)
	{
		if (valueType.SpecialType == SpecialType.System_Object || IsUnresolved(valueType) || IsUnresolved(propertyType))
		{
			return true;
		}

		if (propertyType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableType &&
			SymbolEqualityComparer.Default.Equals(nullableType.TypeArguments[0], valueType))
		{
			return true;
		}

		// Numeric and user-defined conversions change the boxed type, so only reference and boxing conversions keep it readable.
		var conversion = _compilation.ClassifyCommonConversion(valueType, propertyType);
		return conversion.IsIdentity || (conversion.IsImplicit && !conversion.IsUserDefined && propertyType.IsReferenceType);
	}

	private static DefaultValueInfo GetDefaultOf(ITypeSymbol type)
	{
		// default(T) wouldn't compile either, so avoid adding an error in generated code.
		if (IsUnresolved(type))
		{
			return new DefaultValueInfo("null", CachedBox: null, BoxableType: null);
		}

		switch (type.SpecialType)
		{
			case SpecialType.System_Boolean:
				return new DefaultValueInfo("false", "BoolBoxes.False", BoxableType: null);
			case SpecialType.System_Int32:
				return new DefaultValueInfo("0", "IntBoxes.Zero", BoxableType: null);
			case SpecialType.System_Double:
				return new DefaultValueInfo("0d", "DoubleBoxes.Zero", BoxableType: null);
		}

		if (type.IsReferenceType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
		{
			return new DefaultValueInfo("null", CachedBox: null, BoxableType: null);
		}

		var typeName = type.ToDisplayString(s_fullyQualifiedFormat);
		return new DefaultValueInfo($"default({typeName})", CachedBox: null, BoxableType: typeName);
	}

	private DefaultValueInfo? TryFormatDefaultValue(TypedConstant constant, ITypeSymbol propertyType)
	{
		if (constant.Kind == TypedConstantKind.Error)
		{
			return null;
		}

		var isNullable = propertyType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
		var valueType = isNullable ? ((INamedTypeSymbol)propertyType).TypeArguments[0] : propertyType;

		if (constant.IsNull)
		{
			return propertyType.IsReferenceType || isNullable
				? new DefaultValueInfo("null", CachedBox: null, BoxableType: null)
				: null;
		}

		switch (constant.Kind)
		{
			case TypedConstantKind.Type when constant.Value is ITypeSymbol typeValue && constant.Type is not null && _compilation.HasImplicitConversion(constant.Type, propertyType):
				return new DefaultValueInfo($"typeof({typeValue.ToDisplayString(s_fullyQualifiedFormat)})", CachedBox: null, BoxableType: null);

			case TypedConstantKind.Enum when constant.Type is INamedTypeSymbol enumType && ConstantFormatter.TryGetBits(constant.Value, out var enumBits):
				if (SymbolEqualityComparer.Default.Equals(enumType, valueType) || (valueType.TypeKind != TypeKind.Enum && _compilation.HasImplicitConversion(enumType, propertyType)))
				{
					return CreateEnumDefault(enumType, enumBits);
				}

				// An enum constant for a numeric property is converted through its underlying value.
				return constant.Value is { } underlyingValue && valueType.TypeKind != TypeKind.Enum
					? TryFormatPrimitive(underlyingValue, propertyType, valueType)
					: null;

			case TypedConstantKind.Primitive when constant.Value is { } value:
				return TryFormatPrimitive(value, propertyType, valueType);

			default:
				return null;
		}
	}

	private DefaultValueInfo? TryFormatPrimitive(object value, ITypeSymbol propertyType, ITypeSymbol valueType)
	{
		if (value is string or bool)
		{
			var sourceType = _compilation.GetSpecialType(value is string ? SpecialType.System_String : SpecialType.System_Boolean);
			return SymbolEqualityComparer.Default.Equals(sourceType, valueType) || (propertyType.IsReferenceType && _compilation.HasImplicitConversion(sourceType, propertyType))
				? CreatePrimitiveDefault(value)
				: null;
		}

		if (valueType is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } underlyingType } enumType)
		{
			return ConstantFormatter.TryConvert(value, underlyingType.SpecialType, out var converted) && ConstantFormatter.TryGetBits(converted, out var bits)
				? CreateEnumDefault(enumType, bits)
				: null;
		}

		if (valueType.SpecialType is >= SpecialType.System_Char and <= SpecialType.System_Double)
		{
			return ConstantFormatter.TryConvert(value, valueType.SpecialType, out var converted) && converted is not null
				? CreatePrimitiveDefault(converted)
				: null;
		}

		// Boxed as its own type for object, ValueType, IComparable and similar properties.
		var constantType = _compilation.GetSpecialType(GetSpecialType(value));
		return propertyType.IsReferenceType && constantType.SpecialType != SpecialType.None && _compilation.HasImplicitConversion(constantType, propertyType)
			? CreatePrimitiveDefault(value)
			: null;
	}

	private static DefaultValueInfo CreatePrimitiveDefault(object value)
	{
		var cachedBox = value switch
		{
			true => "BoolBoxes.True",
			false => "BoolBoxes.False",
			-1 => "IntBoxes.NegativeOne",
			0 => "IntBoxes.Zero",
			1 => "IntBoxes.One",
			// Compared by bit pattern, like Boxer.Box(double) and the boxing analyzer, so -0.0 stays a plain literal.
			double d when BitConverter.DoubleToInt64Bits(d) == 0 => "DoubleBoxes.Zero",
			double d when BitConverter.DoubleToInt64Bits(d) == BitConverter.DoubleToInt64Bits(1d) => "DoubleBoxes.One",
			_ => null,
		};

		return new DefaultValueInfo(ConstantFormatter.FormatLiteral(value), cachedBox, BoxableType: null);
	}

	private static DefaultValueInfo CreateEnumDefault(INamedTypeSymbol enumType, ulong bits)
		=> new(ConstantFormatter.FormatEnum(enumType, bits), CachedBox: null, BoxableType: enumType.ToDisplayString(s_fullyQualifiedFormat));

	private static SpecialType GetSpecialType(object value)
		=> value switch
		{
			char => SpecialType.System_Char,
			sbyte => SpecialType.System_SByte,
			byte => SpecialType.System_Byte,
			short => SpecialType.System_Int16,
			ushort => SpecialType.System_UInt16,
			int => SpecialType.System_Int32,
			uint => SpecialType.System_UInt32,
			long => SpecialType.System_Int64,
			ulong => SpecialType.System_UInt64,
			float => SpecialType.System_Single,
			double => SpecialType.System_Double,
			_ => SpecialType.None,
		};
}
