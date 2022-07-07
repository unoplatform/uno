#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Markup
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IXamlType 
	{
		#if false
		global::Windows.UI.Xaml.Markup.IXamlType BaseType
		{
			get;
		}
		#endif
		#if false
		global::Windows.UI.Xaml.Markup.IXamlMember ContentProperty
		{
			get;
		}
		#endif
		#if false
		string FullName
		{
			get;
		}
		#endif
		#if false
		bool IsArray
		{
			get;
		}
		#endif
		#if false
		bool IsBindable
		{
			get;
		}
		#endif
		#if false
		bool IsCollection
		{
			get;
		}
		#endif
		#if false
		bool IsConstructible
		{
			get;
		}
		#endif
		#if false
		bool IsDictionary
		{
			get;
		}
		#endif
		#if false
		bool IsMarkupExtension
		{
			get;
		}
		#endif
		#if false
		global::Windows.UI.Xaml.Markup.IXamlType ItemType
		{
			get;
		}
		#endif
		#if false
		global::Windows.UI.Xaml.Markup.IXamlType KeyType
		{
			get;
		}
		#endif
		#if false
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
		#if false
		object ActivateInstance();
		#endif
		#if false
		object CreateFromString( string value);
		#endif
		#if false
		global::Windows.UI.Xaml.Markup.IXamlMember GetMember( string name);
		#endif
		#if false
		void AddToVector( object instance,  object value);
		#endif
		#if false
		void AddToMap( object instance,  object key,  object value);
		#endif
		#if false
		void RunInitializer();
		#endif
	}
}
