#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Foundation
{
	public partial interface IPropertyValue
	{
		/// <summary>Gets a value that indicates whether the property value is a scalar value.</summary>
		/// <returns>True if the value is scalar; otherwise false.</returns>
		bool IsNumericScalar
		{
			get;
		}

		PropertyType Type
		{
			get;
		}

		byte GetUInt8();

		short GetInt16();

		ushort GetUInt16();

		int GetInt32();

		uint GetUInt32();

		long GetInt64();

		ulong GetUInt64();

		float GetSingle();

		/// <summary>Returns the floating-point value stored as a property value.</summary>
		/// <returns>The value.</returns>
		double GetDouble();

		/// <summary>Returns the Unicode character stored as a property value.</summary>
		/// <returns>The value.</returns>
		char GetChar16();

		/// <summary>Returns the Boolean value stored as a property value.</summary>
		/// <returns>The value.</returns>
		bool GetBoolean();

		string GetString();

		/// <summary>Returns the GUID value stored as a property value.</summary>
		/// <returns>The value.</returns>
		Guid GetGuid();

		/// <summary>Returns the date and time value stored as a property value.</summary>
		/// <returns>The value.</returns>
		DateTimeOffset GetDateTime();

		TimeSpan GetTimeSpan();

		Point GetPoint();

		Size GetSize();

		Rect GetRect();

		/// <summary>Returns the array of byte values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetUInt8Array(out byte[] value);

		/// <summary>Returns the array of integer values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetInt16Array(out short[] value);

		/// <summary>Returns the array of unsigned integer values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetUInt16Array(out ushort[] value);

		/// <summary>Returns the array of integer values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetInt32Array(out int[] value);

		/// <summary>Returns the array of unsigned integer values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetUInt32Array(out uint[] value);

		/// <summary>Returns the array of integer values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetInt64Array(out long[] value);

		/// <summary>Returns the array of unsigned integer values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetUInt64Array(out ulong[] value);

		/// <summary>Returns the array of floating-point values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetSingleArray(out float[] value);

		void GetDoubleArray(out double[] value);

		void GetChar16Array(out char[] value);

		void GetBooleanArray(out bool[] value);

		/// <summary>Returns the array of string values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetStringArray(out string[] value);

		/// <summary>Returns the array of inspectable objects stored as a property value.</summary>
		/// <param name="value">The array of objects.</param>
		void GetInspectableArray(out object[] value);

		void GetGuidArray(out Guid[] value);

		void GetDateTimeArray(out DateTime[] value);

		/// <summary>Returns the array of time interval values stored as a property value.</summary>
		/// <param name="value">The array of values.</param>
		void GetTimeSpanArray(out TimeSpan[] value);

		/// <summary>Returns the array of point structures stored as a property value.</summary>
		/// <param name="value">The array of structures.</param>
		void GetPointArray(out Point[] value);

		/// <summary>Returns the array of size structures stored as a property value.</summary>
		/// <param name="value">The array of structures.</param>
		void GetSizeArray(out Size[] value);

		/// <summary>Returns the array of rectangle structures stored as a property value.</summary>
		/// <param name="value">The array of structures.</param>
		void GetRectArray(out Rect[] value);
	}
}
