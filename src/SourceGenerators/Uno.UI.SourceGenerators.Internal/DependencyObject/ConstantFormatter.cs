#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.UI.SourceGenerators.Internal.Incremental;

namespace Uno.UI.SourceGenerators.DependencyObject;

/// <summary>
/// Formats attribute constants as C# expressions, converted to a target type the way the compiler would.
/// </summary>
internal static class ConstantFormatter
{
	/// <summary>
	/// Converts a numeric or <see cref="char"/> constant to the given special type, rejecting lossy or out of range conversions.
	/// </summary>
	public static bool TryConvert(object value, SpecialType target, out object? converted)
	{
		converted = null;

		if (value is float or double)
		{
			var floating = Convert.ToDouble(value, CultureInfo.InvariantCulture);
			switch (target)
			{
				case SpecialType.System_Single:
					converted = (float)floating;
					return true;
				case SpecialType.System_Double:
					converted = floating;
					return true;
				case SpecialType.System_Decimal when !double.IsNaN(floating) && !double.IsInfinity(floating) && Math.Abs(floating) <= (double)decimal.MaxValue:
					converted = (decimal)floating;
					return true;
				default:
					return false;
			}
		}

		if (!TryGetIntegral(value, out var integral))
		{
			return false;
		}

		converted = target switch
		{
			SpecialType.System_SByte when integral is >= sbyte.MinValue and <= sbyte.MaxValue => (sbyte)integral,
			SpecialType.System_Byte when integral is >= byte.MinValue and <= byte.MaxValue => (byte)integral,
			SpecialType.System_Int16 when integral is >= short.MinValue and <= short.MaxValue => (short)integral,
			SpecialType.System_UInt16 when integral is >= ushort.MinValue and <= ushort.MaxValue => (ushort)integral,
			SpecialType.System_Char when integral is >= char.MinValue and <= char.MaxValue => (char)integral,
			SpecialType.System_Int32 when integral is >= int.MinValue and <= int.MaxValue => (int)integral,
			SpecialType.System_UInt32 when integral is >= uint.MinValue and <= uint.MaxValue => (uint)integral,
			SpecialType.System_Int64 when integral is >= long.MinValue and <= long.MaxValue => (long)integral,
			SpecialType.System_UInt64 when integral is >= ulong.MinValue and <= ulong.MaxValue => (ulong)integral,
			SpecialType.System_Single => (float)integral,
			SpecialType.System_Double => (double)integral,
			SpecialType.System_Decimal => integral,
			_ => null,
		};

		return converted is not null;
	}

	/// <summary>
	/// Formats a value of a special type as a C# expression of exactly that type.
	/// </summary>
	public static string FormatLiteral(object value)
		=> value switch
		{
			bool b => b ? "true" : "false",
			char c => SymbolDisplay.FormatLiteral(c, quote: true),
			string s => SymbolDisplay.FormatLiteral(s, quote: true),
			sbyte v => $"(sbyte)({v.ToString(CultureInfo.InvariantCulture)})",
			byte v => $"(byte){v.ToString(CultureInfo.InvariantCulture)}",
			short v => $"(short)({v.ToString(CultureInfo.InvariantCulture)})",
			ushort v => $"(ushort){v.ToString(CultureInfo.InvariantCulture)}",
			int v => v.ToString(CultureInfo.InvariantCulture),
			uint v => v.ToString(CultureInfo.InvariantCulture) + "U",
			long v => v.ToString(CultureInfo.InvariantCulture) + "L",
			ulong v => v.ToString(CultureInfo.InvariantCulture) + "UL",
			float v => FormatSingle(v),
			double v => FormatDouble(v),
			decimal v => v.ToString(CultureInfo.InvariantCulture) + "m",
			_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
		};

	/// <summary>
	/// Formats an enum value as a member access, a combination of flags, or a cast.
	/// </summary>
	public static string FormatEnum(INamedTypeSymbol enumType, ulong bits)
	{
		var typeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		var members = new List<(string Name, ulong Bits)>();

		foreach (var member in enumType.GetMembers())
		{
			if (member is IFieldSymbol { HasConstantValue: true } field && TryGetBits(field.ConstantValue, out var memberBits))
			{
				if (memberBits == bits)
				{
					return $"{typeName}.{HierarchyInfo.EscapeIdentifier(field.Name)}";
				}

				members.Add((field.Name, memberBits));
			}
		}

		if (bits != 0 && enumType.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.FlagsAttribute"))
		{
			var remaining = bits;
			var flags = new List<(string Name, ulong Bits)>();
			foreach (var member in members.Where(m => m.Bits != 0).OrderByDescending(m => m.Bits))
			{
				if ((bits & member.Bits) == member.Bits && (remaining & member.Bits) != 0)
				{
					flags.Add(member);
					remaining &= ~member.Bits;
				}
			}

			if (remaining == 0)
			{
				return string.Join(" | ", flags.OrderBy(f => f.Bits).Select(f => $"{typeName}.{HierarchyInfo.EscapeIdentifier(f.Name)}"));
			}
		}

		var underlyingValue = enumType.EnumUnderlyingType?.SpecialType is SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64
			? unchecked((long)bits).ToString(CultureInfo.InvariantCulture)
			: bits.ToString(CultureInfo.InvariantCulture);

		return $"({typeName})({underlyingValue})";
	}

	/// <summary>
	/// Gets the bit pattern of an integral constant, sign-extended to 64 bits.
	/// </summary>
	public static bool TryGetBits(object? value, out ulong bits)
	{
		bits = value switch
		{
			sbyte v => unchecked((ulong)v),
			byte v => v,
			short v => unchecked((ulong)v),
			ushort v => v,
			int v => unchecked((ulong)v),
			uint v => v,
			long v => unchecked((ulong)v),
			ulong v => v,
			char v => v,
			_ => 0,
		};

		return value is sbyte or byte or short or ushort or int or uint or long or ulong or char;
	}

	private static bool TryGetIntegral(object value, out decimal integral)
	{
		integral = value switch
		{
			sbyte v => v,
			byte v => v,
			short v => v,
			ushort v => v,
			int v => v,
			uint v => v,
			long v => v,
			ulong v => v,
			char v => v,
			_ => 0,
		};

		return value is sbyte or byte or short or ushort or int or uint or long or ulong or char;
	}

	private static string FormatDouble(double value)
	{
		if (double.IsNaN(value))
		{
			return "double.NaN";
		}

		if (double.IsPositiveInfinity(value))
		{
			return "double.PositiveInfinity";
		}

		if (double.IsNegativeInfinity(value))
		{
			return "double.NegativeInfinity";
		}

		// .NET Framework hosts format negative zero as "0".
		if (value == 0 && BitConverter.DoubleToInt64Bits(value) != 0)
		{
			return "-0d";
		}

		var text = value.ToString("R", CultureInfo.InvariantCulture);
		if (BitConverter.DoubleToInt64Bits(double.Parse(text, CultureInfo.InvariantCulture)) != BitConverter.DoubleToInt64Bits(value))
		{
			text = value.ToString("G17", CultureInfo.InvariantCulture);
		}

		return text + "d";
	}

	private static string FormatSingle(float value)
	{
		if (float.IsNaN(value))
		{
			return "float.NaN";
		}

		if (float.IsPositiveInfinity(value))
		{
			return "float.PositiveInfinity";
		}

		if (float.IsNegativeInfinity(value))
		{
			return "float.NegativeInfinity";
		}

		if (value == 0 && BitConverter.DoubleToInt64Bits(value) != 0)
		{
			return "-0f";
		}

		var text = value.ToString("R", CultureInfo.InvariantCulture);
		if (float.Parse(text, CultureInfo.InvariantCulture) != value)
		{
			text = value.ToString("G9", CultureInfo.InvariantCulture);
		}

		return text + "f";
	}
}
