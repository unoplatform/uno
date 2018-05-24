#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPropertyValue 
	{
		#if false || false || false || false
		bool IsNumericScalar
		{
			get;
		}
		#endif
		#if false || false || false || false
		global::Windows.Foundation.PropertyType Type
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Foundation.IPropertyValue.Type.get
		// Forced skipping of method Windows.Foundation.IPropertyValue.IsNumericScalar.get
		#if false || false || false || false
		byte GetUInt8();
		#endif
		#if false || false || false || false
		short GetInt16();
		#endif
		#if false || false || false || false
		ushort GetUInt16();
		#endif
		#if false || false || false || false
		int GetInt32();
		#endif
		#if false || false || false || false
		uint GetUInt32();
		#endif
		#if false || false || false || false
		long GetInt64();
		#endif
		#if false || false || false || false
		ulong GetUInt64();
		#endif
		#if false || false || false || false
		float GetSingle();
		#endif
		#if false || false || false || false
		double GetDouble();
		#endif
		#if false || false || false || false
		char GetChar16();
		#endif
		#if false || false || false || false
		bool GetBoolean();
		#endif
		#if false || false || false || false
		string GetString();
		#endif
		#if false || false || false || false
		global::System.Guid GetGuid();
		#endif
		#if false || false || false || false
		global::System.DateTimeOffset GetDateTime();
		#endif
		#if false || false || false || false
		global::System.TimeSpan GetTimeSpan();
		#endif
		#if false || false || false || false
		global::Windows.Foundation.Point GetPoint();
		#endif
		#if false || false || false || false
		global::Windows.Foundation.Size GetSize();
		#endif
		#if false || false || false || false
		global::Windows.Foundation.Rect GetRect();
		#endif
		#if false || false || false || false
		void GetUInt8Array(out byte[] value);
		#endif
		#if false || false || false || false
		void GetInt16Array(out short[] value);
		#endif
		#if false || false || false || false
		void GetUInt16Array(out ushort[] value);
		#endif
		#if false || false || false || false
		void GetInt32Array(out int[] value);
		#endif
		#if false || false || false || false
		void GetUInt32Array(out uint[] value);
		#endif
		#if false || false || false || false
		void GetInt64Array(out long[] value);
		#endif
		#if false || false || false || false
		void GetUInt64Array(out ulong[] value);
		#endif
		#if false || false || false || false
		void GetSingleArray(out float[] value);
		#endif
		#if false || false || false || false
		void GetDoubleArray(out double[] value);
		#endif
		#if false || false || false || false
		void GetChar16Array(out char[] value);
		#endif
		#if false || false || false || false
		void GetBooleanArray(out bool[] value);
		#endif
		#if false || false || false || false
		void GetStringArray(out string[] value);
		#endif
		#if false || false || false || false
		void GetInspectableArray(out object[] value);
		#endif
		#if false || false || false || false
		void GetGuidArray(out global::System.Guid[] value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void GetDateTimeArray(out global::System.DateTimeOffset[] value);
		#endif
		#if false || false || false || false
		void GetTimeSpanArray(out global::System.TimeSpan[] value);
		#endif
		#if false || false || false || false
		void GetPointArray(out global::Windows.Foundation.Point[] value);
		#endif
		#if false || false || false || false
		void GetSizeArray(out global::Windows.Foundation.Size[] value);
		#endif
		#if false || false || false || false
		void GetRectArray(out global::Windows.Foundation.Rect[] value);
		#endif
	}
}
