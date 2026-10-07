// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\ShapingHelpers.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml.Interop;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static Microsoft.UI.Xaml.Controls._Tracing;
using _IBindableVector = System.Collections.IList;
using INotifyCollectionChanged = System.Collections.Specialized.INotifyCollectionChanged;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class ShapingHelpers
{
	// namespace {

	private enum NumericKind { None, Signed, Unsigned, Floating }

	private enum ValueClassRank
	{
		Numeric = 0,
		String,
		Guid,
		Boolean,
		Char16,
		DateTime,
		TimeSpan,
		Empty,
		OtherProperty,
		Stringable,
		Object,
		Unknown,
	}

	// TODO Uno: IPropertyValue projection, the equivalent of try_as<IPropertyValue>(). The CsWinRT CCW of a .NET value
	// offers IPropertyValue only for WinRT scalars, string, Guid, DateTimeOffset, TimeSpan and projected structs/enums
	// (OtherType). decimal, System.DateTime, tuples and app structs/enums fall back to IStringable, as in WinUI.
	private static bool IsPropertyValue(object? value) =>
		value is not null && ValueConversionHelpers.TryGetPropertyType(value, out _);

	// TODO Uno: IPropertyValue projection, the equivalent of IPropertyValue::Type().
	private static PropertyType GetPropertyValueType(object propertyValue) =>
		ValueConversionHelpers.GetPropertyType(propertyValue.GetType());

	// TODO Uno: WinRT DateTime counts 100ns intervals since 1601-01-01 UTC; it projects as DateTimeOffset.
	private const long c_winrtDateTimeEpochTicks = 504911232000000000;

	// TODO Uno: IPropertyValue::GetDateTime().time_since_epoch().count().
	private static long GetDateTimeCount(object propertyValue) =>
		propertyValue switch
		{
			DateTimeOffset dateTimeOffset => dateTimeOffset.UtcTicks - c_winrtDateTimeEpochTicks,
			_ => 0,
		};

	// TODO Uno: IPropertyValue::GetTimeSpan().count(); both count 100ns ticks.
	private static long GetTimeSpanCount(object propertyValue) => ((TimeSpan)propertyValue).Ticks;

	// TODO Uno: the hstring operator< is an ordinal UTF-16 code unit comparison.
	private static bool HStringLess(string a, string b) => string.CompareOrdinal(a, b) < 0;

	private static NumericKind ClassifyNumeric(
		object propertyValue,
		ref long signedValue,
		ref ulong unsignedValue,
		ref double doubleValue)
	{
		switch (GetPropertyValueType(propertyValue))
		{
			case PropertyType.Int16:
				signedValue = (short)propertyValue;
				doubleValue = (double)signedValue;
				return NumericKind.Signed;
			case PropertyType.Int32:
				signedValue = (int)propertyValue;
				doubleValue = (double)signedValue;
				return NumericKind.Signed;
			case PropertyType.Int64:
				signedValue = (long)propertyValue;
				doubleValue = (double)signedValue;
				return NumericKind.Signed;
			case PropertyType.UInt8:
				unsignedValue = (byte)propertyValue;
				doubleValue = (double)unsignedValue;
				return NumericKind.Unsigned;
			case PropertyType.UInt16:
				unsignedValue = (ushort)propertyValue;
				doubleValue = (double)unsignedValue;
				return NumericKind.Unsigned;
			case PropertyType.UInt32:
				unsignedValue = (uint)propertyValue;
				doubleValue = (double)unsignedValue;
				return NumericKind.Unsigned;
			case PropertyType.UInt64:
				unsignedValue = (ulong)propertyValue;
				doubleValue = (double)unsignedValue;
				return NumericKind.Unsigned;
			case PropertyType.Single:
				doubleValue = (float)propertyValue;
				return NumericKind.Floating;
			case PropertyType.Double:
				doubleValue = (double)propertyValue;
				return NumericKind.Floating;
			default:
				return NumericKind.None;
		}
	}

	private static int CompareIntToDouble(long signedValue, double doubleValue)
	{
		if (double.IsNaN(doubleValue))
		{
			return -1;
		}

		const double int64LowerBoundAsDouble = -9223372036854775808.0;
		const double int64UpperBoundAsDouble = 9223372036854775808.0;
		if (doubleValue >= int64UpperBoundAsDouble)
		{
			return -1;
		}
		if (doubleValue < int64LowerBoundAsDouble)
		{
			return 1;
		}

		double floored = Math.Floor(doubleValue);
		var doubleAsInt = (long)floored;
		if (signedValue < doubleAsInt)
		{
			return -1;
		}
		if (signedValue > doubleAsInt)
		{
			return 1;
		}
		return doubleValue > floored ? -1 : 0;
	}

	private static int CompareUIntToDouble(ulong unsignedValue, double doubleValue)
	{
		if (double.IsNaN(doubleValue))
		{
			return -1;
		}

		const double uint64UpperBoundAsDouble = 18446744073709551616.0;
		if (doubleValue >= uint64UpperBoundAsDouble)
		{
			return -1;
		}
		if (doubleValue < 0.0)
		{
			return 1;
		}

		double floored = Math.Floor(doubleValue);
		var doubleAsUInt = (ulong)floored;
		if (unsignedValue < doubleAsUInt)
		{
			return -1;
		}
		if (unsignedValue > doubleAsUInt)
		{
			return 1;
		}
		return doubleValue > floored ? -1 : 0;
	}

	private static int CompareStrings(string a, string b)
	{
		// TODO Uno: a C# string length can never exceed INT_MAX, so the size guard always passes. CompareStringEx with
		// LOCALE_NAME_USER_DEFAULT and no flags maps to the current culture's CompareInfo with CompareOptions.None;
		// .NET has no separate "user default locale", so CurrentCulture stands in for it.
		if (a.Length <= int.MaxValue &&
			b.Length <= int.MaxValue)
		{
			// Original C++:
			// const int result = CompareStringEx(LOCALE_NAME_USER_DEFAULT, 0, a.c_str(), static_cast<int>(a.size()), b.c_str(), static_cast<int>(b.size()), nullptr, nullptr, 0);
			int result = CultureInfo.CurrentCulture.CompareInfo.Compare(a, b, CompareOptions.None);
			if (result < 0)
			{
				return -1;
			}
			if (result > 0)
			{
				return 1;
			}
			if (result == 0)
			{
				return 0;
			}
		}

		return HStringLess(a, b) ? -1 : (HStringLess(b, a) ? 1 : 0);
	}

	private static int CompareSamePropertyType(
		object a,
		object b)
	{
		switch (GetPropertyValueType(a))
		{
			case PropertyType.String:
				{
					string va = (string)a;
					string vb = (string)b;
					return CompareStrings(va, vb);
				}
			case PropertyType.Int16:
				return (short)a < (short)b ? -1 : ((short)a > (short)b ? 1 : 0);
			case PropertyType.Int32:
				return (int)a < (int)b ? -1 : ((int)a > (int)b ? 1 : 0);
			case PropertyType.Int64:
				return (long)a < (long)b ? -1 : ((long)a > (long)b ? 1 : 0);
			case PropertyType.UInt8:
				return (byte)a < (byte)b ? -1 : ((byte)a > (byte)b ? 1 : 0);
			case PropertyType.UInt16:
				return (ushort)a < (ushort)b ? -1 : ((ushort)a > (ushort)b ? 1 : 0);
			case PropertyType.UInt32:
				return (uint)a < (uint)b ? -1 : ((uint)a > (uint)b ? 1 : 0);
			case PropertyType.UInt64:
				return (ulong)a < (ulong)b ? -1 : ((ulong)a > (ulong)b ? 1 : 0);
			case PropertyType.Single:
				{
					var va = (float)a;
					var vb = (float)b;
					bool aNaN = float.IsNaN(va);
					bool bNaN = float.IsNaN(vb);
					if (aNaN || bNaN)
					{
						return aNaN == bNaN ? 0 : (aNaN ? 1 : -1);
					}
					return va < vb ? -1 : (va > vb ? 1 : 0);
				}
			case PropertyType.Double:
				{
					var va = (double)a;
					var vb = (double)b;
					bool aNaN = double.IsNaN(va);
					bool bNaN = double.IsNaN(vb);
					if (aNaN || bNaN)
					{
						return aNaN == bNaN ? 0 : (aNaN ? 1 : -1);
					}
					return va < vb ? -1 : (va > vb ? 1 : 0);
				}
			case PropertyType.Boolean:
				return (bool)a == (bool)b ? 0 : ((bool)a ? 1 : -1);
			case PropertyType.Char16:
				return (char)a < (char)b ? -1 : ((char)a > (char)b ? 1 : 0);
			case PropertyType.DateTime:
				return GetDateTimeCount(a) < GetDateTimeCount(b) ? -1 :
					(GetDateTimeCount(a) > GetDateTimeCount(b) ? 1 : 0);
			case PropertyType.TimeSpan:
				return GetTimeSpanCount(a) < GetTimeSpanCount(b) ? -1 :
					(GetTimeSpanCount(a) > GetTimeSpanCount(b) ? 1 : 0);
			case PropertyType.Guid:
				// Original C++ compares Data1, Data2, Data3 (unsigned), then Data4[0..7] in order;
				// System.Guid.CompareTo walks the same fields in the same order.
				return Math.Sign(((Guid)a).CompareTo((Guid)b));
			case PropertyType.Point:
				{
					var va = (Point)a;
					var vb = (Point)b;
					int x = CompareFloating(va.X, vb.X);
					return x != 0 ? x : CompareFloating(va.Y, vb.Y);
				}
			case PropertyType.Size:
				{
					var va = (Size)a;
					var vb = (Size)b;
					int width = CompareFloating(va.Width, vb.Width);
					return width != 0 ? width : CompareFloating(va.Height, vb.Height);
				}
			case PropertyType.Rect:
				{
					var va = (Rect)a;
					var vb = (Rect)b;
					int x = CompareFloating(va.X, vb.X);
					if (x != 0)
					{
						return x;
					}
					int y = CompareFloating(va.Y, vb.Y);
					if (y != 0)
					{
						return y;
					}
					int width = CompareFloating(va.Width, vb.Width);
					return width != 0 ? width : CompareFloating(va.Height, vb.Height);
				}
			default:
				return CompareObjectLookupKeys(a, b);
		}
	}

	private static int ComparePropertyValues(
		object a,
		object b)
	{
		if (GetPropertyValueType(a) == GetPropertyValueType(b))
		{
			return CompareSamePropertyType(a, b);
		}

		long signedA = 0;
		long signedB = 0;
		ulong unsignedA = 0;
		ulong unsignedB = 0;
		double doubleA = 0;
		double doubleB = 0;
		var kindA = ClassifyNumeric(a, ref signedA, ref unsignedA, ref doubleA);
		var kindB = ClassifyNumeric(b, ref signedB, ref unsignedB, ref doubleB);
		if (kindA == NumericKind.None || kindB == NumericKind.None)
		{
			return 0;
		}

		if (kindA == NumericKind.Signed && kindB == NumericKind.Signed)
		{
			return signedA < signedB ? -1 : (signedA > signedB ? 1 : 0);
		}
		if (kindA == NumericKind.Unsigned && kindB == NumericKind.Unsigned)
		{
			return unsignedA < unsignedB ? -1 : (unsignedA > unsignedB ? 1 : 0);
		}
		if (kindA != NumericKind.Floating && kindB != NumericKind.Floating)
		{
			bool aNegative = kindA == NumericKind.Signed && signedA < 0;
			bool bNegative = kindB == NumericKind.Signed && signedB < 0;
			if (aNegative != bNegative)
			{
				return aNegative ? -1 : 1;
			}

			ulong va = kindA == NumericKind.Signed ? unchecked((ulong)signedA) : unsignedA;
			ulong vb = kindB == NumericKind.Signed ? unchecked((ulong)signedB) : unsignedB;
			return va < vb ? -1 : (va > vb ? 1 : 0);
		}

		{
			bool aNaN = double.IsNaN(doubleA);
			bool bNaN = double.IsNaN(doubleB);
			if (aNaN || bNaN)
			{
				return aNaN == bNaN ? 0 : (aNaN ? 1 : -1);
			}
		}
		if (kindA == NumericKind.Signed)
		{
			return CompareIntToDouble(signedA, doubleB);
		}
		if (kindB == NumericKind.Signed)
		{
			return -CompareIntToDouble(signedB, doubleA);
		}
		if (kindA == NumericKind.Unsigned)
		{
			return CompareUIntToDouble(unsignedA, doubleB);
		}
		if (kindB == NumericKind.Unsigned)
		{
			return -CompareUIntToDouble(unsignedB, doubleA);
		}
		return doubleA < doubleB ? -1 : (doubleA > doubleB ? 1 : 0);
	}

	private static ValueClassRank GetPropertyValueClassRank(object propertyValue)
	{
		switch (GetPropertyValueType(propertyValue))
		{
			case PropertyType.Int16:
			case PropertyType.Int32:
			case PropertyType.Int64:
			case PropertyType.UInt8:
			case PropertyType.UInt16:
			case PropertyType.UInt32:
			case PropertyType.UInt64:
			case PropertyType.Single:
			case PropertyType.Double:
				return ValueClassRank.Numeric;
			case PropertyType.String:
				return ValueClassRank.String;
			case PropertyType.Guid:
				return ValueClassRank.Guid;
			case PropertyType.Boolean:
				return ValueClassRank.Boolean;
			case PropertyType.Char16:
				return ValueClassRank.Char16;
			case PropertyType.DateTime:
				return ValueClassRank.DateTime;
			case PropertyType.TimeSpan:
				return ValueClassRank.TimeSpan;
			case PropertyType.Empty:
				return ValueClassRank.Empty;
			default:
				return ValueClassRank.OtherProperty;
		}
	}

	private static ValueClassRank GetValueClassRank(object? value)
	{
		if (IsPropertyValue(value))
		{
			return GetPropertyValueClassRank(value!);
		}
		// TODO Uno: IStringable projection. Every managed object reaches native code through a CsWinRT CCW that
		// implements IStringable over Object.ToString, so any non-null reference ranks as Stringable.
		// Original C++: if (value.try_as<winrt::Windows::Foundation::IStringable>())
		if (value is not null)
		{
			return ValueClassRank.Stringable;
		}
		// Original C++:
		// if (value.try_as<::IUnknown>())
		// {
		//     return ValueClassRank::Object;
		// }
		return ValueClassRank.Unknown;
	}

	private static int CompareValueClassRank(ValueClassRank a, ValueClassRank b) => a < b ? -1 : (a > b ? 1 : 0);

	private static int CompareFloating(double a, double b)
	{
		bool aNaN = double.IsNaN(a);
		bool bNaN = double.IsNaN(b);
		if (aNaN || bNaN)
		{
			return aNaN == bNaN ? 0 : (aNaN ? 1 : -1);
		}
		return a < b ? -1 : (a > b ? 1 : 0);
	}

	private static int CompareObjectLookupKeys(object? a, object? b)
	{
		var keyA = ValueKey.ToObjectLookupKey(a);
		var keyB = ValueKey.ToObjectLookupKey(b);
		return HStringLess(keyA, keyB) ? -1 : (HStringLess(keyB, keyA) ? 1 : 0);
	}

	// TODO Uno: std::stable_sort has no BCL equivalent (List<T>.Sort is unstable). A stable sort's output is fully
	// determined by a strict weak ordering, so this bottom-up merge sort yields the same order as std::stable_sort.
	private static void StableSort<T>(List<T> items, Func<T, T, bool> less)
	{
		if (items.Count < 2)
		{
			return;
		}

		var current = items.ToArray();
		var scratch = new T[current.Length];
		for (int width = 1; width < current.Length; width *= 2)
		{
			for (int lo = 0; lo < current.Length; lo += 2 * width)
			{
				int mid = Math.Min(lo + width, current.Length);
				int hi = Math.Min(lo + 2 * width, current.Length);
				int left = lo;
				int right = mid;
				int output = lo;
				while (left < mid && right < hi)
				{
					// Take from the right run only when it is strictly less, which keeps equal elements in order.
					scratch[output++] = less(current[right], current[left]) ? current[right++] : current[left++];
				}
				while (left < mid)
				{
					scratch[output++] = current[left++];
				}
				while (right < hi)
				{
					scratch[output++] = current[right++];
				}
			}
			(current, scratch) = (scratch, current);
		}

		for (int i = 0; i < current.Length; i++)
		{
			items[i] = current[i];
		}
	}

	// } // namespace

	static partial class ValueKey
	{
		internal static partial bool TryFormatPropertyValue(
			object propertyValue,
			out string key,
			bool rejectEmptyString)
		{
			key = "";
			switch (GetPropertyValueType(propertyValue))
			{
				case PropertyType.Empty:
					key = "<empty>";
					return true;
				case PropertyType.String:
					{
						var value = (string)propertyValue;
						if (rejectEmptyString && string.IsNullOrEmpty(value))
						{
							return false;
						}
						key = "s:" + value;
						return true;
					}
				case PropertyType.Int16:
					key = "i16:" + ((short)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.Int32:
					key = "i32:" + ((int)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.Int64:
					key = "i64:" + ((long)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.UInt8:
					key = "u8:" + ((byte)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.UInt16:
					key = "u16:" + ((ushort)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.UInt32:
					key = "u32:" + ((uint)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.UInt64:
					key = "u64:" + ((ulong)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.Single:
					{
						var value = (float)propertyValue;
						uint bits = BitConverter.SingleToUInt32Bits(value);
						// Original C++: swprintf_s(buffer, L"f32:%08X", bits);
						key = "f32:" + bits.ToString("X8", CultureInfo.InvariantCulture);
						return true;
					}
				case PropertyType.Double:
					{
						var value = (double)propertyValue;
						ulong bits = BitConverter.DoubleToUInt64Bits(value);
						// Original C++: swprintf_s(buffer, L"f64:%016llX", static_cast<unsigned long long>(bits));
						key = "f64:" + bits.ToString("X16", CultureInfo.InvariantCulture);
						return true;
					}
				case PropertyType.Boolean:
					key = (bool)propertyValue ? "b:1" : "b:0";
					return true;
				case PropertyType.Char16:
					key = "c:" + ((ushort)(char)propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.DateTime:
					key = "dt:" + GetDateTimeCount(propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.TimeSpan:
					key = "ts:" + GetTimeSpanCount(propertyValue).ToString(CultureInfo.InvariantCulture);
					return true;
				case PropertyType.Guid:
					// Original C++: swprintf_s(buffer, 40, L"g:%08X-%04X-%04X-%02X%02X-%02X%02X%02X%02X%02X%02X", ...);
					key = "g:" + ((Guid)propertyValue).ToString("D").ToUpperInvariant();
					return true;
				default:
					return false;
			}
		}

		internal static partial bool TryGetStablePropertyKey(
			object? value,
			out string key,
			bool rejectEmptyString)
		{
			key = "";
			if (value is null)
			{
				return false;
			}

			if (IsPropertyValue(value))
			{
				return TryFormatPropertyValue(value, out key, rejectEmptyString);
			}

			return false;
		}

		internal static partial string ToString(object? value)
		{
			if (value is null)
			{
				return "<null>";
			}

			if (TryGetStablePropertyKey(value, out var key, false))
			{
				return key;
			}

			// TODO Uno: IStringable projection. Every managed object is IStringable through its CsWinRT CCW
			// (Object.ToString); an explicit IStringable implementation wins when present.
			// Original C++: if (auto stringable = value.try_as<winrt::Windows::Foundation::IStringable>())
			if (value is not null)
			{
				try
				{
					return "x:" + (value is IStringable stringable ? stringable.ToString() : value.ToString());
				}
				catch
				{
				}
			}

			// Original C++: if (auto unknown = value.try_as<::IUnknown>())
			if (value is not null)
			{
				// TODO Uno: there is no IUnknown address in .NET; ObjectIdentityHelper hands out a stable id per live object.
				// Original C++: swprintf_s(buffer, 40, L"o:%016llX", static_cast<unsigned long long>(reinterpret_cast<uintptr_t>(unknown.get())));
				return "o:" + ObjectIdentityHelper.GetId(value).ToString("X16", CultureInfo.InvariantCulture);
			}

			return "<unknown>";
		}

		internal static partial string ToObjectLookupKey(object? value, bool rejectEmptyString)
		{
			if (value is null)
			{
				return "null:";
			}

			if (TryGetStablePropertyKey(value, out var key, rejectEmptyString))
			{
				return "value:" + key;
			}

			// Original C++: if (auto unknown = value.try_as<::IUnknown>())
			if (value is not null)
			{
				// TODO Uno: there is no IUnknown address in .NET; ObjectIdentityHelper hands out a stable id per live object.
				// MSVC formats %p on x64 as 16 upper-case hex digits.
				// Original C++: swprintf_s(buffer, 32, L"object:%p", unknown.get());
				return "object:" + ObjectIdentityHelper.GetId(value).ToString("X16", CultureInfo.InvariantCulture);
			}

			return "";
		}
	}

	static partial class ValueComparer
	{
		internal static partial bool UsesFallbackKey(object? value) => value is not null && !IsPropertyValue(value);

		internal static partial int Compare(object? a, object? b) => Compare(a, b, null, null);

		internal static partial int Compare(
			object? a,
			object? b,
			string? fallbackKeyA,
			string? fallbackKeyB)
		{
			if (a is null && b is null)
			{
				return 0;
			}
			if (a is null)
			{
				return -1;
			}
			if (b is null)
			{
				return 1;
			}

			if (IsPropertyValue(a) && IsPropertyValue(b))
			{
				var propertyA = a;
				var propertyB = b;
				if (GetPropertyValueType(propertyA) == GetPropertyValueType(propertyB))
				{
					return ComparePropertyValues(propertyA, propertyB);
				}

				long signedA = 0;
				long signedB = 0;
				ulong unsignedA = 0;
				ulong unsignedB = 0;
				double doubleA = 0;
				double doubleB = 0;
				if (ClassifyNumeric(propertyA, ref signedA, ref unsignedA, ref doubleA) != NumericKind.None &&
					ClassifyNumeric(propertyB, ref signedB, ref unsignedB, ref doubleB) != NumericKind.None)
				{
					return ComparePropertyValues(propertyA, propertyB);
				}

				var propertyRankA = GetPropertyValueClassRank(propertyA);
				var propertyRankB = GetPropertyValueClassRank(propertyB);
				if (propertyRankA != propertyRankB)
				{
					return CompareValueClassRank(propertyRankA, propertyRankB);
				}

				return GetPropertyValueType(propertyA) < GetPropertyValueType(propertyB) ? -1 : 1;
			}

			var rankA = GetValueClassRank(a);
			var rankB = GetValueClassRank(b);
			if (rankA != rankB)
			{
				return CompareValueClassRank(rankA, rankB);
			}

			string keyA = fallbackKeyA is not null ? fallbackKeyA : ValueKey.ToString(a);
			string keyB = fallbackKeyB is not null ? fallbackKeyB : ValueKey.ToString(b);
			return HStringLess(keyA, keyB) ? -1 : (HStringLess(keyB, keyA) ? 1 : 0);
		}
	}

	internal static partial List<object?> EnumerateInspectableItems(
		object? source,
		bool throwIfUnsupported)
	{
		// Single entry point for callers holding a raw source. The classification and the walk both
		// live on CollectionAccessor so there is exactly one interface ladder in the stack.
		return new CollectionAccessor(source).Enumerate(throwIfUnsupported);
	}

	partial struct CollectionAccessor
	{
		public CollectionAccessor(object? source)
		{
			m_source = source;

			if (source is null)
			{
				return;
			}

			// Read resolution, cheapest indexed call first. IVectorView sits above IBindableVector
			// because a projected view is a direct call where the bindable interface goes through the
			// XAML interop shim.
			// TODO Uno: IVector<IInspectable> projects as IList<object>, IVectorView<IInspectable> as IReadOnlyList<object>
			// (covariant, so a List<T> of a reference type lands here) and IBindableVector as System.Collections.IList.
			// A string is a boxed HSTRING in WinRT, not a collection, so it never resolves as one.
			if (source is string)
			{
			}
			else if (source is IList<object?> vector)
			{
				m_vector = vector;
			}
			else if (source is IReadOnlyList<object?> vectorView)
			{
				m_vectorView = vectorView;
			}
			else if (source is _IBindableVector bindableVector)
			{
				m_bindableVector = bindableVector;
			}

			// Observe resolution, independent of the above: a source can be indexable through one
			// interface and observable through another.
			if (source is INotifyCollectionChanged notifyCollectionChanged)
			{
				m_notifyCollectionChanged = notifyCollectionChanged;
			}
			else if (source is IObservableVector<object?> observableVector)
			{
				m_observableVector = observableVector;
			}
			else if (source is IBindableObservableVector bindableObservableVector)
			{
				m_bindableObservableVector = bindableObservableVector;
			}
		}

		public partial bool IsIndexable() => m_vector is not null || m_vectorView is not null || m_bindableVector is not null;

		public partial uint Count()
		{
			if (m_vector is not null)
			{
				return (uint)m_vector.Count;
			}
			if (m_vectorView is not null)
			{
				return (uint)m_vectorView.Count;
			}
			if (m_bindableVector is not null)
			{
				return (uint)m_bindableVector.Count;
			}
			return 0;
		}

		private partial object? GetAtUnchecked(uint index)
		{
			if (m_vector is not null)
			{
				return m_vector[(int)index];
			}
			if (m_vectorView is not null)
			{
				return m_vectorView[(int)index];
			}
			return m_bindableVector![(int)index];
		}

		public partial bool TryGetAt(uint index, out object? item)
		{
			item = null;
			if (!IsIndexable() || index >= Count())
			{
				return false;
			}

			item = GetAtUnchecked(index);
			return true;
		}

		public partial List<object?> Enumerate(bool throwIfUnsupported)
		{
			List<object?> result = new();
			if (m_source is null)
			{
				return result;
			}

			// Indexed sources are walked positionally instead of through an iterator. Besides being the
			// cheaper path, it avoids the managed-iterator problem handled below entirely: a source can
			// have a working indexed surface and an unusable iterator.
			if (IsIndexable())
			{
				uint size = Count();
				result.Capacity = (int)size;
				for (uint i = 0; i < size; ++i)
				{
					result.Add(GetAtUnchecked(i));
				}
				return result;
			}

			// Enumerate-only sources: no indexed surface, so the iterator is the only way in.
			// TODO Uno: IIterable<IInspectable> projects as IEnumerable<object>, IBindableIterable as System.Collections.IEnumerable.
			// A string is a boxed HSTRING in WinRT, not an iterable.
			if (m_source is not string && m_source is IEnumerable<object?> iterable)
			{
				try
				{
					foreach (var item in iterable)
					{
						result.Add(item);
					}
					return result;
				}
				// TODO Uno: E_NOINTERFACE surfaces in .NET as InvalidCastException.
				// Original C++: catch (winrt::hresult_error const& e) { if (e.code() != E_NOINTERFACE) { throw; } ... }
				catch (InvalidCastException)
				{
					// CLR compatibility: some managed sources project IIterable<Object>, but their
					// iterator returns E_NOINTERFACE from First()/HasCurrent(). Legacy XAML
					// (CollectionViewManager) retries such sources through IBindableIterable; mirror
					// that here. Discard any partial enumeration and fall through to the bindable
					// branch below. Any other failure is a genuine error and is rethrown.
					result.Clear();
				}
			}

			if (m_source is not string && m_source is global::System.Collections.IEnumerable bindableIterable)
			{
				var iterator = bindableIterable.GetEnumerator();
				// Original C++: while (iterator && iterator.HasCurrent()) { push_back(Current()); if (!iterator.MoveNext()) break; }
				// TODO Uno: a .NET enumerator starts before the first element, so MoveNext() is HasCurrent() for the first step.
				while (iterator is not null && iterator.MoveNext())
				{
					result.Add(iterator.Current);
				}
				return result;
			}

			if (throwIfUnsupported)
			{
				throw new ArgumentException(
					"items must implement a supported collection interface: IVector<IInspectable>, " +
					"IBindableVector, IIterable<IInspectable>, or IBindableIterable.");
			}

			return result;
		}
	}

	internal static partial void ApplyPredicateFilter(
		List<object?> items,
		Predicate? predicate)
	{
		if (predicate is null)
		{
			return;
		}

		List<object?> kept = new(items.Count);
		foreach (var item in items)
		{
			bool keep = false;
			try
			{
				keep = predicate(item);
			}
			catch
			{
				keep = false;
			}

			if (keep)
			{
				kept.Add(item);
			}
		}
		items.Clear();
		items.AddRange(kept);
	}

	internal static partial void StableSortByKeys(
		List<object?> items,
		int axisCount,
		Func<object?, int, object?> extractKey,
		Func<int, SortDirection> axisDirection)
	{
		if (axisCount == 0 || items.Count < 2)
		{
			return;
		}

		// Decorate-sort-undecorate: extract each item's keys once up front (key extraction can be
		// O(reflection)), then run a cheap stable_sort over an index permutation. stable_sort
		// preserves the relative order of equal-key items (indices start in original order).
		//
		// The decoration is held as flat row-major arrays rather than a per-row struct of three
		// vectors: at 100k rows the latter is ~300k separate heap allocations before the sort even
		// starts, which dominates the sort itself.
		//
		// Reference keys fall through to a ToString-based tiebreak that may call app code.
		// FallbackKeys materializes it once here so the comparator stays cheap and, more
		// importantly, sees a frozen key set — a non-deterministic app ToString would otherwise
		// break the strict-weak-ordering precondition of stable_sort. The entry is empty (and its
		// HasFallbackKey flag false) for IPropertyValue keys, which never reach that tiebreak.
		int n = items.Count;
		var keys = new object?[n * axisCount];
		var fallbackKeys = new string[n * axisCount];
		var hasFallbackKey = new bool[n * axisCount];

		for (int i = 0; i < n; ++i)
		{
			int rowBase = i * axisCount;
			for (int s = 0; s < axisCount; ++s)
			{
				var key = extractKey(items[i], s);
				bool needsFallback = ValueComparer.UsesFallbackKey(key);
				if (needsFallback)
				{
					fallbackKeys[rowBase + s] = ValueKey.ToString(key);
				}
				hasFallbackKey[rowBase + s] = needsFallback;
				keys[rowBase + s] = key;
			}
		}

		List<int> order = new(n);
		for (int i = 0; i < n; ++i)
		{
			order.Add(i);
		}

		// TODO Uno: std::stable_sort is StableSort (see its note).
		StableSort(order,
			(a, b) =>
			{
				int aBase = a * axisCount;
				int bBase = b * axisCount;
				for (int s = 0; s < axisCount; ++s)
				{
					int cmp = ValueComparer.Compare(
						keys[aBase + s],
						keys[bBase + s],
						hasFallbackKey[aBase + s] ? fallbackKeys[aBase + s] : null,
						hasFallbackKey[bBase + s] ? fallbackKeys[bBase + s] : null);
					if (cmp != 0)
					{
						return axisDirection(s) == SortDirection.Ascending ? (cmp < 0) : (cmp > 0);
					}
				}
				return false;
			});

		List<object?> sorted = new(n);
		foreach (var index in order)
		{
			sorted.Add(items[index]);
		}
		items.Clear();
		items.AddRange(sorted);
	}

	internal static partial bool BucketizeToGroups(
		List<object?> items,
		KeySelector? resolveKey,
		ResolveIdentityCallback? resolveIdentity,
		Func<object?, object?, bool>? keysConsideredEqual,
		List<KeyedBucket> outBuckets,
		ref string? degradeReason)
	{
		outBuckets.Clear();
		// identity string -> index into outBuckets, so first-seen group order is preserved by
		// outBuckets itself (no separate order vector needed).
		Dictionary<string, int> indexByIdentity = new(StringComparer.Ordinal);

		foreach (var item in items)
		{
			object? key = resolveKey is not null ? resolveKey(item) : null;

			string identity = "";
			string? reason = null;
			if (resolveIdentity is null || !resolveIdentity(key, ref identity, ref reason))
			{
				degradeReason = reason;
				return false;
			}

			if (!indexByIdentity.TryGetValue(identity, out var bucketIndex))
			{
				indexByIdentity.Add(identity, outBuckets.Count);
				outBuckets.Add(new KeyedBucket { Key = key, Identity = identity, Items = new() { item } });
			}
			else
			{
				var bucket = outBuckets[bucketIndex];
				// A genuine identity COLLISION (two logically-different keys mapping to the same
				// identity string) forces a flat degrade; keysConsideredEqual lets the adapter
				// treat intentional shared identities (e.g. an app-supplied identity selector) as
				// the same group instead.
				if (keysConsideredEqual is not null && !keysConsideredEqual(bucket.Key, key))
				{
					degradeReason = "duplicate group identity";
					return false;
				}
				bucket.Items.Add(item);
			}
		}

		return true;
	}

	internal static partial uint UpperBoundInsertIndex(
		uint count,
		Func<uint, int> compareNewToExisting)
	{
		uint lo = 0;
		uint hi = count;
		while (lo < hi)
		{
			uint mid = lo + (hi - lo) / 2;
			if (compareNewToExisting(mid) < 0)
			{
				hi = mid;
			}
			else
			{
				lo = mid + 1;
			}
		}
		return lo;
	}

	sealed partial class CustomSortRankAdapter
	{
		private partial int SafeCompare(
			object? left,
			object? right)
		{
			if (m_comparer is null)
			{
				return 0;
			}

			try
			{
				var result = m_comparer(left, right);
				return result < 0 ? -1 : (result > 0 ? 1 : 0);
			}
			catch
			{
				return 0;
			}
		}

		private partial bool IndexComesBefore(
			List<object?> rows,
			ulong generation,
			int leftIndex,
			int rightIndex,
			ref bool staleState)
		{
			if (leftIndex == rightIndex)
			{
				return false;
			}

			int comparison = SafeCompare(rows[leftIndex], rows[rightIndex]);
			if (m_generation != generation)
			{
				staleState = true;
				return false;
			}
			if (comparison != 0)
			{
				return comparison < 0;
			}

			// Equal keys keep source order, which is what makes the sort stable.
			return leftIndex < rightIndex;
		}

		private partial bool StableMergeSortOrder(
			List<int> order,
			List<object?> rows,
			ulong generation)
		{
			// TODO Uno: order.resize + std::iota, then order.swap(scratch) per pass. The passes run over arrays and the
			// result is copied back into `order`, the caller's vector.
			order.Clear();
			for (int i = 0; i < rows.Count; i++)
			{
				order.Add(i);
			}
			if (order.Count < 2)
			{
				return true;
			}

			var current = order.ToArray();
			var scratch = new int[current.Length];
			for (int width = 1; width < current.Length; width *= 2)
			{
				for (int lo = 0; lo < current.Length; lo += 2 * width)
				{
					int mid = Math.Min(lo + width, current.Length);
					int hi = Math.Min(lo + 2 * width, current.Length);
					int left = lo;
					int right = mid;
					int output = lo;
					while (left < mid && right < hi)
					{
						bool staleState = false;
						bool takeRight = IndexComesBefore(rows, generation, current[right], current[left], ref staleState);
						if (staleState)
						{
							return false;
						}
						scratch[output++] = takeRight ? current[right++] : current[left++];
					}
					while (left < mid)
					{
						scratch[output++] = current[left++];
					}
					while (right < hi)
					{
						scratch[output++] = current[right++];
					}
				}
				(current, scratch) = (scratch, current);
			}

			for (int i = 0; i < current.Length; i++)
			{
				order[i] = current[i];
			}

			return true;
		}

		private partial void ClearRanks()
		{
			++m_generation;
			m_ranks.Clear();
			m_rankByIdentity.Clear();
		}

		public partial void Reset()
		{
			m_comparer = null;
			ClearRanks();
		}

		public partial void Rank(
			PairwiseComparer? comparer,
			List<object?> rows)
		{
			// Set the flag before ClearRanks so a reentrant path cannot observe a torn intermediate.
			if (m_comparerActive)
			{
				MUX_ASSERT(false, "TableView custom sort comparer re-entered rank population. A sort predicate must be a pure function of its inputs.");
				return;
			}
			m_comparerActive = true;
			try
			{
				m_comparer = comparer;
				ClearRanks();

				if (m_comparer is null)
				{
					return;
				}
				var generation = m_generation;

				List<int> order = new();
				if (!StableMergeSortOrder(order, rows, generation))
				{
					return;
				}

				// Rank, not position: equal items must share a rank or the projection would impose an
				// arbitrary order on a tie the comparer called equal.
				int rank = 0;
				for (int i = 0; i < order.Count; ++i)
				{
					if (i > 0)
					{
						int comparison = SafeCompare(rows[order[i - 1]], rows[order[i]]);
						if (m_generation != generation)
						{
							return;
						}
						if (comparison != 0)
						{
							++rank;
						}
					}

					var item = rows[order[i]];
					m_ranks.Add(new RankEntry { Item = item, Rank = rank });
					// Original C++: if (auto unknown = item.try_as<::IUnknown>())
					if (item is not null)
					{
						m_rankByIdentity[item] = rank;
					}
				}
			}
			finally
			{
				m_comparerActive = false;
			}
		}

		public partial object? KeyFor(object? item)
		{
			if (m_comparer is null)
			{
				return null;
			}

			if (m_comparerActive)
			{
				MUX_ASSERT(false, "TableView custom sort comparer re-entered the sort infrastructure. A sort predicate must be a pure function of its inputs.");
				return null;
			}
			m_comparerActive = true;
			try
			{
				var generation = m_generation;

				// Original C++: if (auto unknown = item.try_as<::IUnknown>())
				if (item is not null)
				{
					if (m_rankByIdentity.TryGetValue(item, out var found))
					{
						return found;
					}
				}

				// A row the rank pass never saw - added after the sort was applied. Find where it belongs
				// among the existing ranks and insert it there, shifting the ranks above it.
				int rank = 0;
				for (int i = 0; i < m_ranks.Count; ++i)
				{
					var entryItem = m_ranks[i].Item;
					var entryRank = m_ranks[i].Rank;
					int comparison = SafeCompare(entryItem, item);
					if (m_generation != generation)
					{
						return null;
					}
					if (comparison == 0)
					{
						return entryRank;
					}
					if (comparison < 0)
					{
						rank = Math.Max(rank, entryRank + 1);
					}
				}

				if (m_generation != generation)
				{
					return null;
				}

				for (int i = 0; i < m_ranks.Count; i++)
				{
					var entry = m_ranks[i];
					if (entry.Rank >= rank)
					{
						++entry.Rank;
						m_ranks[i] = entry;
						// Original C++: if (auto unknown = entry.Item.try_as<::IUnknown>())
						if (entry.Item is not null)
						{
							m_rankByIdentity[entry.Item] = entry.Rank;
						}
					}
				}

				m_ranks.Add(new RankEntry { Item = item, Rank = rank });
				// Original C++: if (auto unknown = item.try_as<::IUnknown>())
				if (item is not null)
				{
					m_rankByIdentity[item] = rank;
				}
				return rank;
			}
			finally
			{
				m_comparerActive = false;
			}
		}
	}
}
