using System.Collections.Generic;

namespace Windows.Foundation.Collections;

/// <summary>
/// Represents a property set, which is a set of PropertyValue objects with string keys.
/// </summary>
public partial class PropertySet :
	IDictionary<string, object>,
	IEnumerable<KeyValuePair<string, object>>,
	IObservableMap<string, object>,
	IPropertySet
{
	public uint Size
	{
		get
		{
			throw new global::System.NotImplementedException("The member uint PropertySet.Size is not implemented in Uno.");
		}
	}

	/// <summary>
	/// Creates and initializes a new instance of the property set.
	/// </summary>
	public PropertySet()
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Collections.PropertySet", "PropertySet.PropertySet()");
	}
#endif
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.PropertySet()
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.MapChanged.add
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.MapChanged.remove
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.Lookup(string)
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.Size.get
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.HasKey(string)
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.GetView()
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.Insert(string, object)
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.Remove(string)
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.Clear()
	// Forced skipping of method Windows.Foundation.Collections.PropertySet.First()
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public event global::Windows.Foundation.Collections.MapChangedEventHandler<string, object> MapChanged
	{
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		add
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Collections.PropertySet", "event MapChangedEventHandler<string, object> PropertySet.MapChanged");
		}
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		remove
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Collections.PropertySet", "event MapChangedEventHandler<string, object> PropertySet.MapChanged");
		}
	}
#endif
	// Processing: Windows.Foundation.Collections.IPropertySet
	// Processing: Windows.Foundation.Collections.IObservableMap<string, object>
	// Processing: System.Collections.Generic.IDictionary<string, object>
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.IDictionary<string, object>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void Add(string key, object value)
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.IDictionary<string, object>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool ContainsKey(string key)
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.IDictionary<string, object>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool Remove(string key)
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.IDictionary<string, object>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool TryGetValue(string key, out object value)
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public object this[string key]
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
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::System.Collections.Generic.ICollection<object> Values
	{
		get
		{
			throw new global::System.NotSupportedException();
		}
	}
#endif
	// Processing: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void Add(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void Clear()
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool Contains(global::System.Collections.Generic.KeyValuePair<string, object> item)
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public void CopyTo(global::System.Collections.Generic.KeyValuePair<string, object>[] array, int arrayIndex)
	{
		throw new global::System.NotSupportedException();
	}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, object>>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public bool Remove(global::System.Collections.Generic.KeyValuePair<string, object> item)
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
	}
#endif
	// Processing: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	// DeclaringType: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, object>>
	[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, object>> GetEnumerator()
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
