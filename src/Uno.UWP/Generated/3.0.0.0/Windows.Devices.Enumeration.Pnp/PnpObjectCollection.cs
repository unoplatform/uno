#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration.Pnp
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PnpObjectCollection : global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Enumeration.Pnp.PnpObject>,global::System.Collections.Generic.IEnumerable<global::Windows.Devices.Enumeration.Pnp.PnpObject>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint PnpObjectCollection.Size is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectCollection.GetAt(uint)
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectCollection.Size.get
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectCollection.IndexOf(Windows.Devices.Enumeration.Pnp.PnpObject, out uint)
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectCollection.GetMany(uint, Windows.Devices.Enumeration.Pnp.PnpObject[])
		// Forced skipping of method Windows.Devices.Enumeration.Pnp.PnpObjectCollection.First()
		// Processing: System.Collections.Generic.IReadOnlyList<Windows.Devices.Enumeration.Pnp.PnpObject>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.Devices.Enumeration.Pnp.PnpObject this[int index]
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
		// Processing: System.Collections.Generic.IEnumerable<Windows.Devices.Enumeration.Pnp.PnpObject>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Windows.Devices.Enumeration.Pnp.PnpObject>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::Windows.Devices.Enumeration.Pnp.PnpObject> GetEnumerator()
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
		// Processing: System.Collections.Generic.IReadOnlyCollection<Windows.Devices.Enumeration.Pnp.PnpObject>
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
	}
}
