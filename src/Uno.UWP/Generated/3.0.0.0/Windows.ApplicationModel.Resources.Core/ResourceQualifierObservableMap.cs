#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Resources.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResourceQualifierObservableMap : global::Windows.Foundation.Collections.IObservableMap<string, string>,global::System.Collections.Generic.IDictionary<string, string>,global::System.Collections.Generic.IEnumerable<global::System.Collections.Generic.KeyValuePair<string, string>>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ResourceQualifierObservableMap.Size is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.MapChanged.add
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.MapChanged.remove
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.Lookup(string)
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.Size.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.HasKey(string)
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.GetView()
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.Insert(string, string)
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.Remove(string)
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.Clear()
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap.First()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.Collections.MapChangedEventHandler<string, string> MapChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap", "event MapChangedEventHandler<string, string> ResourceQualifierObservableMap.MapChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceQualifierObservableMap", "event MapChangedEventHandler<string, string> ResourceQualifierObservableMap.MapChanged");
			}
		}
		#endif
		// Processing: Windows.Foundation.Collections.IObservableMap<string, string>
		// Processing: System.Collections.Generic.IDictionary<string, string>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, string>
		[global::Uno.NotImplemented]
		public void Add( string key,  string value)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, string>
		[global::Uno.NotImplemented]
		public bool ContainsKey( string key)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, string>
		[global::Uno.NotImplemented]
		public bool Remove( string key)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IDictionary<string, string>
		[global::Uno.NotImplemented]
		public bool TryGetValue( string key, out string value)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public string this[string key]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public global::System.Collections.Generic.ICollection<string> Values
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
		// Processing: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>
		[global::Uno.NotImplemented]
		public void Add( global::System.Collections.Generic.KeyValuePair<string, string> item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>
		[global::Uno.NotImplemented]
		public void Clear()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>
		[global::Uno.NotImplemented]
		public bool Contains( global::System.Collections.Generic.KeyValuePair<string, string> item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>
		[global::Uno.NotImplemented]
		public void CopyTo( global::System.Collections.Generic.KeyValuePair<string, string>[] array,  int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>
		[global::Uno.NotImplemented]
		public bool Remove( global::System.Collections.Generic.KeyValuePair<string, string> item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
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
		// Processing: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>
		[global::Uno.NotImplemented]
		public global::System.Collections.Generic.IEnumerator<global::System.Collections.Generic.KeyValuePair<string, string>> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
	}
}
