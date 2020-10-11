using System;

namespace Windows.Foundation
{
	public partial class PropertyValue
	{
		public static object CreateEmpty() => null;

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

		public static object CreateInspectable(object value) => value;

		public static object CreateGuid(Guid value) => value;

		public static object CreateDateTime(DateTimeOffset value) => value;

		public static object CreateTimeSpan(TimeSpan value) => value;

		public static object CreatePoint(Point value) => value;

		public static object CreateSize(Size value) => value;

		public static object CreateRect(Rect value) => value;

		public static object CreateUInt8Array(byte[] value) => value;

		public static object CreateInt16Array(short[] value) => value;

		public static object CreateUInt16Array(ushort[] value) => value;

		public static object CreateInt32Array(int[] value) => value;

		public static object CreateUInt32Array(uint[] value) => value;

		public static object CreateInt64Array(long[] value) => value;

		public static object CreateUInt64Array(ulong[] value) => value;

		public static object CreateSingleArray(float[] value) => value;

		public static object CreateDoubleArray(double[] value) => value;

		public static object CreateChar16Array(char[] value) => value;

		public static object CreateBooleanArray(bool[] value) => value;

		public static object CreateStringArray(string[] value) => value;

		public static object CreateInspectableArray(object[] value) => value;

		public static object CreateGuidArray(Guid[] value) => value;

		public static object CreateDateTimeArray(DateTimeOffset[] value) => value;

		public static object CreateTimeSpanArray(TimeSpan[] value) => value;

		public static object CreatePointArray(Point[] value) => value;

		public static object CreateSizeArray(Size[] value) => value;

		public static object CreateRectArray(Rect[] value) => value;
	}
}
