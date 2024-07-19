// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ValueHelpers.cpp

using System;
using System.Globalization;

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

	internal static string ConvertValueToString(object value, Type type) => Convert.ToString(value, CultureInfo.InvariantCulture);
}
