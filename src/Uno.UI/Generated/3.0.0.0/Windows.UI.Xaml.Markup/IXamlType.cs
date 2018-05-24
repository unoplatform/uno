#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Markup
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IXamlType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Markup.IXamlType BaseType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Markup.IXamlMember ContentProperty
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string FullName
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsArray
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsBindable
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsCollection
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsConstructible
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsDictionary
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool IsMarkupExtension
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Markup.IXamlType ItemType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Markup.IXamlType KeyType
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::System.Type UnderlyingType
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.BaseType.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.ContentProperty.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.FullName.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.IsArray.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.IsCollection.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.IsConstructible.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.IsDictionary.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.IsMarkupExtension.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.IsBindable.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.ItemType.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.KeyType.get
		// Forced skipping of method Windows.UI.Xaml.Markup.IXamlType.UnderlyingType.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		object ActivateInstance();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		object CreateFromString( string value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Markup.IXamlMember GetMember( string name);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void AddToVector( object instance,  object value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void AddToMap( object instance,  object key,  object value);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void RunInitializer();
		#endif
	}
}
