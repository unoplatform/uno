#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Json
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class JsonObject : global::Windows.Data.Json.IJsonValue,global::System.Collections.Generic.IDictionary<string, global::Windows.Data.Json.IJsonValue>,global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, global::Windows.Data.Json.IJsonValue>>,global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonValueType ValueType
		{
			get
			{
				throw new global::System.NotImplementedException("The member JsonValueType JsonObject.ValueType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint JsonObject.Size is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public JsonObject() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Json.JsonObject", "JsonObject.JsonObject()");
		}
		#endif
		// Forced skipping of method Windows.Data.Json.JsonObject.JsonObject()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonValue GetNamedValue( string name)
		{
			throw new global::System.NotImplementedException("The member JsonValue JsonObject.GetNamedValue(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetNamedValue( string name,  global::Windows.Data.Json.IJsonValue value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Json.JsonObject", "void JsonObject.SetNamedValue(string name, IJsonValue value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonObject GetNamedObject( string name)
		{
			throw new global::System.NotImplementedException("The member JsonObject JsonObject.GetNamedObject(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonArray GetNamedArray( string name)
		{
			throw new global::System.NotImplementedException("The member JsonArray JsonObject.GetNamedArray(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetNamedString( string name)
		{
			throw new global::System.NotImplementedException("The member string JsonObject.GetNamedString(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double GetNamedNumber( string name)
		{
			throw new global::System.NotImplementedException("The member double JsonObject.GetNamedNumber(string name) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool GetNamedBoolean( string name)
		{
			throw new global::System.NotImplementedException("The member bool JsonObject.GetNamedBoolean(string name) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Data.Json.JsonObject.ValueType.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Stringify()
		{
			throw new global::System.NotImplementedException("The member string JsonObject.Stringify() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetString()
		{
			throw new global::System.NotImplementedException("The member string JsonObject.GetString() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double GetNumber()
		{
			throw new global::System.NotImplementedException("The member double JsonObject.GetNumber() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool GetBoolean()
		{
			throw new global::System.NotImplementedException("The member bool JsonObject.GetBoolean() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonArray GetArray()
		{
			throw new global::System.NotImplementedException("The member JsonArray JsonObject.GetArray() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonObject GetObject()
		{
			throw new global::System.NotImplementedException("The member JsonObject JsonObject.GetObject() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Data.Json.JsonObject.Lookup(string)
		// Forced skipping of method Windows.Data.Json.JsonObject.Size.get
		// Forced skipping of method Windows.Data.Json.JsonObject.HasKey(string)
		// Forced skipping of method Windows.Data.Json.JsonObject.GetView()
		// Forced skipping of method Windows.Data.Json.JsonObject.Insert(string, Windows.Data.Json.IJsonValue)
		// Forced skipping of method Windows.Data.Json.JsonObject.Remove(string)
		// Forced skipping of method Windows.Data.Json.JsonObject.Clear()
		// Forced skipping of method Windows.Data.Json.JsonObject.First()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonValue GetNamedValue( string name,  global::Windows.Data.Json.JsonValue defaultValue)
		{
			throw new global::System.NotImplementedException("The member JsonValue JsonObject.GetNamedValue(string name, JsonValue defaultValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonObject GetNamedObject( string name,  global::Windows.Data.Json.JsonObject defaultValue)
		{
			throw new global::System.NotImplementedException("The member JsonObject JsonObject.GetNamedObject(string name, JsonObject defaultValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetNamedString( string name,  string defaultValue)
		{
			throw new global::System.NotImplementedException("The member string JsonObject.GetNamedString(string name, string defaultValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Json.JsonArray GetNamedArray( string name,  global::Windows.Data.Json.JsonArray defaultValue)
		{
			throw new global::System.NotImplementedException("The member JsonArray JsonObject.GetNamedArray(string name, JsonArray defaultValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double GetNamedNumber( string name,  double defaultValue)
		{
			throw new global::System.NotImplementedException("The member double JsonObject.GetNamedNumber(string name, double defaultValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool GetNamedBoolean( string name,  bool defaultValue)
		{
			throw new global::System.NotImplementedException("The member bool JsonObject.GetNamedBoolean(string name, bool defaultValue) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string JsonObject.ToString() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Data.Json.JsonObject Parse( string input)
		{
			throw new global::System.NotImplementedException("The member JsonObject JsonObject.Parse(string input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TryParse( string input, out global::Windows.Data.Json.JsonObject result)
		{
			throw new global::System.NotImplementedException("The member bool JsonObject.TryParse(string input, out JsonObject result) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Data.Json.IJsonValue
		// Processing: System.Collections.Generic.IDictionary<string, Windows.Data.Json.IJsonValue>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, Windows.Data.Json.IJsonValue>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Add( string key,  global::Windows.Data.Json.IJsonValue value)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, Windows.Data.Json.IJsonValue>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool ContainsKey( string key)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, Windows.Data.Json.IJsonValue>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool Remove( string key)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, Windows.Data.Json.IJsonValue>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool TryGetValue( string key, out global::Windows.Data.Json.IJsonValue value)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.Data.Json.IJsonValue this[string key]
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.ICollection<string> Keys
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.ICollection<global::Windows.Data.Json.IJsonValue> Values
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Add( global::System.Collections.Generic.KeyValuePair<string, global::Windows.Data.Json.IJsonValue> item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Clear()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool Contains( global::System.Collections.Generic.KeyValuePair<string, global::Windows.Data.Json.IJsonValue> item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void CopyTo( global::System.Collections.Generic.KeyValuePair<string, global::Windows.Data.Json.IJsonValue>[] array,  int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool Remove( global::System.Collections.Generic.KeyValuePair<string, global::Windows.Data.Json.IJsonValue> item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public int Count
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool IsReadOnly
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Windows.Data.Json.IJsonValue>>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, global::Windows.Data.Json.IJsonValue>> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
	}
}
