// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ValueHelpers.cpp

using System;
using System.Globalization;
using Windows.Foundation;

namespace Uno.UI.Helpers.WinUI;

// Uno specific: In WinUI this class works with IPropertyValue members.
// For C# projection, using Type is more accurate. To match the behavior,
// non WinRT types are not present (e.g. sbyte, decimal).
internal static class ValueConversionHelpers
{
	internal static bool CanConvertValueToString(Type type) =>
		type == typeof(byte) ||
		type == typeof(short) ||
		type == typeof(ushort) ||
		type == typeof(int) ||
		type == typeof(uint) ||
		type == typeof(long) ||
		type == typeof(ulong) ||
		type == typeof(float) ||
		type == typeof(double) ||
		type == typeof(char) ||
		type == typeof(bool) ||
		type == typeof(string) ||
		type == typeof(Guid);

	internal static object ConvertStringToValue(string hstr, PropertyType parameterType) =>
		parameterType == PropertyType.UInt8 ? byte.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.Int16 ? short.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.UInt16 ? ushort.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.Int32 ? int.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.UInt32 ? uint.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.Int64 ? long.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.UInt64 ? ulong.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.Single ? float.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.Double ? double.Parse(hstr, CultureInfo.InvariantCulture) :
		parameterType == PropertyType.Char16 ? char.Parse(hstr) :
		parameterType == PropertyType.Boolean ? bool.Parse(hstr) :
		parameterType == PropertyType.String ? hstr :
		parameterType == PropertyType.Guid ? Guid.Parse(hstr, CultureInfo.InvariantCulture) :
		null;

	internal static string ConvertValueToString(object value, Type type) => Convert.ToString(value, CultureInfo.InvariantCulture);

	// TODO Uno: IPropertyValue projection. A null type stands for an empty IPropertyValue. Enums report
	// OtherType, as C++/WinRT's boxed IReference<enum> does; any other type outside the WinRT scalar set does too.
	// Callers decide whether the value is an IPropertyValue at all (WinRT boxes only value types and strings).
	internal static PropertyType GetPropertyType(Type type) =>
		type is null ? PropertyType.Empty :
		type == typeof(byte) ? PropertyType.UInt8 :
		type == typeof(short) ? PropertyType.Int16 :
		type == typeof(ushort) ? PropertyType.UInt16 :
		type == typeof(int) ? PropertyType.Int32 :
		type == typeof(uint) ? PropertyType.UInt32 :
		type == typeof(long) ? PropertyType.Int64 :
		type == typeof(ulong) ? PropertyType.UInt64 :
		type == typeof(float) ? PropertyType.Single :
		type == typeof(double) ? PropertyType.Double :
		type == typeof(char) ? PropertyType.Char16 :
		type == typeof(bool) ? PropertyType.Boolean :
		type == typeof(string) ? PropertyType.String :
		type == typeof(Guid) ? PropertyType.Guid :
		// Windows.Foundation.DateTime projects to DateTimeOffset; System.DateTime is accepted for the same value.
		type == typeof(DateTimeOffset) ? PropertyType.DateTime :
		type == typeof(DateTime) ? PropertyType.DateTime :
		type == typeof(TimeSpan) ? PropertyType.TimeSpan :
		type == typeof(Point) ? PropertyType.Point :
		type == typeof(Size) ? PropertyType.Size :
		type == typeof(Rect) ? PropertyType.Rect :
		PropertyType.OtherType;

	// TODO Uno: IPropertyValue projection, the equivalent of try_as<IPropertyValue>() followed by Type().
	// WinRT boxes only value types and strings; any other reference type is not an IPropertyValue.
	internal static bool TryGetPropertyType(object value, out PropertyType type)
	{
		if (value is string || (value is not null && value.GetType().IsValueType))
		{
			type = GetPropertyType(value.GetType());
			return true;
		}

		type = PropertyType.Empty;
		return false;
	}
}
