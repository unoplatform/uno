#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class StyleTypedPropertyAttribute : global::System.Attribute
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public StyleTypedPropertyAttribute() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.StyleTypedPropertyAttribute", "StyleTypedPropertyAttribute.StyleTypedPropertyAttribute()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.StyleTypedPropertyAttribute.StyleTypedPropertyAttribute()
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  string Property;
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		public  global::System.Type StyleTargetType;
		#endif
	}
}
