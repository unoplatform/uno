#nullable enable

using System;

namespace Windows.Foundation;

/// <summary>
/// Represents a value in a property store (such as a PropertySet instance).
/// </summary>
public static partial class PropertyValue
{
	public static object? CreateEmpty() => null;

	public static object CreateUInt8(byte value) => value;

	public static object CreateInt16(short value) => value;

	public static object CreateUInt16(ushort value) => value;

	public static object CreateInt32(int value) => value;

	public static object CreateUInt32(uint value) => value;

	public static object CreateInt64(long value) => value;

	public static object CreateUInt64(ulong value) => value;

	public static object CreateSingle(float value) => value;

	public static object CreateDouble(double value) => value;

	public static object CreateChar16(char value) => value;

	public static object CreateBoolean(bool value) => value;

	public static object CreateString(string value) => value;

	public static object CreateInspectable(object value)
	{
		if (value is null)
		{
			throw new ArgumentNullException(nameof(value));
		}

		return value;
	}

	public static object CreateGuid(Guid value) => value;

	public static object CreateDateTime(DateTimeOffset value) => value;

	public static object CreateTimeSpan(TimeSpan value) => value;

	public static object CreatePoint(Point value) => value;

	public static object CreateSize(Size value) => value;

	public static object CreateRect(Rect value) => value;

	public static object CreateUInt8Array(byte[] value) => value ?? Array.Empty<byte>();

	public static object CreateInt16Array(short[] value) => value ?? Array.Empty<short>();

	public static object CreateUInt16Array(ushort[] value) => value ?? Array.Empty<ushort>();

	public static object CreateInt32Array(int[] value) => value ?? Array.Empty<int>();

	public static object CreateUInt32Array(uint[] value) => value ?? Array.Empty<uint>();

	public static object CreateInt64Array(long[] value) => value ?? Array.Empty<long>();

	public static object CreateUInt64Array(ulong[] value) => value ?? Array.Empty<ulong>();

	public static object CreateSingleArray(float[] value) => value ?? Array.Empty<float>();

	public static object CreateDoubleArray(double[] value) => value ?? Array.Empty<double>();

	public static object CreateChar16Array(char[] value) => value ?? Array.Empty<char>();

	public static object CreateBooleanArray(bool[] value) => value ?? Array.Empty<bool>();

	public static object CreateStringArray(string[] value) => value ?? Array.Empty<string>();

	public static object CreateInspectableArray(object[] value) => value ?? Array.Empty<object>();

	public static object CreateGuidArray(Guid[] value) => value ?? Array.Empty<Guid>();

	public static object CreateDateTimeArray(DateTimeOffset[] value) => value ?? Array.Empty<DateTimeOffset>();

	public static object CreateTimeSpanArray(TimeSpan[] value) => value ?? Array.Empty<TimeSpan>();

	public static object CreatePointArray(Point[] value) => value ?? Array.Empty<Point>();

	public static object CreateSizeArray(Size[] value) => value ?? Array.Empty<Size>();

	public static object CreateRectArray(Rect[] value) => value ?? Array.Empty<Rect>();
}
